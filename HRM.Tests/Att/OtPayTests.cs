using HRM.Models;
using HRM.Services.Att.Calculators;
using Xunit;

namespace HRM.Tests.Att;

// OT pay by law (ม.61–63): hourly wage × statutory multiplier by day type; config may only raise it
public class OtPayTests
{
    // 24,000 / 30 / 8 = 100 per hour
    private const decimal Monthly = 24000m;

    [Fact]
    public void Workday_ot_is_at_least_one_and_a_half_times()
    {
        Assert.Equal(300m, OtRateCalculator.CalculatePay(Monthly, null, 2m, OtDayType.Workday, configuredMultiplier: null).Amount);
    }

    [Fact]
    public void A_configured_multiplier_below_the_law_is_ignored()
    {
        Assert.Equal(300m, OtRateCalculator.CalculatePay(Monthly, null, 2m, OtDayType.Workday, configuredMultiplier: 1m).Amount);
    }

    [Fact]
    public void A_configured_multiplier_above_the_law_is_used()
    {
        Assert.Equal(400m, OtRateCalculator.CalculatePay(Monthly, null, 2m, OtDayType.Workday, configuredMultiplier: 2m).Amount);
    }

    [Fact]
    public void Monthly_staff_working_a_holiday_within_normal_hours_get_one_times_extra()
    {
        Assert.Equal(800m, OtRateCalculator.CalculatePay(Monthly, null, 8m, OtDayType.Holiday, null).Amount);
    }

    [Fact]
    public void Daily_staff_working_a_holiday_within_normal_hours_get_two_times()
    {
        // 800/day / 8 = 100 per hour × 8 h × 2
        Assert.Equal(1600m, OtRateCalculator.CalculatePay(null, 800m, 8m, OtDayType.RestDay, null).Amount);
    }

    [Fact]
    public void Hours_beyond_normal_on_a_holiday_are_paid_three_times()
    {
        // 8 h × 100 × 1 + 2 h × 100 × 3
        Assert.Equal(1400m, OtRateCalculator.CalculatePay(Monthly, null, 10m, OtDayType.Holiday, null).Amount);
    }

    [Fact]
    public void Normal_hours_per_day_changes_the_hourly_wage_and_the_split()
    {
        // 24,000 / 30 / 7.5 = 106.67/h; 7.5 h at 1× + 0.5 h at 3×
        var pay = OtRateCalculator.CalculatePay(Monthly, null, 8m, OtDayType.Holiday, null, normalHoursPerDay: 7.5m);
        Assert.Equal(Math.Round(7.5m * (24000m / 30m / 7.5m) + 0.5m * (24000m / 30m / 7.5m) * 3m, 2, MidpointRounding.AwayFromZero), pay.Amount);
    }

    [Fact]
    public void No_wage_on_file_pays_nothing()
    {
        Assert.Equal(0m, OtRateCalculator.CalculatePay(null, null, 3m, OtDayType.Workday, null).Amount);
    }
}
