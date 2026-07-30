using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Pay;

public record LatestRunSnapshot(string PayrollPeriod, decimal TotalNetPay, int EmployeeCount, decimal TotalTaxWithheld);

// Non-financial-trend KPIs for a real payroll dashboard — deliberately
// distinct from LaborCostTrendReport (multi-period financial trend) and
// PayItemBreakdownReport (single-period pay-item breakdown): workflow
// state, alerts, and headcount, none of which either report covers.
public static class PayrollDashboardService
{
    public static async Task<Dictionary<PayrollRunStatus, int>> GetRunStatusBoardAsync(HRMContext ctx, string companyId, int recentPeriods, CancellationToken ct = default)
    {
        var periods = await ctx.Pay_PayrollRuns
            .Where(r => r.CompanyId == companyId)
            .OrderByDescending(r => r.PayrollPeriod)
            .Take(recentPeriods)
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return periods.ToDictionary(p => p.Status, p => p.Count);
    }

    public static Task<int> GetPendingApprovalCountAsync(HRMContext ctx, string companyId, CancellationToken ct = default) =>
        ctx.Pay_PayrollRuns.CountAsync(r => r.CompanyId == companyId && r.Status == PayrollRunStatus.Reviewed, ct);

    public static async Task<List<Pay_PayrollEmployee>> GetNegativeNetPayAlertsAsync(HRMContext ctx, string companyId, CancellationToken ct = default)
    {
        var latestRunId = await LatestRunIdAsync(ctx, companyId, ct);
        if (latestRunId is null) return new();

        return await ctx.Pay_PayrollEmployees
            .Include(pe => pe.Hremployee)
            .Where(pe => pe.PayrollRunId == latestRunId && pe.IsNegativeNetPayFlag)
            .ToListAsync(ct);
    }

    public static async Task<List<Pay_PayrollEmployee>> GetExcludedEmployeesAsync(HRMContext ctx, string companyId, CancellationToken ct = default)
    {
        var latestRunId = await LatestRunIdAsync(ctx, companyId, ct);
        if (latestRunId is null) return new();

        return await ctx.Pay_PayrollEmployees
            .Include(pe => pe.Hremployee)
            .Where(pe => pe.PayrollRunId == latestRunId && pe.IsExcluded)
            .ToListAsync(ct);
    }

    public static async Task<(int Active, int Resigned)> GetHeadcountAsync(HRMContext ctx, string companyId, CancellationToken ct = default)
    {
        var active = await ctx.Hremployee.CountAsync(e => e.companyid == companyId && e.ResignDate == null, ct);
        var resigned = await ctx.Hremployee.CountAsync(e => e.companyid == companyId && e.ResignDate != null, ct);
        return (active, resigned);
    }

    public static Task<int> GetPendingAdhocCountAsync(HRMContext ctx, string companyId, CancellationToken ct = default) =>
        ctx.Pay_AdhocPayItems.CountAsync(a => a.Hremployee.companyid == companyId && a.Status == PayAdhocItemStatus.Pending, ct);

    public static async Task<LatestRunSnapshot?> GetLatestRunSnapshotAsync(HRMContext ctx, string companyId, CancellationToken ct = default)
    {
        var run = await ctx.Pay_PayrollRuns
            .Where(r => r.CompanyId == companyId && r.Status >= PayrollRunStatus.Approved)
            .OrderByDescending(r => r.PayrollPeriod)
            .FirstOrDefaultAsync(ct);
        if (run is null) return null;

        var employees = await ctx.Pay_PayrollEmployees.Where(pe => pe.PayrollRunId == run.Id).ToListAsync(ct);
        return new LatestRunSnapshot(run.PayrollPeriod, employees.Sum(e => e.NetPay), employees.Count, employees.Sum(e => e.TaxAmount));
    }

    public static Task<List<Pay_PayrollAuditLog>> GetRecentActivityAsync(HRMContext ctx, string companyId, int take, CancellationToken ct = default) =>
        ctx.Pay_PayrollAuditLogs
            .Include(a => a.Pay_PayrollRun)
            .Where(a => a.Pay_PayrollRun.CompanyId == companyId)
            .OrderByDescending(a => a.EventDate)
            .Take(take)
            .ToListAsync(ct);

    private static Task<long?> LatestRunIdAsync(HRMContext ctx, string companyId, CancellationToken ct) =>
        ctx.Pay_PayrollRuns
            .Where(r => r.CompanyId == companyId)
            .OrderByDescending(r => r.PayrollPeriod)
            .Select(r => (long?)r.Id)
            .FirstOrDefaultAsync(ct);
}
