namespace HRM.Services.Pay.Calculators;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Fixes PayrollProcess.razor GetOTIncome (Components\Pages\Payroll\PayrollProcess.razor
// ~line 1550): the legacy query was `HrwOts.Where(x => x.EmpNo == EmpNo)` with NO date
// filter at all, so every OT record ever entered for the employee was re-added as
// income on every subsequent payroll run. This scopes strictly to the pay period.
//
// Only approved OT is paid (BA audit 25 ก.ย. 2569): a row counts when it is marked approved
// (ApvOtStatus "A" — the legacy convention, now also stamped when an approved OT request is sent to
// payroll) or when it is linked to an OT request whose approval job COMPLETED. Before this, any row
// in HRW_OT was paid, and the legacy /hrwot pages let any signed-in user insert one.
public class OvertimeEarningsCalculator
{
    private readonly IDbContextFactory<HRMContext> _dbFactory;

    public OvertimeEarningsCalculator(IDbContextFactory<HRMContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<HrwOt>> GetOvertimeForPeriodAsync(string companyId, string empNo, DateOnly periodStart, DateOnly periodEnd, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var start = periodStart.ToDateTime(TimeOnly.MinValue);
        var end = periodEnd.ToDateTime(TimeOnly.MaxValue);

        return await context.HrwOts
            .Where(IsApproved(context))
            .Where(x => x.companyid == companyId
                        && x.EmpNo == empNo
                        && x.DateWork != null
                        && x.DateWork >= start
                        && x.DateWork <= end)
            .ToListAsync(ct);
    }

    // ทั้งบริษัทในงวดเดียว หนึ่ง query แล้วแยกตามรหัสพนักงาน (audit M14) — เดิมยิงรายคน 7,000 ครั้งต่อรอบ
    public async Task<Dictionary<string, List<HrwOt>>> GetOvertimeForPeriodByEmployeeAsync(string companyId, DateOnly periodStart, DateOnly periodEnd, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var start = periodStart.ToDateTime(TimeOnly.MinValue);
        var end = periodEnd.ToDateTime(TimeOnly.MaxValue);
        var rows = await context.HrwOts
            .Where(IsApproved(context))
            .Where(x => x.companyid == companyId && x.EmpNo != null && x.DateWork != null && x.DateWork >= start && x.DateWork <= end)
            .ToListAsync(ct);
        return rows.GroupBy(x => x.EmpNo!).ToDictionary(g => g.Key, g => g.ToList());
    }

    public const string ApprovedStatus = "A";

    private static System.Linq.Expressions.Expression<Func<HrwOt, bool>> IsApproved(HRMContext context)
    {
        var completed = HRM.Services.Workflow.WorkflowEngineService.StatusCompleted;
        return x => x.ApvOtStatus == ApprovedStatus
            || context.emp_overtime_requests.Any(r => r.hrwOtId == x.ID && r.jobmasterid != null
                && context.job_masters.Any(j => j.jobmasterid == r.jobmasterid && j.status == completed));
    }

    public static decimal SumAmount(IEnumerable<HrwOt> otRecords) => otRecords.Sum(x => x.OtAmt ?? 0m);
}
