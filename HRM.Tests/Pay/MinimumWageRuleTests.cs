using HRM.Models;
using HRM.Services.Pay;
using Xunit;

namespace HRM.Tests.Pay;

// ค่าจ้างขั้นต่ำเป็นตารางตั้งค่า — ตัวเลขในเทสต์เป็นค่าสมมติ ไม่ใช่อัตราจริง
public class MinimumWageRuleTests
{
    private static Pay_MinimumWage Row(string? province, decimal daily, int y, int m, int d, bool active = true) =>
        new() { ProvinceName = province, DailyAmount = daily, EffectiveFrom = new DateOnly(y, m, d), IsActive = active };

    [Fact]
    public void Empty_table_means_no_check()
    {
        Assert.Null(MinimumWageRule.DailyMinimum(Array.Empty<Pay_MinimumWage>(), "X", new DateOnly(2026, 1, 1)));
        Assert.False(MinimumWageRule.IsBelow(1m, null, null));
    }

    [Fact]
    public void Latest_row_not_after_the_date_wins()
    {
        var rows = new[] { Row(null, 300m, 2025, 1, 1), Row(null, 320m, 2026, 1, 1), Row(null, 340m, 2027, 1, 1) };
        Assert.Equal(300m, MinimumWageRule.DailyMinimum(rows, null, new DateOnly(2025, 12, 31)));
        Assert.Equal(320m, MinimumWageRule.DailyMinimum(rows, null, new DateOnly(2026, 6, 1)));
        Assert.Equal(340m, MinimumWageRule.DailyMinimum(rows, null, new DateOnly(2027, 1, 1)));
    }

    [Fact]
    public void A_matching_province_beats_the_all_province_row()
    {
        var rows = new[] { Row(null, 300m, 2026, 1, 1), Row("ภูเก็ต", 400m, 2026, 1, 1) };
        Assert.Equal(400m, MinimumWageRule.DailyMinimum(rows, " ภูเก็ต ", new DateOnly(2026, 6, 1)));
        Assert.Equal(300m, MinimumWageRule.DailyMinimum(rows, "เชียงใหม่", new DateOnly(2026, 6, 1)));
    }

    [Fact]
    public void Inactive_rows_and_future_rows_are_ignored()
    {
        var rows = new[] { Row(null, 300m, 2026, 1, 1), Row(null, 999m, 2026, 1, 1, active: false), Row(null, 500m, 2030, 1, 1) };
        Assert.Equal(300m, MinimumWageRule.DailyMinimum(rows, null, new DateOnly(2026, 6, 1)));
    }

    [Fact]
    public void Monthly_salary_is_compared_as_salary_divided_by_30()
    {
        Assert.False(MinimumWageRule.IsBelow(9_000m, null, 300m));   // 300/day exactly
        Assert.True(MinimumWageRule.IsBelow(8_999m, null, 300m));
        Assert.True(MinimumWageRule.IsBelow(null, 299.99m, 300m));   // daily wage used as-is
        Assert.False(MinimumWageRule.IsBelow(null, 300m, 300m));
    }

    [Fact]
    public void Daily_wage_wins_over_salary_when_both_are_set()
    {
        Assert.Equal(250m, MinimumWageRule.DailyEquivalent(30_000m, 250m));
    }

    [Fact]
    public void No_pay_data_is_not_a_minimum_wage_violation()
    {
        Assert.False(MinimumWageRule.IsBelow(null, null, 300m));
        Assert.False(MinimumWageRule.IsBelow(0m, 0m, 300m));
    }
}
