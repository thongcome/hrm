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
    public record LeaveBalanceRow(LeaveType LeaveType, decimal EntitlementDays, decimal UsedDays, decimal RemainingDays);

    public async Task<List<LeaveBalanceRow>> GetBalancesAsync(long hremployeeId, string companyId, LeaveType? leaveType = null, int? year = null, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var policiesQuery = context.Lve_LeavePolicies.Where(p => p.CompanyId == companyId && p.IsActive);
        if (leaveType is LeaveType lt)
            policiesQuery = policiesQuery.Where(p => p.LeaveType == lt);
        var policies = await policiesQuery.ToListAsync(ct);
        if (policies.Count == 0)
            return new();

        var targetYear = year ?? DateTime.Today.Year;
        var rows = new List<LeaveBalanceRow>();
        foreach (var policy in policies)
        {
            var usedDays = await (
                from r in context.Lve_LeaveRequests
                join j in context.job_masters on r.JobMasterId equals j.jobmasterid
                where r.HremployeeId == hremployeeId
                      && r.LeaveType == policy.LeaveType
                      && r.StartDate.Year == targetYear
                      && j.status == "COMPLETED"
                select r.TotalDays
            ).SumAsync(ct);

            rows.Add(new LeaveBalanceRow(policy.LeaveType, policy.EntitlementDaysPerYear, usedDays, policy.EntitlementDaysPerYear - usedDays));
        }

        return rows;
    }
}
