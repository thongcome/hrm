using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Pay;

public record LatestRunSnapshot(string PayrollPeriod, decimal TotalNetPay, int EmployeeCount, decimal TotalTaxWithheld);

public record YtdSummary(decimal TotalGross, decimal TotalDeductions, decimal TotalNetPay, decimal TotalTax, int EmployeeCount);
public record CostCenterRow(string CostCenterCode, decimal TotalGross, int EmployeeCount);
public record FiscalPeriodRow(long RunId, string PayrollPeriod, PayrollRunStatus Status, DateOnly PeriodStart, DateOnly PeriodEnd, decimal TotalNetPay, int EmployeeCount);

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

    // "fiscalYear" is the calendar year the fiscal year STARTS in — e.g. with
    // FiscalYearStartMonth=4 (Thai govt style), fiscalYear=2026 means
    // Apr 2026 - Mar 2027.
    public static (DateOnly Start, DateOnly End) GetFiscalYearBounds(int fiscalYearStartMonth, int fiscalYear)
    {
        var start = new DateOnly(fiscalYear, fiscalYearStartMonth, 1);
        var end = start.AddYears(1).AddDays(-1);
        return (start, end);
    }

    public static int GetCurrentFiscalYear(int fiscalYearStartMonth, DateOnly today) =>
        today.Month >= fiscalYearStartMonth ? today.Year : today.Year - 1;

    public static async Task<YtdSummary> GetYtdSummaryAsync(HRMContext ctx, string companyId, DateOnly fyStart, DateOnly fyEnd, CancellationToken ct = default)
    {
        var query = ctx.Pay_PayrollEmployees
            .Where(pe => pe.Pay_PayrollRun.CompanyId == companyId
                && pe.Pay_PayrollRun.Status >= PayrollRunStatus.Approved
                && pe.Pay_PayrollRun.PeriodStart >= fyStart
                && pe.Pay_PayrollRun.PeriodStart <= fyEnd);

        var totals = await query
            .GroupBy(x => 1)
            .Select(g => new
            {
                Gross = g.Sum(x => x.GrossEarnings),
                Deductions = g.Sum(x => x.TotalDeductions),
                Net = g.Sum(x => x.NetPay),
                Tax = g.Sum(x => x.TaxAmount),
            })
            .FirstOrDefaultAsync(ct);

        var employeeCount = await query.Select(x => x.HremployeeId).Distinct().CountAsync(ct);

        return new YtdSummary(totals?.Gross ?? 0, totals?.Deductions ?? 0, totals?.Net ?? 0, totals?.Tax ?? 0, employeeCount);
    }

    public static async Task<List<CostCenterRow>> GetCostCenterBreakdownAsync(HRMContext ctx, string companyId, DateOnly fyStart, DateOnly fyEnd, CancellationToken ct = default)
    {
        var rows = await ctx.Pay_PayrollEmployees
            .Where(pe => pe.Pay_PayrollRun.CompanyId == companyId
                && pe.Pay_PayrollRun.Status >= PayrollRunStatus.Approved
                && pe.Pay_PayrollRun.PeriodStart >= fyStart
                && pe.Pay_PayrollRun.PeriodStart <= fyEnd)
            .GroupBy(pe => pe.CostCenterCode)
            .Select(g => new
            {
                CostCenterCode = g.Key,
                Gross = g.Sum(x => x.GrossEarnings),
                Count = g.Select(x => x.HremployeeId).Distinct().Count(),
            })
            .ToListAsync(ct);

        return rows
            .Select(r => new CostCenterRow(string.IsNullOrWhiteSpace(r.CostCenterCode) ? "ไม่ระบุ" : r.CostCenterCode, r.Gross, r.Count))
            .OrderByDescending(r => r.TotalGross)
            .ToList();
    }

    public static async Task<List<FiscalPeriodRow>> GetPeriodsInFiscalYearAsync(HRMContext ctx, string companyId, DateOnly fyStart, DateOnly fyEnd, CancellationToken ct = default)
    {
        var runs = await ctx.Pay_PayrollRuns
            .Where(r => r.CompanyId == companyId && r.PeriodStart >= fyStart && r.PeriodStart <= fyEnd)
            .OrderBy(r => r.PeriodStart)
            .Select(r => new
            {
                r.Id,
                r.PayrollPeriod,
                r.Status,
                r.PeriodStart,
                r.PeriodEnd,
                NetPay = r.Pay_PayrollEmployees.Sum(pe => pe.NetPay),
                EmployeeCount = r.Pay_PayrollEmployees.Count(),
            })
            .ToListAsync(ct);

        return runs.Select(r => new FiscalPeriodRow(r.Id, r.PayrollPeriod, r.Status, r.PeriodStart, r.PeriodEnd, r.NetPay, r.EmployeeCount)).ToList();
    }
}
