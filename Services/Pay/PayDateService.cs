namespace HRM.Services.Pay;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Computes the default salary pay date for a payroll period from company
// config (Pay_PayslipSettings.PayDayOfMonth) with an optional weekend/holiday
// back-shift (PayDateAdjustBackward): e.g. pay on the 28th, and if that lands
// on a Saturday/Sunday or a company holiday, move earlier to the nearest
// working day so employees are never paid late — the common Thai convention.
// Config-first: HR sets the day + the rule; this only applies them.
public class PayDateService(IDbContextFactory<HRMContext> dbFactory)
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
            // Only the days around the target month can ever be hit by the
            // backward walk, so fetch a small window rather than every holiday.
            var monthStart = new DateOnly(periodYear, periodMonth, 1);
            var windowStart = monthStart.AddDays(-10);
            var windowEnd = monthStart.AddMonths(1);
            holidays = (await ctx.Lve_CompanyHolidays
                .Where(h => h.CompanyId == companyId && h.IsActive && h.HolidayDate >= windowStart && h.HolidayDate <= windowEnd)
                .Select(h => h.HolidayDate)
                .ToListAsync(ct)).ToHashSet();
        }

        return Compute(periodYear, periodMonth, payDay, adjust, holidays);
    }

    // Pure + testable: clamp the configured day to the month, then (when
    // adjusting) walk backward off weekends/holidays to the nearest earlier
    // working day.
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
