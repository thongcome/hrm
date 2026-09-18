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
    public void Amount_is_computed_from_the_exact_monthly_wage_not_a_pre_rounded_daily_rate()
    {
        // 30 days' wage of a 10,000 salary is exactly 10,000 — the old code rounded the daily
        // rate first (333.33 × 30 = 9,999.90). The displayed daily wage is still 333.33.
        // serviceDays=364 (30-day tier) — NOT AddDays(364), which lands on 365 (the 90-day tier).
        var result = SeveranceCalculator.Calculate(HireDate, LastWorkDateForServiceDays(364), monthlyWage: 10000m);

        Assert.Equal(30, result.EntitledDays);
        Assert.Equal(333.33m, result.DailyWage);
        Assert.Equal(10000m, result.Amount);
    }

    [Fact]
    public void Daily_wage_employee_gets_daily_wage_times_entitled_days()
    {
        // audit H-11: ม.118 ลูกจ้างรายวัน = ค่าจ้างรายวันอัตราสุดท้าย × วัน — 4 ปี → 180 วัน × 500 = 90,000
        var result = SeveranceCalculator.CalculateForDailyWage(HireDate, LastWorkDateForServiceDays(4 * 365), dailyWage: 500m);

        Assert.Equal(180, result.EntitledDays);
        Assert.Equal(500m, result.DailyWage);
        Assert.Equal(90000m, result.Amount);
    }

    [Fact]
    public void Twenty_years_gives_400_days()
    {
        var result = SeveranceCalculator.Calculate(HireDate, LastWorkDateForServiceDays(7300), monthlyWage: 30000m);
        Assert.Equal(400000m, result.Amount);   // 30,000 ÷ 30 × 400
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
