namespace HRM.Services.Dev;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Demo-scale company "AUTOX" (CEO order, 2026-09-01): 7,000 fictitious Thai
// employees wired consistently into com_organization + position tables +
// sc_user, so demos and performance testing have a believable large company
// next to the tiny real "001" data set.
//
// Design notes (all verified against the live schema/data before writing):
//
// - ONE-SHOT / IDEMPOTENT: if any HREMPLOYEE row with companyid = "AUTOX"
//   exists the seeder returns immediately. To re-seed from scratch, delete
//   the AUTOX rows (HREMPLOYEE / sc_user+sc_user_role via empid like 'AX%',
//   com_organization comp_code='AUTOX', pos_position pos_code 'A01'-'A07',
//   Pos_ExecType CompanyId='AUTOX', com_company code='AUTOX') and restart.
//   All randomness comes from ONE Random with a FIXED seed consumed in a
//   fixed order, so a wipe + rerun reproduces identical data.
//
// - Never touches any existing row in any table (company "001", real
//   sc_user accounts, the existing CEO/HR org tree). Every phase inserts
//   only rows that are missing, keyed by AUTOX-prefixed codes that cannot
//   collide with existing data ("AX...." EmpNos, "AX"/"AX-*" org codes,
//   "A01".."A07" position codes — existing data uses "001"-style codes).
//
// - Column choices mirror what existing rows actually populate:
//   * HREMPLOYEE: modern rows (e.g. EMP_NO 002) carry OrganizationId +
//     orgcode + orgcodefull; DEPTGRP_CODE is the broken legacy linkage
//     (see Hremployee.cs doc comments) and stays NULL here, like row 002.
//   * com_organization: code/name/layer_code/parent_code/node_level/istop/
//     isCompany/orgcodefull/approver_empid — the exact columns the existing
//     CEO/HR tree uses. approver_empid drives the workflow engine's
//     vertical approval (WorkflowEngineService.ResolveOrgChainApproverAsync
//     anchors on Hremployee.orgcode and walks parent_code by code).
//   * sc_user: mirrors userid 26 (empid '008'): loginname = empid = EmpNo,
//     isdisable=0, iscancel=0, isActivate=1, isroot=0, isforcechanged=1,
//     isEmployee left NULL (every existing row has NULL), one active
//     sc_user_role on the "Employee" role. NO password and NO ASP.NET
//     Identity account — linking a login stays a separate explicit step
//     (UserProvisioningService / LinkIdentityAccount).
//
// - Identity columns verified via sys.columns (2026-09-01): HREMPLOYEE.ID,
//   com_organization.id, com_company.id, pos_position.id, Pos_ExecType.Id,
//   sc_user.userid and sc_user_role.user_roleID are ALL real IDENTITY
//   columns, so EF's Identity mapping is safe here and
//   EntitySearchHelper.NextIdAsync is not needed. The one non-identity
//   NOT NULL key-ish column is pos_position.posid (plain int) — supplied
//   explicitly below.
//
// - Writes Hremployee.OrganizationId/orgcode/orgcodefull directly instead
//   of going through Pos_PositionSlot + EmployeePositionSync (the normal
//   single source of truth per Hremployee.cs). Deliberate, documented
//   deviation: creating 7,000 headcount-slot rows would double the insert
//   volume for no demo benefit, and no slot rows exist for AUTOX that the
//   sync could ever contradict.
//
// - The automatic audit hook in HRMContext.Audit.cs logs every insert —
//   correct and intentional; expect roughly 21,000+ AuditLog rows from one
//   full run.
public static class AutoxDemoSeeder
{
    private const string CompanyId = "AUTOX";           // HREMPLOYEE.companyid (nvarchar(6)) — the string company scope
    private const string SeederName = "AutoxDemoSeeder";
    private const int FixedSeed = 20260901;             // fixed → deterministic reruns after a wipe
    private const int BatchSize = 500;
    private const int TotalEmployees = 7000;

