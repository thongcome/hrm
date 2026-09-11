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
    public enum Severity { Error, Warning }

    public record Issue(long HremployeeId, string EmpNo, string Name, Severity Severity, string Code, string Message);
    public record PendingItem(string Kind, long? HremployeeId, string EmpNo, string Name, string Detail);
    public record Hold(long Id, long HremployeeId, string EmpNo, string Name, string Reason, DateTime HeldDate);

    public record Report(int EligibleCount, int HeldCount, List<string> ConfigErrors, List<Issue> Issues, List<PendingItem> PendingApprovals, List<Hold> Holds)
    {
        public int ErrorCount => ConfigErrors.Count + Issues.Count(i => i.Severity == Severity.Error);
        public int WarningCount => Issues.Count(i => i.Severity == Severity.Warning);
        public bool ReadyToCalculate => ErrorCount == 0 && PendingApprovals.Count == 0;
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

        // ---- eligible employees (same predicate as the engine) minus holds ----
        var holdsRaw = await ctx.Pay_PayrollRunHolds.Where(h => h.PayrollRunId == runId && h.IsActive).ToListAsync(ct);
        var heldIds = holdsRaw.Select(h => h.HremployeeId).ToHashSet();

        var eligible = await ctx.Hremployee
            .Where(e => e.companyid == run.CompanyId
                        && e.WorkDate != null && e.WorkDate <= periodEndDt
                        && (e.ResignDate == null || e.ResignDate >= periodStartDt))
            .Select(e => new { e.id, e.EmpNo, e.EmpName, e.EmpSurname, e.SalaryAmt, e.DailyWage, e.SalexpAccid, e.SalexpBank, e.CostCenterCode, e.PosCode })
            .ToListAsync(ct);

        var issues = new List<Issue>();
        foreach (var e in eligible.Where(e => !heldIds.Contains(e.id)))
        {
            var name = $"{e.EmpName} {e.EmpSurname}".Trim();
            if ((e.SalaryAmt ?? 0m) <= 0m && (e.DailyWage ?? 0m) <= 0m)
                issues.Add(new Issue(e.id, e.EmpNo, name, Severity.Error, "NO_SALARY", "ไม่มีเงินเดือนฐานและไม่มีค่าจ้างรายวัน — คำนวณได้ 0 บาท"));
            if (string.IsNullOrWhiteSpace(e.SalexpAccid))
                issues.Add(new Issue(e.id, e.EmpNo, name, Severity.Warning, "NO_BANK_ACCOUNT", "ไม่มีเลขบัญชีธนาคาร — คำนวณได้ แต่ต้องเติมก่อนทำไฟล์โอนเงิน"));
            else if (string.IsNullOrWhiteSpace(e.SalexpBank))
                issues.Add(new Issue(e.id, e.EmpNo, name, Severity.Warning, "NO_BANK_CODE", "มีเลขบัญชีแต่ไม่ระบุธนาคาร"));
            if (string.IsNullOrWhiteSpace(e.CostCenterCode))
                issues.Add(new Issue(e.id, e.EmpNo, name, Severity.Warning, "NO_COST_CENTER", "ไม่มีศูนย์ต้นทุน — GL/รายงานต้นทุนจะไม่มีที่ลง"));
            if (string.IsNullOrWhiteSpace(e.PosCode))
                issues.Add(new Issue(e.id, e.EmpNo, name, Severity.Warning, "NO_POSITION", "ไม่มีตำแหน่ง — เบี้ยตามตำแหน่งจะไม่ถูกจ่าย"));
        }

        var nameById = eligible.ToDictionary(e => e.id, e => (e.EmpNo, Name: $"{e.EmpName} {e.EmpSurname}".Trim()));

        // ---- approval checklist: exists for the period but NOT yet approved ----
        var pending = new List<PendingItem>();
        var pendingAdhoc = await ctx.Pay_AdhocPayItems
            .Include(a => a.Pay_PayItemType)
            .Where(a => a.TargetPeriod == run.PayrollPeriod && a.Status == PayAdhocItemStatus.Pending
                        && (run.RunType == PayrollRunType.Bonus ? a.TargetRunType == PayrollRunType.Bonus : a.TargetRunType != PayrollRunType.Bonus))
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
