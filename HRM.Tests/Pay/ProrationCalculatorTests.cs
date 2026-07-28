using HRM.Services.Pay.Calculators;
using Xunit;

namespace HRM.Tests.Pay;

public class ProrationCalculatorTests
{
    private static readonly DateOnly PeriodStart = new(2026, 6, 1);
    private static readonly DateOnly PeriodEnd = new(2026, 6, 30); // 30-day period

    [Fact]
    public void Full_month_employee_gets_factor_of_one()
    {
        var result = ProrationCalculator.Calculate(PeriodStart, PeriodEnd, joinDate: null, resignDate: null);

        Assert.Equal(1.0m, result.ProrationFactor);
        Assert.Equal(30, result.WorkingDaysInPeriod);
        Assert.Equal(30, result.ActualWorkingDays);
    }

    [Fact]
    public void Mid_month_joiner_is_prorated_to_days_actually_worked()
    {
        // joins June 16 -> worked June 16-30 = 15 of 30 days
        var result = ProrationCalculator.Calculate(PeriodStart, PeriodEnd, joinDate: new DateOnly(2026, 6, 16), resignDate: null);

        Assert.Equal(15, result.ActualWorkingDays);
        Assert.Equal(0.5m, result.ProrationFactor);
    }

    [Fact]
    public void Mid_month_leaver_is_prorated_to_days_actually_worked()
    {
        // resigns June 10 -> worked June 1-10 = 10 of 30 days
        var result = ProrationCalculator.Calculate(PeriodStart, PeriodEnd, joinDate: null, resignDate: new DateOnly(2026, 6, 10));

        Assert.Equal(10, result.ActualWorkingDays);
        Assert.Equal(Math.Round(10m / 30m, 4), result.ProrationFactor);
    }

    [Fact]
    public void Employee_who_both_joined_and_resigned_within_the_period_is_prorated_correctly()
    {
        // joined June 5, resigned June 14 -> 10 days
        var result = ProrationCalculator.Calculate(PeriodStart, PeriodEnd, joinDate: new DateOnly(2026, 6, 5), resignDate: new DateOnly(2026, 6, 14));

        Assert.Equal(10, result.ActualWorkingDays);
    }

    [Fact]
    public void Employee_who_resigned_before_the_period_starts_gets_zero_factor()
    {
        var result = ProrationCalculator.Calculate(PeriodStart, PeriodEnd, joinDate: null, resignDate: new DateOnly(2026, 5, 20));

        Assert.Equal(0m, result.ProrationFactor);
        Assert.Equal(0, result.ActualWorkingDays);
    }

    [Fact]
    public void Join_date_after_period_end_is_treated_as_not_yet_started()
    {
        var result = ProrationCalculator.Calculate(PeriodStart, PeriodEnd, joinDate: new DateOnly(2026, 7, 1), resignDate: null);

        Assert.Equal(0m, result.ProrationFactor);
    }

    [Fact]
    public void Period_end_before_start_throws()
    {
        Assert.Throws<ArgumentException>(() => ProrationCalculator.Calculate(PeriodEnd, PeriodStart, null, null));
    }
}
