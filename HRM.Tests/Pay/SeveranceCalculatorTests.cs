using HRM.Services.Pay.Calculators;
using Xunit;

namespace HRM.Tests.Pay;

public class SeveranceCalculatorTests
{
    private static readonly DateOnly HireDate = new(2020, 1, 1);

    private static DateOnly LastWorkDateForServiceDays(int serviceDays) => HireDate.AddDays(serviceDays - 1);

    [Theory]
    [InlineData(119, 0)]   // just under 120 days -> no entitlement
    [InlineData(120, 30)]  // >=120 days, <1 year
    [InlineData(364, 30)]
    [InlineData(365, 90)]  // >=1 year, <3 years
    [InlineData(1094, 90)]
    [InlineData(1095, 180)] // >=3 years, <6 years
    [InlineData(2189, 180)]
    [InlineData(2190, 240)] // >=6 years, <10 years
    [InlineData(3649, 240)]
    [InlineData(3650, 300)] // >=10 years, <20 years
    [InlineData(7299, 300)]
    [InlineData(7300, 400)] // >=20 years
    public void Entitled_days_follow_section_118_tier_boundaries(int serviceDays, int expectedEntitledDays)
    {
        var lastWorkDate = LastWorkDateForServiceDays(serviceDays);

        var result = SeveranceCalculator.Calculate(HireDate, lastWorkDate, monthlyWage: 30000m);

        Assert.Equal(serviceDays, result.ContinuousServiceDays);
        Assert.Equal(expectedEntitledDays, result.EntitledDays);
    }

    [Fact]
    public void Daily_wage_rounds_away_from_zero_at_the_midpoint()
    {
        // 10000 / 30 = 333.333... -> 333.33. serviceDays=364 (30-day tier) —
        // NOT AddDays(364), which lands on serviceDays=365 (the 90-day tier).
        var result = SeveranceCalculator.Calculate(HireDate, LastWorkDateForServiceDays(364), monthlyWage: 10000m);

        Assert.Equal(30, result.EntitledDays);
        Assert.Equal(333.33m, result.DailyWage);
        Assert.Equal(Math.Round(333.33m * 30, 2, MidpointRounding.AwayFromZero), result.Amount);
    }

    [Fact]
    public void Zero_entitlement_tier_still_returns_zero_amount_not_an_error()
    {
        var result = SeveranceCalculator.Calculate(HireDate, HireDate.AddDays(50), monthlyWage: 30000m);

        Assert.Equal(0, result.EntitledDays);
        Assert.Equal(0m, result.Amount);
    }

    [Fact]
    public void LastWorkDate_before_hireDate_throws()
    {
        Assert.Throws<ArgumentException>(() => SeveranceCalculator.Calculate(HireDate, HireDate.AddDays(-1), 30000m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Non_positive_monthly_wage_throws(decimal wage)
    {
        Assert.Throws<ArgumentException>(() => SeveranceCalculator.Calculate(HireDate, HireDate.AddDays(365), wage));
    }
}
