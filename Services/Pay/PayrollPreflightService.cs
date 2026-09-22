using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Pay;

// Phase B: everything HR should know BEFORE pressing "คำนวณ", in one report —
// so missing/wrong data gets chased and unapproved items get approved first,
// instead of being discovered (or silently dropped) after a 7,000-employee run.
//
//  1. Config errors  — company-level setup whose absence makes CalculateAsync
//     THROW (the "No Hrucfsecurity rate configured" crash the CEO hit in a demo,
//     missing tax brackets for the year, missing pay-item codes the engine
//     indexes by key).
//  2. Employee issues — Error = the run cannot be correct/paid for this person
//     (no base salary, no bank account); Warning = worth a look (no bank code,
//     no cost centre, no position).
//  3. Approval checklist — period items that exist but are NOT approved yet and
//     would therefore be excluded from the run: Pay_AdhocPayItem still Pending
//     (the engine only picks up Approved), and OT requests in the period with
//     no approval date. This is the "checklist ว่าต้องอนุมัติอะไรก่อนประมวลผล".
//  4. Holds — employees HR parked for this run with a reason (skipped by the
//     engine; see PayrollCalculationService), listed for transparency/release.
// Uses the SAME eligibility predicate as the engine so the two never disagree.
public class PayrollPreflightService(IDbContextFactory<HRMContext> dbFactory)
{
    // Info = left out on purpose (e.g. an employee type not paid by payroll) — shown, never blocking.
    public enum Severity { Error, Warning, Info }

    public record Issue(long HremployeeId, string EmpNo, string Name, Severity Severity, string Code, string Message);
    public record PendingItem(string Kind, long? HremployeeId, string EmpNo, string Name, string Detail);
    public record Hold(long Id, long HremployeeId, string EmpNo, string Name, string Reason, DateTime HeldDate);

    public record Report(int EligibleCount, int HeldCount, List<string> ConfigErrors, List<Issue> Issues, List<PendingItem> PendingApprovals, List<Hold> Holds)
    {
        public int ErrorCount => ConfigErrors.Count + Issues.Count(i => i.Severity == Severity.Error);
        public int WarningCount => Issues.Count(i => i.Severity == Severity.Warning);
        public bool ReadyToCalculate => ErrorCount == 0 && PendingApprovals.Count == 0;
    }

    // ตรวจก่อนประมวลผล = ด่าน ไม่ใช่รายงานให้อ่านเล่น (CEO, 20 ก.ย. 2569) — เดิมกดคำนวณตรง ๆ ได้
    // โดยไม่เคยกดตรวจ คนที่ค่าจ้างต่ำกว่าขั้นต่ำ/ไม่มีเงินเดือน/วันที่ผิด จึงไหลไปถึงไฟล์โอนเงินและ ภ.ง.ด.1
    // ทางออกของผู้ใช้มีสองทางเสมอ: แก้ข้อมูลของคนนั้น หรือ "พักการจ่าย" คนนั้นไว้รอบนี้ (hold)
    // ที่เดียวที่บังคับ ใช้ร่วมกันทั้งคำนวณตรง ๆ คำนวณเบื้องหลัง และส่งอนุมัติ (พอร์ตจาก Advance.Payroll)
    public async Task EnsureClearAsync(long runId, string action, CancellationToken ct = default)
    {
        var report = await CheckAsync(runId, ct);
        if (report.ErrorCount == 0) return;

        var lines = report.ConfigErrors
            .Concat(report.Issues.Where(i => i.Severity == Severity.Error).Select(i => $"{i.EmpNo} {i.Name}: {i.Message}"))
            .Take(5)
            .ToList();
        var more = report.ErrorCount - lines.Count;
        throw new InvalidOperationException(
            $"ตรวจก่อนประมวลผลพบข้อผิดพลาด {report.ErrorCount} รายการ จึง{action}ไม่ได้ — แก้ข้อมูล หรือพักการจ่ายคนนั้นไว้ก่อน:"
            + Environment.NewLine + string.Join(Environment.NewLine, lines.Select(l => "• " + l))
            + (more > 0 ? Environment.NewLine + $"• และอีก {more} รายการ — ดูทั้งหมดที่ปุ่ม \"ตรวจสอบข้อมูลก่อนประมวลผล\"" : ""));
    }