    // Fixed "today" anchor so BirthDate/WorkDate are deterministic too —
    // never DateTime.Now anywhere in this class.
    private static readonly DateTime Anchor = new(2026, 9, 1);

    // ---------------------------------------------------------------------
    // Fictitious Thai name parts (in-code, no network). Combinations are
    // random-but-deterministic; duplicates across 7,000 people are fine
    // (EmpNo is the unique key, as in real life).
    // ---------------------------------------------------------------------
    private static readonly string[] MaleFirstNames =
    {
        "สมชาย","สมศักดิ์","ประเสริฐ","วิชัย","สุรชัย","อนุชา","ธนกร","กิตติพงษ์","ณัฐพล","ปิยะ",
        "จักรพันธ์","เอกชัย","วีระ","ชัยวัฒน์","พงศกร","ศุภชัย","ทวีศักดิ์","ไพโรจน์","มานพ","บุญส่ง",
        "คมสัน","อาทิตย์","นราธิป","ภาณุวัฒน์","เกรียงไกร","ยุทธนา","สมพงษ์","ธีรพงษ์","วรวุฒิ","สถาพร",
        "อดิศักดิ์","นพดล","ชูชาติ","สันติ","พีระพล","ธวัชชัย","สุทธิพงษ์","กมล","อภิสิทธิ์","จิรายุ",
        "ปกรณ์","รัฐพล","วุฒิชัย","สราวุธ","องอาจ","ชนินทร์","ดนัย","ตะวัน","ถิรวัฒน์","ทศพร",
        "นันทวัฒน์","บวร","ปรีชา","พลากร","ภูวดล","มงคล","รณชัย","ฤทธิชัย","วสันต์","ศักดิ์ดา",
    };

    private static readonly string[] FemaleFirstNames =
    {
        "กัญญา","ขวัญใจ","จันทร์เพ็ญ","ฉวีวรรณ","ชลธิชา","ญาณิศา","ฐิติมา","ณัฐธิดา","ดวงใจ","ทิพวรรณ",
        "ธนพร","นภาพร","นิภาพร","บุษบา","ปวีณา","ผกามาศ","พรทิพย์","พิมพ์ใจ","ภัทราวดี","มะลิวัลย์",
        "ยุพิน","รัตนา","ลัดดาวัลย์","วรรณา","วาสนา","ศิริพร","สายฝน","สุกัญญา","สุดารัตน์","สุนิสา",
        "สุพรรณี","สุมาลี","เสาวลักษณ์","หทัยรัตน์","อรทัย","อรุณี","อัจฉรา","อุไรวรรณ","เบญจวรรณ","กมลชนก",
        "จิราภรณ์","ชุติมา","ณิชกานต์","ดารุณี","ธัญญรัตน์","นันทนา","ปนัดดา","ปรียานุช","พัชรี","เพ็ญศรี",
        "ภาวิณี","มณีรัตน์","รุ่งนภา","วิภาดา","ศศิธร","สิริพร","อมรรัตน์","อินทิรา","อำไพ","กาญจนา",
    };

