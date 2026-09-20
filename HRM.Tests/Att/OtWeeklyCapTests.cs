using HRM.Services.Att.Calculators;
using Xunit;

namespace HRM.Tests.Att;

public class OtWeeklyCapTests
{
    [Fact]
    public void Within_cap_does_not_exceed()
    {
        var r = OtWeeklyCap.Evaluate(cap: 36m, existingHours: 30m, newHours: 6m);
        Assert.False(r.Exceeds);
        Assert.Equal(36m, r.TotalHours);
        Assert.Equal(0m, r.Excess);
    }

    [Fact]
    public void Over_cap_reports_the_excess_hours()
    {
        var r = OtWeeklyCap.Evaluate(cap: 36m, existingHours: 33.5m, newHours: 4m);
        Assert.True(r.Exceeds);
        Assert.Equal(37.5m, r.TotalHours);
        Assert.Equal(1.5m, r.Excess);
    }

    [Theory]
    [InlineData("2026-09-14", "2026-09-14")] // จันทร์ = ต้นสัปดาห์เอง
    [InlineData("2026-09-16", "2026-09-14")] // พุธ
    [InlineData("2026-09-20", "2026-09-14")] // อาทิตย์ = ท้ายสัปดาห์เดิม ไม่ใช่ต้นสัปดาห์ใหม่
    [InlineData("2026-09-21", "2026-09-21")] // จันทร์ถัดไป
    public void Week_runs_monday_to_sunday(string date, string expectedMonday)
        => Assert.Equal(DateOnly.Parse(expectedMonday, System.Globalization.CultureInfo.InvariantCulture), OtWeeklyCap.WeekStart(DateOnly.Parse(date, System.Globalization.CultureInfo.InvariantCulture)));
}
