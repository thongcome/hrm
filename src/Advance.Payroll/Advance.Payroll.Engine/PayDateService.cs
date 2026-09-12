using Advance.Payroll.Contracts;
using Advance.Payroll.Data;
using Microsoft.EntityFrameworkCore;

namespace Advance.Payroll.Engine;

// Ported from HRM Services/Pay/PayDateService.cs. Only change: the Lve_CompanyHolidays
// query is replaced with IHolidayCalendarSource.GetCalendarAsync (HRM's implementation
// reads Lve_CompanyHoliday exactly as before; Advance.Payroll Lite reads its own
// pay_holiday table — see EXTRACTION-PLAN.md).
public class PayDateService(IDbContextFactory<PayrollDbContext> dbFactory, IHolidayCalendarSource holidayCalendarSource)
{
    public async Task<DateOnly> ComputeDefaultPayDateAsync(string companyId, int periodYear, int periodMonth, CancellationToken ct = default)
    {
        await using var ctx = await dbFactory.CreateDbContextAsync(ct);
        var settings = await ctx.Pay_PayslipSettings.FirstOrDefaultAsync(s => s.CompanyId == companyId, ct);
        var payDay = settings?.PayDayOfMonth ?? 28;
        var adjust = settings?.PayDateAdjustBackward ?? true;

        var holidays = new HashSet<DateOnly>();
        if (adjust)
        {
            var monthStart = new DateOnly(periodYear, periodMonth, 1);
            var windowStart = monthStart.AddDays(-10);
            var windowEnd = monthStart.AddMonths(1);
            var calendar = await holidayCalendarSource.GetCalendarAsync(companyId, windowStart, windowEnd, ct);
            holidays = calendar.Holidays.ToHashSet();
        }

        return Compute(periodYear, periodMonth, payDay, adjust, holidays);
    }

    public static DateOnly Compute(int year, int month, int payDay, bool adjustBackward, ISet<DateOnly> holidays)
    {
        var lastDay = DateTime.DaysInMonth(year, month);
        var day = Math.Clamp(payDay, 1, lastDay);
        var date = new DateOnly(year, month, day);
        if (!adjustBackward) return date;

        while (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday || holidays.Contains(date))
            date = date.AddDays(-1);
        return date;
    }
}