    private static readonly string[] Surnames =
    {
        "บุญประเสริฐ","ศรีสมบัติ","วงศ์วัฒนา","จันทร์เจริญ","สุขสวัสดิ์","ทองดีเลิศ","แก้วมณี","พูลทรัพย์","รุ่งเรืองกิจ","อินทร์อุดม",
        "คำมั่นคง","ธาราทิพย์","ป้อมเพชร","มั่งมีศรี","ยอดยิ่งยง","เลิศวิไล","วัฒนชัยกุล","ศิริมงคล","สายสัมพันธ์","หาญกล้า",
        "อุดมโชค","เอี่ยมสอาด","โชติช่วงชัย","ไชยประเสริฐ","กิจเจริญพร","ขจรเกียรติ","คงคาทอง","งามพร้อมสุข","จิตรใสเย็น","ใจภักดี",
        "ชัยชนะกุล","ดวงแก้วใส","ตั้งตรงจิตร","ถาวรวงศ์","ทวีทรัพย์สิน","ธนบดีศรี","นาคสุวรรณ","บัวบานเย็น","ประกายเพชร","ผ่องอำไพวงศ์",
        "พงษ์พิพัฒน์","ฟ้าประทาน","ภักดีคุณากร","มงคลวรกุล","ยั่งยืนนาน","รักษาธรรมกุล","ฤกษ์งามดี","ลาภอนันต์","วิเศษสุนทร","ศรแก้วกล้า",
        "สกุลรุ่งโรจน์","สิงห์ทองคำ","เสรีวัฒน์","หงษ์ทองแท้","อนันตกูล","อัครวุฒิกุล","เพียรพากเพียร","เมฆขาวสะอาด","แสงจันทร์งาม","ไพศาลศรี",
    };

    // ---------------------------------------------------------------------
    // Org tree definition: HQ → 6 divisions → 30 departments → 120 sections
    // ---------------------------------------------------------------------
    private static readonly (string Code, string Name)[] Divisions =
    {
        ("PRD", "ฝ่ายผลิต"),
        ("SAL", "ฝ่ายขายและการตลาด"),
        ("FIN", "ฝ่ายการเงินและบัญชี"),
        ("HRM", "ฝ่ายทรัพยากรบุคคล"),
        ("ITC", "ฝ่ายเทคโนโลยีสารสนเทศ"),
        ("SCM", "ฝ่ายซัพพลายเชน"),
    };

    private static readonly string[][] DepartmentNames =
    {
        new[] { "แผนกประกอบยานยนต์", "แผนกพ่นสีและตัวถัง", "แผนกควบคุมคุณภาพ", "แผนกวิศวกรรมการผลิต", "แผนกซ่อมบำรุงเครื่องจักร" },
        new[] { "แผนกขายในประเทศ", "แผนกขายต่างประเทศ", "แผนกการตลาดดิจิทัล", "แผนกบริการหลังการขาย", "แผนกบริหารตัวแทนจำหน่าย" },
        new[] { "แผนกบัญชีทั่วไป", "แผนกการเงินรับ-จ่าย", "แผนกงบประมาณ", "แผนกภาษีอากร", "แผนกตรวจสอบภายใน" },
        new[] { "แผนกสรรหาว่าจ้าง", "แผนกค่าตอบแทนและสวัสดิการ", "แผนกพัฒนาบุคลากร", "แผนกแรงงานสัมพันธ์", "แผนกความปลอดภัยในการทำงาน" },
        new[] { "แผนกพัฒนาระบบ", "แผนกโครงสร้างพื้นฐานไอที", "แผนกความมั่นคงปลอดภัยไซเบอร์", "แผนกสนับสนุนผู้ใช้งาน", "แผนกข้อมูลและวิเคราะห์" },
        new[] { "แผนกจัดซื้อ", "แผนกคลังสินค้า", "แผนกขนส่งและกระจายสินค้า", "แผนกวางแผนการผลิต", "แผนกบริหารซัพพลายเออร์" },
    };

    private const int SectionsPerDept = 4; // 6 × 5 × 4 = 120 sections

