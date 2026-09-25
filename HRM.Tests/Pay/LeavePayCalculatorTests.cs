using HRM.Services.Pay.Calculators;
using Xunit;

namespace HRM.Tests.Pay;

public class LeavePayCalculatorTests
{
    // Mon–Fri working week (bit per DayOfWeek, Sunday = bit 0)
    private const int MonToFri = 0b0111110;
    private static readonly HashSet<DateOnly> NoHolidays = new();
    private static readonly DateOnly From = new(2026, 9, 1);   // Tuesday
    private static readonly DateOnly To = new(2026, 9, 30);

    private static LeavePayCalculator.ApprovedLeave Leave(DateOnly start, DateOnly end, bool paid, bool halfDay = false)
        => new(1, start, end, halfDay, paid);

    [Fact]
    public void Only_working_days_count_weekends_inside_a_leave_are_ignored()
    {
        // Fri 4 Sep → Tue 8 Sep = Fri, Mon, Tue = 3 working days
        var days = LeavePayCalculator.DaysInWindow(new[] { Leave(new(2026, 9, 4), new(2026, 9, 8), paid: false) }, From, To, NoHolidays, MonToFri);
        Assert.Equal(3m, days.Sum(d => d.UnpaidFraction));
        Assert.Equal(0m, days.Sum(d => d.PaidFraction));
    }

    [Fact]
    public void Company_holidays_are_not_leave_days()
    {
        var holidays = new HashSet<DateOnly> { new(2026, 9, 7) };
        var days = LeavePayCalculator.DaysInWindow(new[] { Leave(new(2026, 9, 7), new(2026, 9, 8), paid: true) }, From, To, holidays, MonToFri);
        Assert.Equal(1m, days.Sum(d => d.PaidFraction));
    }

    [Fact]
    public void Leave_is_clipped_to_the_attendance_window()
    {
        // 28 Aug → 2 Sep, window starts 1 Sep: only Tue 1 and Wed 2 count
        var days = LeavePayCalculator.DaysInWindow(new[] { Leave(new(2026, 8, 28), new(2026, 9, 2), paid: false) }, From, To, NoHolidays, MonToFri);
        Assert.Equal(2m, days.Sum(d => d.UnpaidFraction));
    }

    [Fact]
    public void Half_day_counts_half()
    {
        var d = new DateOnly(2026, 9, 10);
        var days = LeavePayCalculator.DaysInWindow(new[] { Leave(d, d, paid: false, halfDay: true) }, From, To, NoHolidays, MonToFri);
        Assert.Equal(0.5m, days.Single().UnpaidFraction);
    }

    [Fact]
    public void Paid_and_unpaid_halves_on_one_date_stay_separate_and_never_exceed_a_day()
    {
        var d = new DateOnly(2026, 9, 10);
        var days = LeavePayCalculator.DaysInWindow(new[]
        {
            Leave(d, d, paid: true, halfDay: true),
            Leave(d, d, paid: false, halfDay: true),
            Leave(d, d, paid: false),   // overlapping full day adds nothing beyond the whole day
        }, From, To, NoHolidays, MonToFri);
        var day = days.Single();
        Assert.Equal(0.5m, day.PaidFraction);
        Assert.Equal(0.5m, day.UnpaidFraction);
    }

    [Fact]
    public void Leave_outside_the_window_is_ignored()
    {
        var days = LeavePayCalculator.DaysInWindow(new[] { Leave(new(2026, 10, 5), new(2026, 10, 6), paid: false) }, From, To, NoHolidays, MonToFri);
        Assert.Empty(days);
    }
}