    // Pay-item codes CalculateAsync indexes with payItemTypes["X"] — a missing one
    // is a KeyNotFoundException mid-run, so surface it here instead.
    private static readonly string[] RequiredPayItemCodes = { "BASE", "OT", "ALLOWANCE", "SSO", "PF", "INSURANCE", "WELFAREFUND", "LOAN", "TAX", "LATE", "ABSENT", "SAL_ADVANCE" };

    public async Task<Report> CheckAsync(long runId, CancellationToken ct = default)
    {
        await using var ctx = await dbFactory.CreateDbContextAsync(ct);
        var run = await ctx.Pay_PayrollRuns.FirstOrDefaultAsync(r => r.Id == runId, ct)
            ?? throw new InvalidOperationException($"ไม่พบรอบเงินเดือน #{runId}");

        var periodEndDt = run.PeriodEnd.ToDateTime(TimeOnly.MaxValue);
        var periodStartDt = run.PeriodStart.ToDateTime(TimeOnly.MinValue);

        // ---- company-level config that would crash the engine ----
        var configErrors = new List<string>();
        if (!await ctx.Hrucfsecuritys.AnyAsync(x => x.companyid == run.CompanyId && x.SecurityCode == HrucfsecurityRateProvider.CurrentEmployeeSecurityCode, ct))
            configErrors.Add($"ยังไม่ได้ตั้งค่าอัตราประกันสังคม (Hrucfsecurity code 01) ของบริษัท {run.CompanyId} — คำนวณจะล้มเหลวทันที");
        if (!await ctx.Pay_TaxBrackets.AnyAsync(b => b.EffectiveYear == run.PeriodStart.Year && b.IsActive, ct))
            configErrors.Add($"ไม่มีตารางอัตราภาษีปี {run.PeriodStart.Year} (Pay_TaxBracket) — ภาษีหัก ณ ที่จ่ายจะคำนวณไม่ได้");
        var codes = await ctx.Pay_PayItemTypes.Select(t => t.Code).ToListAsync(ct);
        var missingCodes = RequiredPayItemCodes.Where(c => !codes.Contains(c)).ToList();
        if (missingCodes.Count > 0)
            configErrors.Add("ไม่มีประเภทรายการเงินได้/เงินหักที่เครื่องคำนวณต้องใช้: " + string.Join(", ", missingCodes));
        var paidByOldSystem = await OpeningBalanceGuard.OverlappingEmpNosAsync(ctx, run, ct);
        if (paidByOldSystem.Count > 0)
            configErrors.Add(OpeningBalanceGuard.Message(run, paidByOldSystem));

        // ---- eligible employees (same predicate as the engine) minus holds ----
        var holdsRaw = await ctx.Pay_PayrollRunHolds.Where(h => h.PayrollRunId == runId && h.IsActive).ToListAsync(ct);
        var heldIds = holdsRaw.Select(h => h.HremployeeId).ToHashSet();

        var nonPayrollTypes = await PayrollEligibility.LoadNonPayrollTypeIdsAsync(ctx, run.CompanyId, ct);
        var typeNames = await ctx.Pos_EmployeeTypes
            .Where(t => t.CompanyId == run.CompanyId)
            .ToDictionaryAsync(t => t.Id, t => t.Name ?? t.Code ?? $"#{t.Id}", ct);
        var eligible = await ctx.Hremployee
            .Where(PayrollEligibility.InPeriod(run.CompanyId, periodStartDt, periodEndDt, nonPayrollTypes))
            .Select(e => new { e.id, e.EmpNo, e.EmpName, e.EmpSurname, e.SalaryAmt, e.DailyWage, e.SalexpAccid, e.SalexpBank, e.CostCenterCode, e.PosExecTypeId })
            .ToListAsync(ct);

        var issues = new List<Issue>();

        // ---- leaver final pay: only its members · regular: members of this period's final-pay runs are out ----
        if (run.RunType == PayrollRunType.FinalPay)
        {
            var members = await ctx.Pay_PayrollRunMembers.Where(m => m.PayrollRunId == run.Id).ToListAsync(ct);
            if (members.Count == 0)
                configErrors.Add("รอบจ่ายคนออกยังไม่มีพนักงาน — เพิ่มพนักงานที่ออกจากหน้าพนักงาน (ส่วน \"จ่ายเงินคนออก\")");
            var memberIds = members.Select(m => m.HremployeeId).ToHashSet();
            foreach (var m in members.Where(m => eligible.All(e => e.id != m.HremployeeId)))
                issues.Add(new Issue(m.HremployeeId, m.EmpNo, m.EmpNo, Severity.Error, "MEMBER_NOT_ELIGIBLE",
                    "อยู่ในรอบนี้แต่ไม่เข้าเงื่อนไขรับเงินงวดนี้ (วันที่ออกไม่อยู่ในงวด / ถูกปิดสถานะ / ประเภทไม่รับเงินเดือน) — จะไม่ถูกคำนวณ"));
            eligible = eligible.Where(e => memberIds.Contains(e.id)).ToList();
        }
        else
        {
            var settled = await PayrollEligibility.PaidInFinalPayRunAsync(ctx, run, ct);
            foreach (var e in eligible.Where(e => settled.ContainsKey(e.id)))
                issues.Add(new Issue(e.id, e.EmpNo, $"{e.EmpName} {e.EmpSurname}".Trim(), Severity.Info, "PAID_IN_FINAL_PAY",
                    $"จ่ายแล้วในรอบจ่ายคนออก #{settled[e.id]} — ไม่อยู่ในรอบนี้"));
            if (settled.Count > 0) eligible = eligible.Where(e => !settled.ContainsKey(e.id)).ToList();
        }

        // ---- employees left out of the run by hire/leave date or status (never silently) ----
        var isFinalPay = run.RunType == PayrollRunType.FinalPay;   // a leaver run: the rest of the company is not its business
        var excluded = await ctx.Hremployee
            .Where(PayrollEligibility.ExcludedButRelevant(run.CompanyId, periodStartDt, periodEndDt, nonPayrollTypes))
            .Where(e => !isFinalPay)
            .Select(e => new { e.id, e.EmpNo, e.EmpName, e.EmpSurname, e.IsActive, e.WorkDate, e.ResignDate, e.EmployeeTypeId })
            .ToListAsync(ct);
        foreach (var e in excluded)
        {
            var name = $"{e.EmpName} {e.EmpSurname}".Trim();
            switch (PayrollEligibility.WhyExcluded(e.IsActive, e.WorkDate, e.ResignDate, e.EmployeeTypeId, nonPayrollTypes))
            {
                case PayrollEligibility.Reason.NotPayrollType:
                    issues.Add(new Issue(e.id, e.EmpNo, name, Severity.Info, "NOT_PAYROLL_TYPE",
                        $"ประเภท \"{typeNames.GetValueOrDefault(e.EmployeeTypeId!.Value, "?")}\" ไม่รับเงินเดือนผ่านระบบ — ไม่อยู่ในรอบนี้ (ตั้งค่าที่ประเภทพนักงาน)"));
                    break;
                case PayrollEligibility.Reason.NoHireDate:
                    issues.Add(new Issue(e.id, e.EmpNo, name, Severity.Error, "NO_HIRE_DATE", "ไม่มีวันเริ่มงาน — จะไม่ถูกจ่ายเงินเดือนในรอบนี้"));
                    break;
                case PayrollEligibility.Reason.ResignBeforeHire:
                    issues.Add(new Issue(e.id, e.EmpNo, name, Severity.Error, "RESIGN_BEFORE_HIRE",
                        $"วันที่ออก {e.ResignDate:dd/MM/yyyy} อยู่ก่อนวันเริ่มงาน {e.WorkDate:dd/MM/yyyy} — ข้อมูลผิด จะไม่ถูกจ่าย"));
                    break;
                case PayrollEligibility.Reason.Inactive:
                    issues.Add(new Issue(e.id, e.EmpNo, name, Severity.Warning, "INACTIVE",
                        "ถูกปิดสถานะ (IsActive) — ไม่ถูกจ่ายในรอบนี้ ถ้าต้องจ่ายให้เปิดสถานะก่อนคำนวณ"));
                    break;
            }
        }
        // ค่าจ้างขั้นต่ำ: ตารางว่าง = ไม่ตรวจ (ต้องมีคนตั้งค่าที่ /pay/admin/minimum-wage) — ห้ามจ่ายต่ำกว่าที่กฎหมายกำหนด
        var minimumWageRows = await ctx.Pay_MinimumWages.AsNoTracking().Where(m => m.IsActive).ToListAsync(ct);
        var workProvince = await ctx.Pay_PayslipSettings.AsNoTracking()
            .Where(s => s.CompanyId == run.CompanyId).Select(s => s.WorkProvince).FirstOrDefaultAsync(ct);
        var dailyMinimum = MinimumWageRule.DailyMinimum(minimumWageRows, workProvince, run.PeriodStart);

        foreach (var e in eligible.Where(e => !heldIds.Contains(e.id)))
        {
            var name = $"{e.EmpName} {e.EmpSurname}".Trim();
            if ((e.SalaryAmt ?? 0m) <= 0m && (e.DailyWage ?? 0m) <= 0m)
                issues.Add(new Issue(e.id, e.EmpNo, name, Severity.Error, "NO_SALARY", "ไม่มีเงินเดือนฐานและไม่มีค่าจ้างรายวัน — คำนวณได้ 0 บาท"));
            else if (MinimumWageRule.IsBelow(e.SalaryAmt, e.DailyWage, dailyMinimum))
                issues.Add(new Issue(e.id, e.EmpNo, name, Severity.Error, "BELOW_MIN_WAGE",
                    $"ค่าจ้างต่ำกว่าค่าจ้างขั้นต่ำ — ได้ {MinimumWageRule.DailyEquivalent(e.SalaryAmt, e.DailyWage):N2} บาท/วัน ขั้นต่ำ {dailyMinimum:N2} บาท/วัน" +
                    (string.IsNullOrWhiteSpace(workProvince) ? "" : $" (จังหวัด{workProvince})")));
            if (string.IsNullOrWhiteSpace(e.SalexpAccid))
                issues.Add(new Issue(e.id, e.EmpNo, name, Severity.Warning, "NO_BANK_ACCOUNT", "ไม่มีเลขบัญชีธนาคาร — คำนวณได้ แต่ต้องเติมก่อนทำไฟล์โอนเงิน"));
            else if (string.IsNullOrWhiteSpace(e.SalexpBank))
                issues.Add(new Issue(e.id, e.EmpNo, name, Severity.Warning, "NO_BANK_CODE", "มีเลขบัญชีแต่ไม่ระบุธนาคาร"));
            if (string.IsNullOrWhiteSpace(e.CostCenterCode))
                issues.Add(new Issue(e.id, e.EmpNo, name, Severity.Warning, "NO_COST_CENTER", "ไม่มีศูนย์ต้นทุน — GL/รายงานต้นทุนจะไม่มีที่ลง"));
            if (e.PosExecTypeId is null)
                issues.Add(new Issue(e.id, e.EmpNo, name, Severity.Warning, "NO_POSITION", "ไม่มีตำแหน่ง — เบี้ยตามตำแหน่งจะไม่ถูกจ่าย"));
        }

        var nameById = eligible.ToDictionary(e => e.id, e => (e.EmpNo, Name: $"{e.EmpName} {e.EmpSurname}".Trim()));

        // ---- approval checklist: exists for the period but NOT yet approved ----
        var pending = new List<PendingItem>();
        var pendingAdhoc = await ctx.Pay_AdhocPayItems
            .Include(a => a.Pay_PayItemType)
            .Where(a => a.TargetPeriod == run.PayrollPeriod && a.Status == PayAdhocItemStatus.Pending
                        && (run.RunType == PayrollRunType.Bonus ? a.TargetRunType == PayrollRunType.Bonus
                            : run.RunType == PayrollRunType.FinalPay ? a.TargetRunType == PayrollRunType.Regular || a.TargetRunType == PayrollRunType.FinalPay
                            : a.TargetRunType != PayrollRunType.Bonus && a.TargetRunType != PayrollRunType.FinalPay))
            .ToListAsync(ct);
        foreach (var a in pendingAdhoc)
        {
            if (!nameById.TryGetValue(a.HremployeeId, out var who)) continue; // belongs to another company
            pending.Add(new PendingItem("รายการเฉพาะกิจ", a.HremployeeId, who.EmpNo, who.Name,
                $"{a.Pay_PayItemType?.NameTh ?? "รายการ"} {a.Amount:N2} บาท — {a.Reason} (ยังไม่อนุมัติ → จะไม่ถูกนำมาคำนวณ)"));
        }
        var pendingOt = await ctx.emp_overtime_requests
            .Where(o => o.companyid == run.CompanyId && o.approvedate == null
                        && o.starttime >= periodStartDt && o.starttime <= periodEndDt)
            .Select(o => new { o.hremployeeid, o.empid, o.starttime, o.workhour })
            .ToListAsync(ct);
        foreach (var o in pendingOt)
        {
            var who = o.hremployeeid is long hid && nameById.TryGetValue(hid, out var w) ? w : (EmpNo: o.empid, Name: o.empid);
            pending.Add(new PendingItem("OT", o.hremployeeid, who.EmpNo, who.Name,
                $"OT วันที่ {o.starttime:dd/MM/yyyy} {o.workhour:0.##} ชม. ยังไม่อนุมัติ → จะไม่ถูกนำมาคำนวณ"));
        }

        var pendingAdvances = await ctx.Pay_SalaryAdvances
            .Where(a => a.CompanyId == run.CompanyId && a.TargetPeriod == run.PayrollPeriod && a.Status == PaySalaryAdvanceStatus.Pending)
            .ToListAsync(ct);
        foreach (var a in pendingAdvances)
        {
            var who = nameById.TryGetValue(a.HremployeeId, out var w) ? w : (EmpNo: a.EmpNo ?? "?", Name: a.EmpNo ?? "?");
            pending.Add(new PendingItem("เบิกล่วงหน้า", a.HremployeeId, who.EmpNo, who.Name,
                $"เบิกเงินเดือนล่วงหน้า {a.Amount:N2} บาท — {a.Reason} (ยังไม่อนุมัติ → จะไม่ถูกหักคืนในรอบนี้)"));
        }

        // ---- holds (with names) ----
        var holds = holdsRaw.Select(h =>
        {
            var who = nameById.TryGetValue(h.HremployeeId, out var w) ? w : (EmpNo: h.EmpNo ?? "?", Name: h.EmpNo ?? "?");
            return new Hold(h.Id, h.HremployeeId, who.EmpNo, who.Name, h.Reason, h.HeldDate);
        }).OrderByDescending(h => h.HeldDate).ToList();

        return new Report(eligible.Count - heldIds.Count, heldIds.Count, configErrors,
            issues.OrderBy(i => i.Severity).ThenBy(i => i.EmpNo).ToList(),
            pending.OrderBy(p => p.Kind).ThenBy(p => p.EmpNo).ToList(),
            holds);
    }

