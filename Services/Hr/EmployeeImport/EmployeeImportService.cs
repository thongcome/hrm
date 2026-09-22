using System.Security.Cryptography;
using HRM.Models;
using HRM.Services.Login;
using HRM.Services.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Hr.EmployeeImport;

public sealed record ImportPreview(
    string FileName, string FileSha256, ParsedFile Parsed,
    int NewEmployees, int ExistingEmployees, int NewOrgs, int ExistingOrgs, int NewOpeningBalances, int ReplacedOpeningBalances,
    Hr_EmployeeImportBatch? SameFileImportedBefore, IReadOnlyList<ImportIssue> Issues, IReadOnlyList<string> Notes)
{
    public bool CanImport => Issues.Count == 0 && (Parsed.Employees.Count + Parsed.Orgs.Count + Parsed.OpeningBalances.Count) > 0;
}

public sealed record ImportResult(long BatchId, int OrgsAdded, int OrgsUpdated, int EmployeesAdded, int EmployeesUpdated,
    int LoginsCreated, int OpeningBalancesImported, IReadOnlyList<string> Warnings);

// Onboards a customer's organisation units and employees from the Excel template
// (EmployeeImportSchema). Two steps, matching the page: PreviewAsync validates everything and
// writes nothing; ImportAsync writes it all in ONE transaction or nothing.
//
// Re-import / double upload never duplicates (CEO, 19 ก.ย. 2569): units are matched by code,
// employees by company + EMP_NO, and an existing row is UPDATED — optional columns left blank in
// the file keep their current value. Imports of one company are serialised with an application
// lock, so two clicks at once run one after the other and the second only updates.
// ESS logins are created afterwards (Identity has its own context) and never for someone who
// already has one; the password is the company's default (ตั้งค่าสลิปเงินเดือน) — without it no
// login is created and the result says so.
public class EmployeeImportService(IDbContextFactory<HRMContext> dbFactory, IServiceProvider services)
{
    public async Task<ImportPreview> PreviewAsync(Stream file, string fileName, string companyCode, CancellationToken ct = default)
    {
        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, ct);
        var sha = Convert.ToHexString(SHA256.HashData(buffer.ToArray()));
        buffer.Position = 0;

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var company = await CompanyAsync(db, companyCode, ct);
        var refs = new ImportReferenceData(
            (await db.Com_Banks.Where(b => b.IsActive).Select(b => b.Code).ToListAsync(ct)).ToHashSet(),
            (await db.pos_positions.Select(p => p.pos_code).ToListAsync(ct)).ToHashSet(StringComparer.OrdinalIgnoreCase),
            (await db.com_organizations.Where(o => o.companyid == company.id && o.code != null && !o.isCompany)
                .Select(o => o.code!).ToListAsync(ct)).ToHashSet(StringComparer.OrdinalIgnoreCase));

        var parsed = EmployeeImportParser.Parse(buffer, refs);
        var issues = parsed.Issues.ToList();
        var notes = new List<string>();

        var empNos = parsed.Employees.Select(e => e.EmpNo).ToList();
        var existing = await db.Hremployee.Where(e => e.companyid == companyCode && empNos.Contains(e.EmpNo))
            .Select(e => new { e.EmpNo, e.IdCard }).ToListAsync(ct);

        // the same ID card may not belong to a DIFFERENT employee code in this company
        var idCards = parsed.Employees.Select(e => e.IdCard).ToList();
        var idOwners = await db.Hremployee.Where(e => e.companyid == companyCode && e.IdCard != null && idCards.Contains(e.IdCard))
            .Select(e => new { e.EmpNo, e.IdCard }).ToListAsync(ct);
        foreach (var e in parsed.Employees)
        {
            var owner = idOwners.FirstOrDefault(o => o.IdCard == e.IdCard && !o.EmpNo.Equals(e.EmpNo, StringComparison.OrdinalIgnoreCase));
            if (owner is not null)
                issues.Add(new(EmployeeImportSchema.Employee.Name, e.Row, "เลขบัตรประชาชน",
                    $"เลขบัตรนี้เป็นของพนักงานรหัส {owner.EmpNo} อยู่แล้วในระบบ"));
        }

