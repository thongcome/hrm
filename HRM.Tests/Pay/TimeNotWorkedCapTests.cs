using HRM.Services.Pay.Calculators;
using Xunit;

namespace HRM.Tests.Pay;

// ม.76: late/absence may only take the wage for the time not worked from base salary, never a fine
public class TimeNotWorkedCapTests
{
    [Fact]
    public void Late_cap_is_the_wage_of_the_minutes_actually_late()
    {
        // 30,000 ÷ 30 ÷ 8 ÷ 60 = 2.0833/min × 45 min = 93.75
        var (lateCap, _) = AttendanceRuleCalculator.TimeNotWorkedValue(30000m, 30, 8m, totalLateMinutes: 45, absentDays: 0);
        Assert.Equal(93.75m, lateCap);
    }

    [Fact]
    public void Absent_cap_is_the_daily_wage_per_absent_day()
    {
        var (_, absentCap) = AttendanceRuleCalculator.TimeNotWorkedValue(30000m, 30, 8m, totalLateMinutes: 0, absentDays: 2);
        Assert.Equal(2000m, absentCap);
    }

    [Fact]
    public void A_per_occurrence_fine_above_the_time_value_is_cut_to_the_time_value()
    {
        // 3 late days of 5 minutes = 15 min → 31.25; a 100/occurrence fine (300) is cut to 31.25
        var (lateCap, _) = AttendanceRuleCalculator.TimeNotWorkedValue(30000m, 30, 8m, 15, 0);
        Assert.Equal(31.25m, AttendanceRuleCalculator.CapToTimeNotWorked(300m, lateCap, alreadyTaken: 0m));
    }

    [Fact]
    public void Forfeit_whole_salary_for_lateness_is_cut_to_the_time_value()
    {
        var (lateCap, _) = AttendanceRuleCalculator.TimeNotWorkedValue(30000m, 30, 8m, 10, 0);
        Assert.Equal(lateCap, AttendanceRuleCalculator.CapToTimeNotWorked(30000m, lateCap, 0m));
    }

    [Fact]
    public void Several_rules_together_never_exceed_the_cap()
    {
        var cap = 100m;
        var first = AttendanceRuleCalculator.CapToTimeNotWorked(70m, cap, 0m);
        var second = AttendanceRuleCalculator.CapToTimeNotWorked(70m, cap, first);
        Assert.Equal(70m, first);
        Assert.Equal(30m, second);
        Assert.Equal(0m, AttendanceRuleCalculator.CapToTimeNotWorked(10m, cap, first + second));
    }

    [Fact]
    public void Missing_policy_values_fall_back_to_30_days_and_8_hours()
    {
        var (lateCap, absentCap) = AttendanceRuleCalculator.TimeNotWorkedValue(24000m, 0, 0m, 60, 1);
        Assert.Equal(100m, lateCap);    // 24,000 ÷ 30 ÷ 8 = 100/hour
        Assert.Equal(800m, absentCap);
    }
}
