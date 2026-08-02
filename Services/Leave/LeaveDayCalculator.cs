namespace HRM.Services.Leave;

// Static, pure day-count calculator — no DB access, matches the
// Services/Pay/Calculators pattern (e.g. ProrationCalculator). Counts
// business days in an inclusive date range: excludes Saturday/Sunday and
// any date present in the supplied holiday set. The 5-day work week is a
// simplifying default for this MVP, not configurable per company yet.
public static class LeaveDayCalculator
{
    public static decimal CalculateWorkingDays(DateOnly start, DateOnly end, IReadOnlySet<DateOnly> holidayDates)
    {
        if (end < start) return 0m;

        var count = 0;
        for (var date = start; date <= end; date = date.AddDays(1))
        {
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;
            if (holidayDates.Contains(date)) continue;
            count++;
        }

        return count;
    }
}