        // approvers named on the unit sheet must be employees (in the file or already in the system)
        var approverNos = parsed.Orgs.Where(o => o.ApproverEmpNo is not null).Select(o => o.ApproverEmpNo!).Distinct().ToList();
        var knownApprovers = (await db.Hremployee.Where(e => e.companyid == companyCode && approverNos.Contains(e.EmpNo))
            .Select(e => e.EmpNo).ToListAsync(ct)).Concat(empNos).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var o in parsed.Orgs.Where(o => o.ApproverEmpNo is not null && !knownApprovers.Contains(o.ApproverEmpNo!)))
            issues.Add(new(EmployeeImportSchema.Org.Name, o.Row, "รหัสพนักงานผู้อนุมัติ", $"ไม่พบพนักงาน {o.ApproverEmpNo} ทั้งในไฟล์และในระบบ"));

        var (newOpening, replacedOpening) = await CheckOpeningBalancesAsync(db, parsed, companyCode, issues, ct);
        if (parsed.Employees.Any(e => e.CreateLogin) && await DefaultPasswordAsync(db, companyCode, ct) is null)
            notes.Add("ยังไม่ได้ตั้งรหัสผ่านตั้งต้นของพนักงาน (ตั้งค่าสลิปเงินเดือน) — จะนำเข้าพนักงานได้ แต่ยังไม่สร้าง user ESS ให้");

        var before = await db.Hr_EmployeeImportBatches.Where(b => b.CompanyId == companyCode && b.FileSha256 == sha)
            .OrderByDescending(b => b.ImportedAt).FirstOrDefaultAsync(ct);
        var orgCodes = parsed.Orgs.Select(o => o.Code).ToList();
        var existingOrgs = await db.com_organizations.CountAsync(o => o.companyid == company.id && orgCodes.Contains(o.code!), ct);

        return new ImportPreview(fileName, sha, parsed,
            parsed.Employees.Count - existing.Count, existing.Count,
            parsed.Orgs.Count - existingOrgs, existingOrgs,
            newOpening, replacedOpening,
            before, issues, notes);
    }

    // ยอดยกมา rows: the employee must be in the file or already in the system; the month must be in
    // the past; and it must not be a month this system already calculated for that employee — the
    // same pay would then count twice in YTD tax, 50 ทวิ and ภ.ง.ด.1ก. Returns (new, replaced).
    // Ported from Advance.Payroll (CEO order, 22 ก.ย. 2569: mirror the payroll domain).
    private static async Task<(int New, int Replaced)> CheckOpeningBalancesAsync(
        HRMContext db, ParsedFile parsed, string companyCode, List<ImportIssue> issues, CancellationToken ct)
    {
        if (parsed.OpeningBalances.Count == 0) return (0, 0);
        var sheet = EmployeeImportSchema.OpeningBalance.Name;
        var inFile = parsed.Employees.Select(e => e.EmpNo).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var nos = parsed.OpeningBalances.Select(o => o.EmpNo).Distinct().ToList();
        var inSystem = (await db.Hremployee.Where(e => e.companyid == companyCode && nos.Contains(e.EmpNo))
            .Select(e => new { e.id, e.EmpNo }).ToListAsync(ct))
            .ToDictionary(e => e.EmpNo, e => e.id, StringComparer.OrdinalIgnoreCase);
        var ids = inSystem.Values.ToList();
        var years = parsed.OpeningBalances.Select(o => o.TaxYear).Distinct().ToList();

        var calculated = (await db.Pay_PayrollEmployees
                .Where(pe => ids.Contains(pe.HremployeeId) && !pe.IsExcluded
                             && pe.Pay_PayrollRun.Status != PayrollRunStatus.Cancelled
                             && years.Contains(pe.Pay_PayrollRun.PeriodStart.Year))
                .Select(pe => new { pe.HremployeeId, pe.Pay_PayrollRun.PeriodStart.Year, pe.Pay_PayrollRun.PeriodStart.Month, pe.Pay_PayrollRun.PayrollPeriod })
                .ToListAsync(ct))
            .ToLookup(x => (x.HremployeeId, x.Year, x.Month));
        var existing = (await db.Pay_EmployeeOpeningBalances
                .Where(o => ids.Contains(o.HremployeeId) && years.Contains(o.TaxYear))
                .Select(o => new { o.HremployeeId, o.TaxYear, o.Month }).ToListAsync(ct))
            .Select(o => (o.HremployeeId, o.TaxYear, o.Month)).ToHashSet();

        var thisMonth = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1);
        int added = 0, replaced = 0;
        foreach (var o in parsed.OpeningBalances)
        {
            var label = $"{o.Month}/{o.TaxYear + 543}";
            if (new DateOnly(o.TaxYear, o.Month, 1) > thisMonth)
            { issues.Add(new(sheet, o.Row, "เดือน", $"เดือน {label} ยังมาไม่ถึง — ยอดยกมาคือเดือนที่จ่ายจากระบบเดิมไปแล้ว")); continue; }
            if (!inSystem.TryGetValue(o.EmpNo, out var id))
            {
                if (!inFile.Contains(o.EmpNo))
                    issues.Add(new(sheet, o.Row, "รหัสพนักงาน", $"ไม่พบพนักงาน {o.EmpNo} ทั้งในชีตพนักงานและในระบบ"));
                else added++;
                continue;
            }
            var run = calculated[(id, o.TaxYear, o.Month)].FirstOrDefault();
            if (run is not null)
            { issues.Add(new(sheet, o.Row, "เดือน", $"{o.EmpNo} เดือน {label} คำนวณในระบบนี้แล้ว (รอบ {run.PayrollPeriod}) — ใส่ยอดยกมาซ้ำจะนับเงินได้/ภาษีสองครั้ง")); continue; }
            if (existing.Contains((id, o.TaxYear, o.Month))) replaced++; else added++;
        }
        return (added, replaced);
    }

    public async Task<ImportResult> ImportAsync(ImportPreview preview, string companyCode, long actorUserId, CancellationToken ct = default)
    {
        if (!preview.CanImport) throw new InvalidOperationException("ไฟล์ยังมีข้อผิดพลาด — แก้ให้หมดก่อนนำเข้า");
        // ด่านสุดท้ายของเครื่องมือช่วงติดตั้ง (หน้าและ endpoint กันไว้แล้ว) — การเขียนทับทะเบียนพนักงาน
        // ทั้งบริษัทต้องผ่านด่านนี้เสมอ ไม่ว่าจะถูกเรียกจากที่ไหน
        if (!Services.Deploy.InstallerToolsGate.IsEnabled(services))
            throw new InvalidOperationException(Services.Deploy.InstallerToolsGate.DisabledMessage);
        var parsed = preview.Parsed;
        var warnings = new List<string>();
        int orgsAdded = 0, orgsUpdated = 0, empAdded = 0, empUpdated = 0;
        var loginCandidates = new List<long>();

        await using (var db = await dbFactory.CreateDbContextAsync(ct))
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            // one import per company at a time — a double click waits, then only updates
            await db.Database.ExecuteSqlRawAsync(
                "EXEC sp_getapplock @Resource = {0}, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 60000",
                ["EmployeeImport:" + companyCode], ct);

            var company = await CompanyAsync(db, companyCode, ct);
            var root = await db.com_organizations.FirstOrDefaultAsync(o => o.companyid == company.id && o.isCompany, ct)
                ?? throw new InvalidOperationException("ไม่พบหน่วยงานระดับบริษัท (ผังองค์กรว่างเกินไป) — ติดต่อผู้ดูแลระบบ");

            // ── units: insert/update, parents first ───────────────────────────────
            var units = await db.com_organizations.Where(o => o.companyid == company.id && !o.isCompany).ToListAsync(ct);
            var byCode = units.Where(o => o.code != null).GroupBy(o => o.code!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            foreach (var row in OrderParentsFirst(parsed.Orgs))
            {
                var parent = row.ParentCode is null ? root : byCode[row.ParentCode];
                if (byCode.TryGetValue(row.Code, out var org))
                {
                    var moved = org.parentID != parent.id;
                    org.name = row.Name;
                    org.parent_code = parent.code;
                    org.parentID = parent.id;
                    if (moved) org.orgcodefull = NextChildPath(parent, byCode.Values);
                    org.moddate = DateTime.Now;
                    orgsUpdated++;
                }
                else
                {
                    org = new com_organization
                    {
                        code = row.Code, name = row.Name, parent_code = parent.code, parentID = parent.id,
                        companyid = company.id, comp_code = companyCode, isActive = true, isCompany = false,
                        orgcodefull = NextChildPath(parent, byCode.Values), moddate = DateTime.Now, modby = "import",
                    };
                    db.com_organizations.Add(org);
                    await db.SaveChangesAsync(ct);
                    byCode[row.Code] = org;
                    orgsAdded++;
                }
            }
            await db.SaveChangesAsync(ct);

            // ── employees: match by company + EMP_NO ──────────────────────────────
            var empNos = parsed.Employees.Select(e => e.EmpNo).ToList();
            var existing = (await db.Hremployee.Where(e => e.companyid == companyCode && empNos.Contains(e.EmpNo)).ToListAsync(ct))
                .ToDictionary(e => e.EmpNo, StringComparer.OrdinalIgnoreCase);
            var emailByEmp = new List<(Hremployee Emp, string Email)>();
            var addressByEmp = new List<(Hremployee Emp, ParsedAddress Address)>();
            foreach (var r in parsed.Employees)
            {
                if (!existing.TryGetValue(r.EmpNo, out var e))
                {
                    e = new Hremployee { companyid = companyCode, EmpNo = r.EmpNo, IsActive = true };
                    db.Hremployee.Add(e);
                    empAdded++;
                }
                else empUpdated++;

                var org = byCode.TryGetValue(r.OrgCode, out var o) ? o : throw new InvalidOperationException($"ไม่พบหน่วยงาน {r.OrgCode}");
                e.PrenameCode = r.PrenameCode;
                e.Sex = r.Sex;
                e.EmpName = r.FirstName;
                e.EmpSurname = r.LastName;
                e.EmpEname = r.FirstNameEn ?? e.EmpEname;
                e.EmpEsurname = r.LastNameEn ?? e.EmpEsurname;
                e.IdCard = r.IdCard;
                e.BirthDate = r.BirthDate ?? e.BirthDate;
                e.WorkDate = r.HireDate;
                e.OrganizationId = org.id;
                e.orgcode = org.code;
                e.orgcodefull = org.orgcodefull;
                e.PosCode = r.PosCode ?? e.PosCode;
                e.EmptypeCode = r.EmpTypeCode;
                // the engine treats "daily wage > 0 and salary 0" as a daily-paid employee
                e.SalaryAmt = r.IsDaily ? 0m : r.Pay;
                e.DailyWage = r.IsDaily ? r.Pay : 0m;
                e.SalexpBank = r.BankCode;
                e.SalexpAccid = r.BankAccount;
                e.SalexpBranch = r.BankBranch ?? e.SalexpBranch;
                if (r.PvdEmployeeRate is not null) { e.ProvfEmprate = r.PvdEmployeeRate; e.ProvfCorprate = r.PvdEmployerRate ?? r.PvdEmployeeRate; }
                e.AdnTel = r.Phone ?? e.AdnTel;
                if (r.Email is not null) emailByEmp.Add((e, r.Email));
                if (r.Address is not null) addressByEmp.Add((e, r.Address));
            }
            await db.SaveChangesAsync(ct);
            foreach (var (emp, email) in emailByEmp) await EmployeeEmailResolver.SetAsync(db, emp.id, email, ct);

            // ที่อยู่ → ตาราง address เดิม: แถว REG = แทนที่ด้วยค่าในไฟล์ (อัปโหลดซ้ำไม่เกิดแถวซ้ำ) ·
            // แถว CUR = เติมให้เฉพาะเมื่อยังไม่มีที่อยู่ (อาจมีแถวอยู่แล้วเพราะเก็บอีเมล) ไม่ทับที่พนักงานแก้เองใน ESS
            foreach (var (emp, a) in addressByEmp)
            {
                var rows = await db.addresses.Where(x => x.hremployeeid == emp.id && x.isactive).ToListAsync(ct);
                foreach (var typeId in new long[] { 1, 2 })   // mas_address_type: 1 = REG, 2 = CUR
                {
                    var row = rows.Where(x => x.address_type_id == typeId).OrderByDescending(x => x.moddate ?? x.createdate).FirstOrDefault();
                    var curHasAddress = typeId == 2 && row is not null
                        && new[] { row.no, row.subdistrict, row.districtid, row.province, row.postcode }.Any(v => !string.IsNullOrWhiteSpace(v));
                    if (curHasAddress) continue;
                    if (row is null)
                    {
                        row = new address { hremployeeid = emp.id, address_type_id = typeId, isactive = true, createdate = DateTime.Now };
                        db.addresses.Add(row);
                    }
                    else row.moddate = DateTime.Now;
                    row.no = a.No; row.moo = a.Moo; row.village = a.Village; row.soi = a.Soi; row.road = a.Road;
                    row.subdistrict = a.Subdistrict; row.districtid = a.District; row.province = a.Province; row.postcode = a.Postcode;
                }
            }
            if (addressByEmp.Count > 0) await db.SaveChangesAsync(ct);

            // ── unit approvers (after employees exist) ────────────────────────────
            foreach (var row in parsed.Orgs.Where(o => o.ApproverEmpNo is not null))
            {
                var org = byCode[row.Code];
                var approver = await db.Hremployee.FirstAsync(e => e.companyid == companyCode && e.EmpNo == row.ApproverEmpNo, ct);
                org.approver_hremployee_id = approver.id;   // id คือความจริง — approver_empid ตามมาเอง (HRMContext.OrgParent.cs)
                org.approver_name = $"{approver.EmpName} {approver.EmpSurname}".Trim();
                org.approver_userid = await db.sc_users.Where(u => u.hremployee_id == approver.id && u.isdisable != true)
                    .OrderBy(u => u.userid).Select(u => (long?)u.userid).FirstOrDefaultAsync(ct);
            }

            var batch = new Hr_EmployeeImportBatch
            {
                CompanyId = companyCode, FileName = preview.FileName, FileSha256 = preview.FileSha256,
                OrgsAdded = orgsAdded, OrgsUpdated = orgsUpdated, EmployeesAdded = empAdded, EmployeesUpdated = empUpdated,
                ImportedByUserId = actorUserId, ImportedAt = DateTime.Now,
            };
            db.Hr_EmployeeImportBatches.Add(batch);
            await db.SaveChangesAsync(ct);

            // ── ยอดยกมา: one row per employee per month, a re-import REPLACES ──────
            if (parsed.OpeningBalances.Count > 0)
            {
                // re-checked inside the lock: a payroll run may have been calculated since the preview
                var recheck = new List<ImportIssue>();
                await CheckOpeningBalancesAsync(db, parsed, companyCode, recheck, ct);
                if (recheck.Count > 0)
                    throw new InvalidOperationException("ยอดยกมาขัดกับข้อมูลในระบบแล้ว — ตรวจไฟล์ใหม่อีกครั้ง: " + recheck[0].Message);
                batch.OpeningBalancesImported = await UpsertOpeningBalancesAsync(db, parsed.OpeningBalances, companyCode, batch.Id, actorUserId, ct);
                await db.SaveChangesAsync(ct);
            }
            await tx.CommitAsync(ct);

            var wanted = parsed.Employees.Where(e => e.CreateLogin).Select(e => e.EmpNo).ToList();
            loginCandidates = await db.Hremployee.Where(e => e.companyid == companyCode && wanted.Contains(e.EmpNo)).Select(e => e.id).ToListAsync(ct);

            var logins = await CreateLoginsAsync(companyCode, company.id, loginCandidates, preview.Parsed.Employees, warnings, ct);
            batch.LoginsCreated = logins;
            var joined = string.Join(" | ", warnings);
            batch.Warnings = joined.Length == 0 ? null : joined[..Math.Min(2000, joined.Length)];
            await db.SaveChangesAsync(ct);
            return new ImportResult(batch.Id, orgsAdded, orgsUpdated, empAdded, empUpdated, logins, batch.OpeningBalancesImported, warnings);
        }
    }

    // Ported from Advance.Payroll (CEO order, 22 ก.ย. 2569: mirror the payroll domain).
    private static async Task<int> UpsertOpeningBalancesAsync(HRMContext db, IReadOnlyList<ParsedOpeningBalance> rows,
        string companyCode, long batchId, long actorUserId, CancellationToken ct)
    {
        var nos = rows.Select(r => r.EmpNo).Distinct().ToList();
        var empIds = (await db.Hremployee.Where(e => e.companyid == companyCode && nos.Contains(e.EmpNo))
                .Select(e => new { e.id, e.EmpNo }).ToListAsync(ct))
            .ToDictionary(e => e.EmpNo, e => (Id: e.id, EmpNo: e.EmpNo), StringComparer.OrdinalIgnoreCase);
        var ids = empIds.Values.Select(v => v.Id).ToList();
        var years = rows.Select(r => r.TaxYear).Distinct().ToList();
        var current = (await db.Pay_EmployeeOpeningBalances.Where(o => ids.Contains(o.HremployeeId) && years.Contains(o.TaxYear)).ToListAsync(ct))
            .ToDictionary(o => (o.HremployeeId, o.TaxYear, o.Month));

        foreach (var r in rows)
        {
            var (id, empNo) = empIds[r.EmpNo];
            if (!current.TryGetValue((id, r.TaxYear, r.Month), out var o))
            {
                o = new Pay_EmployeeOpeningBalance { CompanyId = companyCode, HremployeeId = id, TaxYear = r.TaxYear, Month = r.Month };
                db.Pay_EmployeeOpeningBalances.Add(o);
                current[(id, r.TaxYear, r.Month)] = o;
            }
            o.EmpNo = empNo;
            o.GrossIncome = r.GrossIncome;
            o.TaxableIncome = r.TaxableIncome;
            o.TaxWithheld = r.TaxWithheld;
            o.SsoEmployee = r.SsoEmployee;
            o.SsoEmployer = r.SsoEmployer;
            o.PvdEmployee = r.PvdEmployee;
            o.PvdEmployer = r.PvdEmployer;
            o.NetPay = r.NetPay;
            o.ImportBatchId = batchId;
            o.IsActive = true;
            o.UpdatedByUserId = actorUserId;
            o.UpdatedAt = DateTime.Now;
        }
        return rows.Count;
    }

    public async Task<List<Hr_EmployeeImportBatch>> HistoryAsync(string companyCode, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Hr_EmployeeImportBatches.Where(b => b.CompanyId == companyCode)
            .OrderByDescending(b => b.ImportedAt).Take(20).ToListAsync(ct);
    }

    // ── ESS logins: only for people without one; never touches an existing password ──
    private async Task<int> CreateLoginsAsync(string companyCode, long companyId, List<long> employeeIds,
        IReadOnlyList<ParsedEmployee> rows, List<string> warnings, CancellationToken ct)
    {
        if (employeeIds.Count == 0) return 0;
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var password = await DefaultPasswordAsync(db, companyCode, ct);
        if (password is null)
        {
            warnings.Add("ไม่ได้สร้าง user ESS: ยังไม่ได้ตั้งรหัสผ่านตั้งต้นในตั้งค่าสลิปเงินเดือน");
            return 0;
        }

        await using var scope = services.CreateAsyncScope();
        var provisioning = scope.ServiceProvider.GetRequiredService<UserProvisioningService>();
        var hasher = new PasswordHasher<sc_user>();
        var created = 0;
        var emps = await db.Hremployee.Where(e => employeeIds.Contains(e.id)).ToListAsync(ct);
        foreach (var emp in emps)
        {
            if (await db.sc_users.AnyAsync(u => u.loginname == emp.EmpNo, ct)) continue;   // already has a login
            var user = new sc_user
            {
                loginname = emp.EmpNo, empid = emp.EmpNo, firstname = emp.EmpName ?? emp.EmpNo, lastname = emp.EmpSurname ?? "",
                company_id = companyId, isforcechanged = true, isActivate = true, isdisable = false, iscancel = false,
                isEmployee = "Y", moddate = DateTime.Now, modby = "import",
            };
            user.password = hasher.HashPassword(user, password);
            db.sc_users.Add(user);
            await db.SaveChangesAsync(ct);

            var email = rows.FirstOrDefault(r => r.EmpNo.Equals(emp.EmpNo, StringComparison.OrdinalIgnoreCase))?.Email;
            var result = await provisioning.EnsureIdentityLinkedAsync(user, password, email, ct);
            if (result.Succeeded) created++;
            else warnings.Add($"{emp.EmpNo}: สร้าง user ไม่สำเร็จ — {result.Error}");
        }
        return created;
    }

    // ── helpers ─────────────────────────────────────────────────────────────────
    private static async Task<com_company> CompanyAsync(HRMContext db, string code, CancellationToken ct) =>
        await db.com_companies.FirstOrDefaultAsync(c => c.code == code, ct)
        ?? throw new InvalidOperationException($"ไม่พบบริษัท {code}");

    private static async Task<string?> DefaultPasswordAsync(HRMContext db, string companyCode, CancellationToken ct)
    {
        var s = await db.Pay_PayslipSettings.FirstOrDefaultAsync(x => x.CompanyId == companyCode, ct);
        var combined = (s?.DefaultPasswordPart1 ?? "") + (s?.DefaultPasswordPart2 ?? "");
        return combined.Length == 0 ? null : combined;
    }

    private static IEnumerable<ParsedOrg> OrderParentsFirst(IReadOnlyList<ParsedOrg> orgs)
    {
        var byCode = orgs.ToDictionary(o => o.Code, StringComparer.OrdinalIgnoreCase);
        int Depth(ParsedOrg o)
        {
            var d = 0;
            for (var p = o.ParentCode; p is not null && byCode.TryGetValue(p, out var parent) && d < 50; p = parent.ParentCode) d++;
            return d;
        }
        return orgs.OrderBy(Depth);
    }

    // org paths are the parent's path + a 2-digit sibling number (com_organization.orgcodefull)
    private static string NextChildPath(com_organization parent, IEnumerable<com_organization> all)
    {
        var prefix = parent.orgcodefull ?? "";
        var used = all.Where(o => o.parentID == parent.id && o.orgcodefull != null && o.orgcodefull.Length == prefix.Length + 2
                                  && o.orgcodefull.StartsWith(prefix, StringComparison.Ordinal))
            .Select(o => int.TryParse(o.orgcodefull![prefix.Length..], out var n) ? n : 0)
            .DefaultIfEmpty(0).Max();
        return prefix + (used + 1).ToString("00");
    }
}
