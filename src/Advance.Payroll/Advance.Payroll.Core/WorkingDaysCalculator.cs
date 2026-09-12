namespace Advance.Payroll.Core;

// New in Advance.Payroll (Phase 0) — extracted from the one-line call
// HRM.Services.Leave.LeaveDayCalculator.CalculateWorkingDays(spanStart, spanEnd,
// companyHolidays, companyWorkDaysMask) inside PayrollCalculationService.cs:489, which
// otherwise would have pulled a dependency on HRM's whole Leave module (Services/Leave/)
// into Payroll for one date-arithmetic helper. Re-implemented here as a pure function
// with the same WorkDaysMask bit convention (bit 0 = Sunday .. bit 6 = Saturday,
// DayOfWeek's own numbering) so Advance.Payroll.Contracts.CompanyCalendar plugs straight
// in without HRM's Leave module ever being referenced.
public static class WorkingDaysCalculator
{
    public static int CalculateWorkingDays(DateOnly start, DateOnly end, IReadOnlySet<DateOnly> holidays, int workDaysMask)
    {
        if (end < start) return 0;
        var count = 0;
        for (var d = start; d <= end; d = d.AddDays(1))
        {
            var bit = 1 << (int)d.DayOfWeek;
            if ((workDaysMask & bit) == 0) continue;
            if (holidays.Contains(d)) continue;
            count++;
        }
        return count;
    }
}