    // ---------------------------------------------------------------------
    // Position ladder. Codes are 3 chars because HREMPLOYEE.POS_CODE is
    // nvarchar(3) in the live DB; "A01".."A07" deliberately avoids the
    // existing employees' "001"-style codes so nothing pre-existing starts
    // resolving to these new rows. Salary bands span 15k–250k.
    // ---------------------------------------------------------------------
    private static readonly (string Code, string Name, string NameEn, int MinSalary, int MaxSalary)[] Positions =
    {
        ("A01", "พนักงาน",                 "Officer",              15_000,  28_000),
        ("A02", "พนักงานอาวุโส",            "Senior Officer",       26_000,  45_000),
        ("A03", "หัวหน้างาน",               "Supervisor",           40_000,  60_000),
        ("A04", "ผู้จัดการแผนก",             "Department Manager",   60_000,  90_000),
        ("A05", "ผู้จัดการฝ่าย",             "Division Manager",     90_000, 140_000),
        ("A06", "ผู้อำนวยการฝ่าย",           "Division Director",   150_000, 200_000),
        ("A07", "ประธานเจ้าหน้าที่บริหาร",     "Chief Executive Officer", 250_000, 250_000),
    };

    private sealed class OrgPlan
    {
        public required string Code { get; init; }
        public required string Name { get; init; }
        public string? ParentCode { get; init; }
        public required int Depth { get; init; }        // 1=HQ, 2=division, 3=department, 4=section
        public required string OrgFull { get; init; }   // fixed-width 2-digits-per-level path
        public string? ApproverEmpNo { get; set; }      // the head employee created for this node
        public string? ApproverName { get; set; }
    }

    private sealed class EmpPlan
    {
        public required string EmpNo { get; init; }
        public required string FirstName { get; init; }
        public required string LastName { get; init; }
        public required string Sex { get; init; }       // "M"/"F"
        public required string Prename { get; init; }   // "1"=นาย, "2"=นาง, "3"=นางสาว
        public required string PosCode { get; init; }
        public required string OrgCode { get; init; }
        public required string OrgFull { get; init; }
        public required decimal Salary { get; init; }
        public required DateTime BirthDate { get; init; }
        public required DateTime WorkDate { get; init; }
    }

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<HRMContext>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(SeederName);

        long companyRowId;
        long? employeeRoleId;
        Dictionary<string, long> orgIdByCode;
        List<OrgPlan> orgPlans;
        List<EmpPlan> empPlans;