    public async Task HoldAsync(long runId, long hremployeeId, string reason, long actorUserId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new InvalidOperationException("ต้องระบุเหตุผลการ hold");
        await using var ctx = await dbFactory.CreateDbContextAsync(ct);
        if (await ctx.Pay_PayrollRunHolds.AnyAsync(h => h.PayrollRunId == runId && h.HremployeeId == hremployeeId && h.IsActive, ct))
            return; // already held — idempotent
        var empNo = await ctx.Hremployee.Where(e => e.id == hremployeeId).Select(e => e.EmpNo).FirstOrDefaultAsync(ct);
        ctx.Pay_PayrollRunHolds.Add(new Pay_PayrollRunHold
        {
            PayrollRunId = runId, HremployeeId = hremployeeId, EmpNo = empNo,
            Reason = reason.Trim(), HeldByUserId = actorUserId, HeldDate = DateTime.Now, IsActive = true,
        });
        await ctx.SaveChangesAsync(ct);
    }

    // Convenience for the page's "hold by employee number" box.
    public async Task<bool> HoldByEmpNoAsync(long runId, string companyId, string empNo, string reason, long actorUserId, CancellationToken ct = default)
    {
        await using var ctx = await dbFactory.CreateDbContextAsync(ct);
        var id = await ctx.Hremployee.Where(e => e.companyid == companyId && e.EmpNo == empNo.Trim()).Select(e => (long?)e.id).FirstOrDefaultAsync(ct);
        if (id is null) return false;
        await HoldAsync(runId, id.Value, reason, actorUserId, ct);
        return true;
    }

    public async Task ReleaseAsync(long holdId, long actorUserId, CancellationToken ct = default)
    {
        await using var ctx = await dbFactory.CreateDbContextAsync(ct);
        var h = await ctx.Pay_PayrollRunHolds.FirstOrDefaultAsync(x => x.Id == holdId, ct);
        if (h is null || !h.IsActive) return;
        h.IsActive = false; h.ReleasedByUserId = actorUserId; h.ReleasedDate = DateTime.Now;
        await ctx.SaveChangesAsync(ct);
    }
}
