namespace HRM.Services.Leave;

// Static, pure day-count calculator — no DB access, matches the
// Services/Pay/Calculators pattern (e.g. ProrationCalculator). Counts
// business days in an inclusive date range: excludes non-working days (per
// company work-week config) and any date present in the supplied holiday
// set.
public static class LeaveDayCalculator
{
    private const int DefaultWorkDaysMask = // Mon-Fri
        (1 << (int)DayOfWeek.Monday) | (1 << (int)DayOfWeek.Tuesday) | (1 << (int)DayOfWeek.Wednesday) |
        (1 << (int)DayOfWeek.Thursday) | (1 << (int)DayOfWeek.Friday);

    // Resolves Lve_CompanySetting.WorkDaysMask to the bit mask working days
    // for CalculateWorkingDays — a null mask (company hasn't configured one)
    // falls back to the same Mon-Fri default this calculator always used.
    public static int ResolveWorkDaysMask(int? companyWorkDaysMask) => companyWorkDaysMask ?? DefaultWorkDaysMask;

    public static decimal CalculateWorkingDays(DateOnly start, DateOnly end, IReadOnlySet<DateOnly> holidayDates, int? workDaysMask = null)
    {
        if (end < start) return 0m;

        var mask = ResolveWorkDaysMask(workDaysMask);
        var count = 0;
        for (var date = start; date <= end; date = date.AddDays(1))
        {
            if ((mask & (1 << (int)date.DayOfWeek)) == 0) continue;
            if (holidayDates.Contains(date)) continue;
            count++;
        }

        return count;
    }
}
