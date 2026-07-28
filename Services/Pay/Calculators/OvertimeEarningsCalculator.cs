namespace HRM.Services.Pay.Calculators;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Fixes PayrollProcess.razor GetOTIncome (Components\Pages\Payroll\PayrollProcess.razor
// ~line 1550): the legacy query was `HrwOts.Where(x => x.EmpNo == EmpNo)` with NO date
// filter at all, so every OT record ever entered for the employee was re-added as
// income on every subsequent payroll run. This scopes strictly to the pay period.
//
// Note: HrwOt.ApvOtStatus exists but its "approved" value convention is not
// documented anywhere in the legacy code (the legacy query didn't check it
// either) — deliberately NOT filtering on it here to avoid silently excluding
// all OT records on a wrong guess. Confirm the real convention with the
// business before adding an approval-status filter.
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
            .Where(x => x.companyid == companyId
                        && x.EmpNo == empNo
                        && x.DateWork != null
                        && x.DateWork >= start
                        && x.DateWork <= end)
            .ToListAsync(ct);
    }

    public static decimal SumAmount(IEnumerable<HrwOt> otRecords) => otRecords.Sum(x => x.OtAmt ?? 0m);
}