        await using (var ctx = await factory.CreateDbContextAsync())
        {
            // ---- One-shot guard --------------------------------------------
            if (await ctx.Hremployee.AnyAsync(e => e.companyid == CompanyId))
            {
                logger.LogInformation("AUTOX demo data already present (HREMPLOYEE companyid={CompanyId}) — seeder skipped.", CompanyId);
                return;
            }

            logger.LogInformation("AUTOX demo seed starting: {Total} employees, fixed seed {Seed}.", TotalEmployees, FixedSeed);

            // ---- Phase 1: com_company display row --------------------------
            // com_company.code lives in a DIFFERENT id space than
            // HREMPLOYEE.companyid (per CLAUDE.md the two never join — the
            // existing employees use companyid '001' while com_company has
            // only code 'AD'). The row here exists because sc_user.company_id
            // is a NOT NULL FK into com_company (every existing sc_user
            // points at row id=1 'AD'), and so AUTOX shows up in any picker
            // that lists com_company.
            var company = await ctx.com_companies.FirstOrDefaultAsync(c => c.code == CompanyId);
            if (company is null)
            {
                company = new com_company
                {
                    code = CompanyId,
                    name = "บริษัท ออโต้เอ็กซ์ จำกัด",
                    name_en = "AutoX Co., Ltd.",
                    abbr = CompanyId,
                    amount_emp = TotalEmployees,
                    isActive = true,
                    VatRegistered = false,
                    moddate = Anchor,
                    modby = SeederName,
                    remark = "บริษัทตัวอย่างสำหรับเดโม (สร้างโดย AutoxDemoSeeder — ข้อมูลสมมติทั้งหมด)",
                };
                ctx.com_companies.Add(company);
                await ctx.SaveChangesAsync();
                logger.LogInformation("AUTOX seed: com_company row created (id={Id}).", company.id);
            }
            companyRowId = company.id;

            // ---- Phase 2: plan org tree + employees in memory --------------
            // Root orgcodefull prefix: existing top-level rows own "01"
            // (CEO Office); AUTOX takes the next free 2-digit slot. If a
            // previous partial run already wrote the AUTOX root, reuse its
            // prefix so orgcodefull values stay consistent.
            var existingAutoxOrgs = await ctx.com_organizations
                .Where(o => o.comp_code == CompanyId)
                .ToListAsync();

            string rootPrefix;
            var existingRoot = existingAutoxOrgs.FirstOrDefault(o => o.code == "AX");
            if (existingRoot?.orgcodefull is { Length: >= 2 } persisted)
            {
                rootPrefix = persisted[..2];
            }
            else
            {
                var topFulls = await ctx.com_organizations
                    .Where(o => o.istop && o.orgcodefull != null)
                    .Select(o => o.orgcodefull!)
                    .ToListAsync();
                var next = 1;
                foreach (var f in topFulls)
                    if (f.Length >= 2 && int.TryParse(f[..2], out var n) && n >= next)
                        next = n + 1;
                rootPrefix = next.ToString("00");
            }

            (orgPlans, empPlans) = BuildPlans(rootPrefix);

            // ---- Phase 3: insert org tree ----------------------------------
            var existingCodes = existingAutoxOrgs.Select(o => o.code).ToHashSet();
            var newOrgs = new List<com_organization>();
            foreach (var o in orgPlans)
            {
                if (existingCodes.Contains(o.Code)) continue;
                newOrgs.Add(new com_organization
                {
                    code = o.Code,
                    name = o.Name,
                    layer_code = o.Depth.ToString(),                    // existing tree: "1"/"2"/"3" per depth
                    parent_code = o.ParentCode,                         // linked by code string, like existing rows
                    node_level = o.Depth == 1 ? null : o.Depth - 1,     // existing: root NULL, children 1,2,...
                    istop = o.Depth == 1,
                    isCompany = o.Depth == 1,                           // existing root CEO Office has isCompany=1
                    orgcodefull = o.OrgFull,
                    approver_empid = o.ApproverEmpNo,                   // vertical-approval anchor (workflow engine)
                    approver_name = o.ApproverName,
                    comp_code = CompanyId,                              // marker so AUTOX org rows are identifiable/wipeable
                    isActive = true,
                    createdate = Anchor,
                    createby = SeederName,
                });
            }
            if (newOrgs.Count > 0)
            {
                ctx.com_organizations.AddRange(newOrgs);
                await ctx.SaveChangesAsync();
            }
            logger.LogInformation("AUTOX seed: org tree ready ({New} inserted, {Existing} pre-existing).", newOrgs.Count, existingAutoxOrgs.Count);

            orgIdByCode = existingAutoxOrgs.Concat(newOrgs).ToDictionary(o => o.code!, o => o.id);

            // ---- Phase 4: position ladder ----------------------------------
            // pos_position.id / Pos_ExecType.Id are real IDENTITY columns
            // (verified via sys.columns); pos_position.posid is a plain
            // NOT NULL int with no identity, so it is supplied explicitly.
            var existingPosCodes = await ctx.pos_positions
                .Where(p => p.pos_code.StartsWith("A0"))
                .Select(p => p.pos_code)
                .ToListAsync();
            for (var i = 0; i < Positions.Length; i++)
            {
                var (code, name, nameEn, min, max) = Positions[i];
                if (existingPosCodes.Contains(code)) continue;
                ctx.pos_positions.Add(new pos_position
                {
                    pos_code = code,
                    code = i + 1,
                    posid = 9000 + i + 1,          // NOT an identity column — explicit value, 9xxx to stay clear of anything future
                    name = name,
                    engname = nameEn,
                    min_salary = min,
                    max_salary = max,
                    normal_salary = (min + max) / 2f,
                    salary_level = (i + 1).ToString(),
                    is_boss = i >= 2 ? "Y" : "N",
                });
            }

            var existingExecCodes = await ctx.Pos_ExecTypes
                .Where(t => t.CompanyId == CompanyId)
                .Select(t => t.Code)
                .ToListAsync();
            for (var i = 0; i < Positions.Length; i++)
            {
                var (code, name, nameEn, _, _) = Positions[i];
                if (existingExecCodes.Contains(code)) continue;
                ctx.Pos_ExecTypes.Add(new Pos_ExecType
                {
                    CompanyId = CompanyId,
                    Code = code,
                    Name = name,
                    NameEn = nameEn,
                    IsBoss = i >= 2,
                    IsTopLevel = code == "A07",
                    IsActive = true,
                    CreateDate = Anchor,
                    CreateBy = SeederName,
                });
            }
            await ctx.SaveChangesAsync();
            logger.LogInformation("AUTOX seed: position ladder ready ({Count} levels).", Positions.Length);

            // Same role existing employee accounts hold (sc_user 26 / empid
            // '008' → role name "Employee"). Looked up by name, never
            // hardcoded id.
            employeeRoleId = await ctx.sc_roles
                .Where(r => r.name == "Employee" && r.isactive)
                .Select(r => (long?)r.roleid)
                .FirstOrDefaultAsync();
            if (employeeRoleId is null)
                logger.LogWarning("AUTOX seed: sc_role 'Employee' not found — sc_user rows will be created WITHOUT a role link.");
        }

