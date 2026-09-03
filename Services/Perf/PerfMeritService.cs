using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Perf;

// Computes and applies the salary/bonus adjustment recommended by an
// approved evaluation's grade band. Writes go through
// Pay_PositionSalaryHistory (ChangeType='S', same table
// PayrollEmployeeAdmin.razor's own manual salary-edit flow writes to) so
// merit increases show up in the same "ประวัติปรับเงินเดือน" history as any
// other salary change — idempotent via Perf_EvaluationInstance.IsMeritApplied,
// mirroring Org_OrganizationChangeRequest.IsApplied. Never auto-applied: HR
// must open the instance and click Apply explicitly, matching the
// severance/insurance-auto-enroll precedent throughout this codebase.
public class PerfMeritService(IDbContextFactory<HRMContext> dbFactory)
{
    public record MeritPreview(
        long InstanceId, string Grade, decimal FinalScorePercent,
        decimal CurrentSalary, decimal SalaryIncreasePercent, decimal RecommendedNewSalary,
        decimal BonusPercent, decimal RecommendedBonusAmount, bool RequiresJustification, bool AlreadyApplied,
        // AutoX #5: the effective raise = grade band's standard % + the per-person
        // adjustment on the instance. Both surfaced so the UI can show the split.
        decimal AdjustmentPercent, decimal EffectiveIncreasePercent);

    public async Task<MeritPreview> PreviewMeritIncreaseAsync(long instanceId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var instance = await context.Perf_EvaluationInstances.FirstOrDefaultAsync(i => i.Id == instanceId, ct)
            ?? throw new InvalidOperationException("ไม่พบรายการประเมินนี้แล้ว");
        if (instance.Status != PerfInstanceStatus.Approved)
            throw new InvalidOperationException("ดูตัวอย่างการปรับเงินเดือนได้เฉพาะรายการที่อนุมัติแล้วเท่านั้น");
        if (string.IsNullOrEmpty(instance.FinalGrade))
            throw new InvalidOperationException("รายการนี้ไม่มีเกรดที่จับคู่ได้ (คะแนนอยู่นอกช่วงเกรดทั้งหมดที่ตั้งไว้) — ไม่สามารถคำนวณการปรับเงินเดือนได้");

        var gradeBand = await context.Perf_GradeBands
            .FirstOrDefaultAsync(g => g.EvaluationPeriodId == instance.EvaluationPeriodId && g.Grade == instance.FinalGrade && g.IsActive, ct)
            ?? throw new InvalidOperationException("ไม่พบเกณฑ์เกรดนี้แล้ว (อาจถูกลบไปหลังคำนวณผล)");

        var employee = await context.Hremployee.FirstOrDefaultAsync(e => e.id == instance.HremployeeId, ct)
            ?? throw new InvalidOperationException("ไม่พบพนักงานคนนี้แล้ว");
        var currentSalary = employee.SalaryAmt ?? 0m;

        var adjust = instance.MeritAdjustmentPercent ?? 0m;
        var effectivePercent = gradeBand.SalaryIncreasePercent + adjust;
        var newSalary = Math.Round(currentSalary * (1 + effectivePercent / 100m), 2);
        var bonusAmount = Math.Round(currentSalary * (gradeBand.BonusPercent / 100m), 2);

        return new MeritPreview(instanceId, gradeBand.Grade, instance.FinalScorePercent ?? 0m,
            currentSalary, gradeBand.SalaryIncreasePercent, newSalary,
            gradeBand.BonusPercent, bonusAmount, gradeBand.RequiresJustification, instance.IsMeritApplied,
            adjust, effectivePercent);
    }

    public async Task ApplyMeritIncreaseAsync(long instanceId, long actorUserId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var instance = await context.Perf_EvaluationInstances.FirstOrDefaultAsync(i => i.Id == instanceId, ct)
            ?? throw new InvalidOperationException("ไม่พบรายการประเมินนี้แล้ว");
        if (instance.Status != PerfInstanceStatus.Approved)
            throw new InvalidOperationException("ปรับเงินเดือนได้เฉพาะรายการที่อนุมัติแล้วเท่านั้น");
        if (instance.IsMeritApplied)
            throw new InvalidOperationException("รายการนี้ถูกปรับเงินเดือนไปแล้ว กดซ้ำไม่ได้");
        if (string.IsNullOrEmpty(instance.FinalGrade))
            throw new InvalidOperationException("รายการนี้ไม่มีเกรดที่จับคู่ได้ ไม่สามารถปรับเงินเดือนได้");

        var gradeBand = await context.Perf_GradeBands
            .FirstOrDefaultAsync(g => g.EvaluationPeriodId == instance.EvaluationPeriodId && g.Grade == instance.FinalGrade && g.IsActive, ct)
            ?? throw new InvalidOperationException("ไม่พบเกณฑ์เกรดนี้แล้ว");

        var employee = await context.Hremployee.FirstOrDefaultAsync(e => e.id == instance.HremployeeId, ct)
            ?? throw new InvalidOperationException("ไม่พบพนักงานคนนี้แล้ว");

        var period = await context.Perf_EvaluationPeriods.FirstOrDefaultAsync(p => p.Id == instance.EvaluationPeriodId, ct);

        var oldSalary = employee.SalaryAmt ?? 0m;
        var adjust = instance.MeritAdjustmentPercent ?? 0m;
        var effectivePercent = gradeBand.SalaryIncreasePercent + adjust;
        var newSalary = Math.Round(oldSalary * (1 + effectivePercent / 100m), 2);

        employee.SalaryAmt = newSalary;

        var adjustText = adjust == 0m ? "" : $" + ปรับเพิ่ม {adjust:0.#}%";
        context.Pay_PositionSalaryHistories.Add(new Pay_PositionSalaryHistory
        {
            HremployeeId = employee.id,
            EmpNo = employee.EmpNo,
            CompanyId = employee.companyid,
            OldSalary = oldSalary,
            NewSalary = newSalary,
            ChangeType = "S",
            Reason = $"ปรับเงินเดือนตามผลประเมิน{(period is null ? "" : $" รอบ {period.Name}")} เกรด {gradeBand.Grade} (+{gradeBand.SalaryIncreasePercent:0.#}%{adjustText} = +{effectivePercent:0.#}%)",
            ChangedByUserId = actorUserId,
            ChangedDate = DateTime.Now,
        });

        instance.IsMeritApplied = true;
        instance.MeritAppliedDate = DateTime.Now;

        await context.SaveChangesAsync(ct);
    }
}
