namespace HRM.Services.Leave;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Extracted from LeaveRequestList.razor's RecomputeBalanceAsync once the
// external (ecosystem/chatbot) API needed the identical
// (policy entitlement − completed-request days-used) calculation for the
// same (employee, leave type, year) shape — same extract-on-second-use
// precedent as Services/Shared/OrgEmployeeResolverHelper.cs /
// DirectReportResolverHelper.cs elsewhere in this codebase.
public class LeaveBalanceService(IDbContextFactory<HRMContext> dbFactory)
{
    public record LeaveBalanceRow(int LeaveTypeId, string LeaveTypeCode, decimal EntitlementDays, decimal CarriedOverDays, decimal UsedDays, decimal RemainingDays);

    public async Task<List<LeaveBalanceRow>> GetBalancesAsync(long hremployeeId, string companyId, int? leaveTypeId = null, int? year = null, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var policiesQuery = context.Lve_LeavePolicies.Include(p => p.Lve_LeaveType).Where(p => p.CompanyId == companyId && p.IsActive);
        if (leaveTypeId is int id)
            policiesQuery = policiesQuery.Where(p => p.LeaveTypeId == id);
        var policies = await policiesQuery.ToListAsync(ct);
        if (policies.Count == 0)
            return new();

        var workDate = (await context.Hremployee.Where(e => e.id == hremployeeId).Select(e => e.WorkDate).FirstOrDefaultAsync(ct));
        var targetYear = year ?? DateTime.Today.Year;

        var rows = new List<LeaveBalanceRow>();
        foreach (var policy in policies)
        {
            var entitlementThisYear = ComputeYearEntitlement(policy.EntitlementDaysPerYear, workDate, targetYear);
            var usedThisYear = await SumUsedDaysAsync(context, hremployeeId, policy.LeaveTypeId, targetYear, ct);
            var carriedOver = await ComputeCarryOverAsync(context, hremployeeId, policy, workDate, targetYear, ct);

            rows.Add(new LeaveBalanceRow(policy.LeaveTypeId, policy.Lve_LeaveType.Code, entitlementThisYear, carriedOver, usedThisYear, entitlementThisYear + carriedOver - usedThisYear));
        }

        return rows;
    }

    // Pro-rates the entitlement for the calendar year the employee was hired
    // in (WorkDate.Year == year); full entitlement for every year after;
    // zero for a year before they were hired (WorkDate in the future
    // relative to `year`, or unknown employee).
    private static decimal ComputeYearEntitlement(decimal entitlementDaysPerYear, DateTime? workDate, int year)
    {
        if (workDate is null) return entitlementDaysPerYear;

        var hireDate = DateOnly.FromDateTime(workDate.Value);
        if (hireDate.Year > year) return 0m;
        if (hireDate.Year < year) return entitlementDaysPerYear;

        var yearEnd = new DateOnly(year, 12, 31);
        var daysInYear = DateTime.IsLeapYear(year) ? 366 : 365;
        var daysRemaining = yearEnd.DayNumber - hireDate.DayNumber + 1;
        var factor = (decimal)daysRemaining / daysInYear;
        return Math.Round(entitlementDaysPerYear * factor, 1, MidpointRounding.AwayFromZero);
    }

    // Only looks at the immediately prior year's OWN entitlement/usage (never
    // layers in that year's own carry-over) — deliberately non-recursive, per
    // the design decision to not chain carry-over across multiple years.
    private static async Task<decimal> ComputeCarryOverAsync(HRMContext context, long hremployeeId, Lve_LeavePolicy policy, DateTime? workDate, int targetYear, CancellationToken ct)
    {
        if (policy.CarryOverMode == LeaveCarryOverMode.None) return 0m;

        var priorYear = targetYear - 1;
        var priorYearEntitlement = ComputeYearEntitlement(policy.EntitlementDaysPerYear, workDate, priorYear);
        var priorYearUsed = await SumUsedDaysAsync(context, hremployeeId, policy.LeaveTypeId, priorYear, ct);
        var priorYearOwnRemaining = Math.Max(0m, priorYearEntitlement - priorYearUsed);

        return policy.CarryOverMode switch
        {
            LeaveCarryOverMode.Capped => Math.Min(priorYearOwnRemaining, policy.MaxCarryOverDays ?? 0m),
            LeaveCarryOverMode.Unlimited => priorYearOwnRemaining,
            _ => 0m,
        };
    }

    private static async Task<decimal> SumUsedDaysAsync(HRMContext context, long hremployeeId, int leaveTypeId, int year, CancellationToken ct)
    {
        return await (
            from r in context.Lve_LeaveRequests
            join j in context.job_masters on r.JobMasterId equals j.jobmasterid
            where r.HremployeeId == hremployeeId
                  && r.LeaveTypeId == leaveTypeId
                  && r.StartDate.Year == year
                  && j.status == "COMPLETED"
            select r.TotalDays
        ).SumAsync(ct);
    }
}