        // ---- Phase 5: 7,000 HREMPLOYEE rows (batched, fresh context per batch)
        var inserted = 0;
        foreach (var chunk in empPlans.Chunk(BatchSize))
        {
            await using var ctx = await factory.CreateDbContextAsync();
            ctx.Hremployee.AddRange(chunk.Select(p => new Hremployee
            {
                companyid = CompanyId,
                EmpNo = p.EmpNo,
                PrenameCode = p.Prename,
                EmpName = p.FirstName,
                EmpSurname = p.LastName,
                EmptypeCode = "01",                     // same value existing employee rows use
                PosCode = p.PosCode,
                OrganizationId = orgIdByCode[p.OrgCode],
                orgcode = p.OrgCode,                    // workflow engine's vertical-approval anchor
                orgcodefull = p.OrgFull,                // subtree filtering via LIKE 'prefix%'
                Sex = p.Sex,
                BirthDate = p.BirthDate,
                WorkDate = p.WorkDate,
                SalaryAmt = p.Salary,
                IsActive = true,
                // DeptgrpCode deliberately NULL — legacy broken linkage,
                // modern rows (EMP_NO 002) leave it NULL too.
            }));
            await ctx.SaveChangesAsync();
            inserted += chunk.Length;
            if (inserted % 1000 == 0 || inserted == empPlans.Count)
                logger.LogInformation("AUTOX seed: {Inserted}/{Total} HREMPLOYEE rows inserted.", inserted, empPlans.Count);
        }

        // ---- Phase 6: one sc_user (+Employee role) per employee ------------
        // Mirrors sc_user 26 (empid '008'). empid = Hremployee.EmpNo is the
        // bridge ScUserClaimsPrincipalFactory ("empno" claim) and
        // PayrollCompanyResolver (payroll_company claim → companyid 'AUTOX')
        // resolve through; loginname = EmpNo is what LoginEndpoints looks up.
        // No password and no ApplicationUser — a login is linked later, per
        // account, via UserProvisioningService/LinkIdentityAccount.
        inserted = 0;
        foreach (var chunk in empPlans.Chunk(BatchSize))
        {
            await using var ctx = await factory.CreateDbContextAsync();
            foreach (var p in chunk)
            {
                var user = new sc_user
                {
                    company_id = companyRowId,
                    firstname = p.FirstName,
                    lastname = p.LastName,
                    loginname = p.EmpNo,
                    empid = p.EmpNo,
                    password = null,                     // no login until explicitly provisioned
                    isdisable = false,
                    iscancel = false,
                    isActivate = true,
                    isroot = false,
                    isforcechanged = true,
                    // isEmployee left NULL — every existing sc_user row has NULL
                    moddate = Anchor,
                    modby = SeederName,
                };
                if (employeeRoleId is long roleId)
                {
                    user.sc_user_roles.Add(new sc_user_role
                    {
                        roleid = roleId,
                        isactive = true,
                        empid = p.EmpNo,
                        modate = Anchor,
                        modby = SeederName,
                    });
                }
                ctx.sc_users.Add(user);
            }
            await ctx.SaveChangesAsync();
            inserted += chunk.Length;
            if (inserted % 1000 == 0 || inserted == empPlans.Count)
                logger.LogInformation("AUTOX seed: {Inserted}/{Total} sc_user rows inserted.", inserted, empPlans.Count);
        }

