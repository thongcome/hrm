using HRM.Services.Leave;
using Xunit;

namespace HRM.Tests.Leave;

public class LeaveDayCalculatorTests
{
    [Fact]
    public void Single_weekday_with_no_holidays_counts_as_one_day()
    {
        var monday = new DateOnly(2026, 6, 1); // a Monday

        var days = LeaveDayCalculator.CalculateWorkingDays(monday, monday, holidayDates: new HashSet<DateOnly>());

        Assert.Equal(1m, days);
    }

    [Fact]
    public void Weekend_days_are_excluded()
    {
        // Mon 2026-06-01 through Sun 2026-06-07 -> 5 weekdays
        var start = new DateOnly(2026, 6, 1);
        var end = new DateOnly(2026, 6, 7);

        var days = LeaveDayCalculator.CalculateWorkingDays(start, end, holidayDates: new HashSet<DateOnly>());

        Assert.Equal(5m, days);
    }

    [Fact]
    public void Holiday_falling_on_a_weekday_is_excluded()
    {
        var start = new DateOnly(2026, 6, 1); // Monday
        var end = new DateOnly(2026, 6, 5);   // Friday
        var holidays = new HashSet<DateOnly> { new(2026, 6, 3) }; // Wednesday holiday

        var days = LeaveDayCalculator.CalculateWorkingDays(start, end, holidays);

        Assert.Equal(4m, days);
    }

    [Fact]
    public void Holiday_falling_on_a_weekend_is_not_double_counted()
    {
        var start = new DateOnly(2026, 6, 1); // Monday
        var end = new DateOnly(2026, 6, 7);   // Sunday
        var holidays = new HashSet<DateOnly> { new(2026, 6, 6) }; // Saturday, already excluded as weekend

        var days = LeaveDayCalculator.CalculateWorkingDays(start, end, holidays);

        Assert.Equal(5m, days);
    }

    [Fact]
    public void End_before_start_returns_zero_instead_of_throwing()
    {
        var start = new DateOnly(2026, 6, 10);
        var end = new DateOnly(2026, 6, 1);

        var days = LeaveDayCalculator.CalculateWorkingDays(start, end, holidayDates: new HashSet<DateOnly>());

        Assert.Equal(0m, days);
    }
}
