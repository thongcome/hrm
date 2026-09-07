using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Perf;

// Facts for the formal "คำสั่งขึ้นเงินเดือน" (salary increase order) letter —
// only buildable AFTER PerfMeritService.ApplyMeritIncreaseAsync has actually
// run (the order documents a raise that happened, it doesn't preview one).
// OldSalary/NewSalary/OrderNo/OrderDate come from the exact
// Pay_PositionSalaryHistory row the merit apply created
// (Perf_EvaluationInstance.MeritSalaryHistoryId), not recomputed from the
// grade band again, so the printed document always matches what was really
// applied even if grade-band % config changes later.
public record SalaryIncreaseOrderData(
    string EmployeeName, string? EmpNo, string? PositionName, string? OrganizationName,
    string Grade, decimal? FinalScorePercent, decimal OldSalary, decimal NewSalary,
    decimal EffectiveIncreasePercent, DateTime AppliedDate, string? PeriodName,
    string? OrderNo, DateTime? OrderDate,
    string CompanyName, string? CompanyAddress);

public static class SalaryIncreaseOrderDataService
{
    public static async Task<SalaryIncreaseOrderData?> BuildAsync(HRMContext context, long instanceId, string companyId, CancellationToken ct = default)
    {
        var instance = await context.Perf_EvaluationInstances
            .Include(i => i.EvaluationPeriod)
            .FirstOrDefaultAsync(i => i.Id == instanceId, ct);
        if (instance is null || !instance.IsMeritApplied || instance.MeritSalaryHistoryId is not long historyId)
            return null;

        var history = await context.Pay_PositionSalaryHistories.FirstOrDefaultAsync(h => h.Id == historyId, ct);
        if (history is null) return null;

        var employee = await context.Hremployee.FirstOrDefaultAsync(e => e.id == instance.HremployeeId && e.companyid == companyId, ct);
        if (employee is null) return null;

        var settings = await context.Pay_PayslipSettings.FirstOrDefaultAsync(s => s.CompanyId == companyId, ct);

        var oldSalary = history.OldSalary ?? 0m;
        var newSalary = history.NewSalary ?? 0m;
        var effectivePercent = oldSalary == 0m ? 0m : Math.Round((newSalary - oldSalary) * 100m / oldSalary, 2);

        return new SalaryIncreaseOrderData(
            instance.SnapshotEmpName ?? $"{employee.EmpName} {employee.EmpSurname}",
            instance.SnapshotEmpNo ?? employee.EmpNo,
            instance.SnapshotPositionName,
            instance.SnapshotOrganizationName,
            instance.FinalGrade ?? "-",
            instance.FinalScorePercent,
            oldSalary, newSalary, effectivePercent,
            history.ChangedDate, instance.EvaluationPeriod?.Name,
            history.OrderNo, history.OrderDate,
            settings?.CompanyName ?? companyId, settings?.CompanyAddress);
    }
}