        logger.LogInformation(
            "AUTOX demo seed finished: {Orgs} org units, {Positions} position levels, {Employees} employees, {Users} sc_user rows.",
            orgPlans.Count, Positions.Length, empPlans.Count, empPlans.Count);
    }

    // -------------------------------------------------------------------------
    // Deterministic in-memory plan: same seed + same code path = same 7,000
    // people every run. All Random draws happen here, in one fixed order.
    // -------------------------------------------------------------------------
    private static (List<OrgPlan> Orgs, List<EmpPlan> Emps) BuildPlans(string rootPrefix)
    {
        var rand = new Random(FixedSeed);
        var orgs = new List<OrgPlan>();
        var emps = new List<EmpPlan>();

        // ---- Org tree -------------------------------------------------------
        var root = new OrgPlan
        {
            Code = "AX",
            Name = "บริษัท ออโต้เอ็กซ์ จำกัด (สำนักงานใหญ่)",
            ParentCode = null,
            Depth = 1,
            OrgFull = rootPrefix,
        };
        orgs.Add(root);

        var divisionPlans = new List<OrgPlan>();
        var departmentPlans = new List<OrgPlan>();
        var sectionPlans = new List<OrgPlan>();

        for (var d = 0; d < Divisions.Length; d++)
        {
            var div = new OrgPlan
            {
                Code = $"AX-{Divisions[d].Code}",
                Name = Divisions[d].Name,
                ParentCode = root.Code,
                Depth = 2,
                OrgFull = rootPrefix + (d + 1).ToString("00"),
            };
            orgs.Add(div);
            divisionPlans.Add(div);

            for (var p = 0; p < DepartmentNames[d].Length; p++)
            {
                var dept = new OrgPlan
                {
                    Code = $"{div.Code}-{p + 1:00}",
                    Name = DepartmentNames[d][p],
                    ParentCode = div.Code,
                    Depth = 3,
                    OrgFull = div.OrgFull + (p + 1).ToString("00"),
                };
                orgs.Add(dept);
                departmentPlans.Add(dept);

                for (var s = 0; s < SectionsPerDept; s++)
                {
                    var section = new OrgPlan
                    {
                        Code = $"{dept.Code}-{s + 1}",
                        Name = $"หน่วยที่ {s + 1} {dept.Name}",
                        ParentCode = dept.Code,
                        Depth = 4,
                        OrgFull = dept.OrgFull + (s + 1).ToString("00"),
                    };
                    orgs.Add(section);
                    sectionPlans.Add(section);
                }
            }
        }

        // ---- Section staffing sizes ----------------------------------------
        // 7,000 total − 157 heads (1 CEO + 6 division heads + 30 dept
        // managers + 120 section heads) = 6,843 rank-and-file, kept inside
        // 40–70 per section (incl. its head) by starting near the mean and
        // shuffling headcount pairwise.
        var managerCount = 1 + divisionPlans.Count + departmentPlans.Count + sectionPlans.Count;
        var staffTotal = TotalEmployees - managerCount;
        var sizes = new int[sectionPlans.Count];
        var baseSize = staffTotal / sectionPlans.Count;
        var remainder = staffTotal % sectionPlans.Count;
        for (var i = 0; i < sizes.Length; i++)
            sizes[i] = baseSize + (i < remainder ? 1 : 0);
        for (var k = 0; k < 4000; k++)
        {
            int i = rand.Next(sizes.Length), j = rand.Next(sizes.Length);
            if (i != j && sizes[i] < 66 && sizes[j] > 42) { sizes[i]++; sizes[j]--; }
        }

        // ---- People ---------------------------------------------------------
        var empNoSeq = 0;
        string NextEmpNo() => $"AX{++empNoSeq:0000}";   // 6 chars — matches EMP_NO nvarchar(6)

        EmpPlan MakePerson(string posCode, OrgPlan org, int minAge, int maxAge, int minTenureYears)
        {
            var male = rand.Next(2) == 0;
            var first = male ? MaleFirstNames[rand.Next(MaleFirstNames.Length)]
                             : FemaleFirstNames[rand.Next(FemaleFirstNames.Length)];
            var last = Surnames[rand.Next(Surnames.Length)];
            var prename = male ? "1" : (rand.Next(2) == 0 ? "2" : "3");

            var ageYears = rand.Next(minAge, maxAge + 1);
            var birth = Anchor.AddYears(-ageYears).AddDays(-rand.Next(0, 365));

            var tenureDays = rand.Next(Math.Max(minTenureYears * 365, 30), 15 * 365 + 1);
            var workDate = Anchor.AddDays(-tenureDays);

            var band = Positions.First(x => x.Code == posCode);
            var salary = rand.Next(band.MinSalary / 100, band.MaxSalary / 100 + 1) * 100m;

            return new EmpPlan
            {
                EmpNo = NextEmpNo(),
                FirstName = first,
                LastName = last,
                Sex = male ? "M" : "F",
                Prename = prename,
                PosCode = posCode,
                OrgCode = org.Code,
                OrgFull = org.OrgFull,
                Salary = salary,
                BirthDate = birth,
                WorkDate = workDate,
            };
        }

        void MakeHead(string posCode, OrgPlan org, int minAge, int maxAge, int minTenureYears)
        {
            var head = MakePerson(posCode, org, minAge, maxAge, minTenureYears);
            org.ApproverEmpNo = head.EmpNo;
            org.ApproverName = $"{head.FirstName} {head.LastName}";
            emps.Add(head);
        }

        // AX0001: CEO at HQ — root org's approver, top of every vertical chain.
        MakeHead("A07", root, 52, 57, 10);

        // AX0002–AX0007: division heads — first three divisions get a
        // ผู้อำนวยการฝ่าย (A06), the rest a ผู้จัดการฝ่าย (A05).
        for (var d = 0; d < divisionPlans.Count; d++)
            MakeHead(d < 3 ? "A06" : "A05", divisionPlans[d], 45, 57, 8);

        // AX0008–AX0037: department managers.
        foreach (var dept in departmentPlans)
            MakeHead("A04", dept, 35, 54, 5);

        // AX0038–AX0157: section heads (หัวหน้างาน) — these are the
        // approver_empid of their own section, so the workflow engine's
        // vertical approval resolves a real person for every employee.
        foreach (var section in sectionPlans)
            MakeHead("A03", section, 28, 49, 3);

        // AX0158–AX7000: rank-and-file (≈30% senior).
        for (var s = 0; s < sectionPlans.Count; s++)
            for (var n = 0; n < sizes[s]; n++)
            {
                var senior = rand.Next(100) < 30;
                emps.Add(MakePerson(senior ? "A02" : "A01", sectionPlans[s], senior ? 25 : 20, senior ? 45 : 44, 0));
            }

        return (orgs, emps);
    }
}
