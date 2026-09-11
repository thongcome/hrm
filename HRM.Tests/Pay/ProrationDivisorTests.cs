using HRM.Services.Pay.Calculators;
using Xunit;

namespace HRM.Tests.Pay;

// audit M7: อัตราต่อวันของคนเข้า/ออกกลางเดือนต้องเป็นตัวเลขเดียวกับที่ใช้หักขาดงาน (เงินเดือน ÷ ตัวหารในนโยบาย)
public class ProrationDivisorTests
{
    private static readonly DateOnly Jul1 = new(2026, 7, 1);
    private static readonly DateOnly Jul31 = new(2026, 7, 31);

    [Fact]
    public void Divisor_mode_uses_the_policy_divisor_not_the_month_length()
    {
        // เข้า 16 ก.ค. → ทำงาน 16 วัน ในเดือน 31 วัน — ตัวหาร 30 ให้ 16/30 ไม่ใช่ 16/31
        var r = ProrationCalculator.Calculate(Jul1, Jul31, joinDate: new DateOnly(2026, 7, 16), resignDate: null, daysPerMonthDivisor: 30);
        Assert.Equal(16, r.ActualWorkingDays);
        Assert.Equal(Math.Round(16m / 30m, 4), r.ProrationFactor);
    }

    [Fact]
    public void Full_period_is_always_the_full_salary_even_when_the_divisor_is_smaller_than_the_month()
    {
        var r = ProrationCalculator.Calculate(Jul1, Jul31, null, null, daysPerMonthDivisor: 30);
        Assert.Equal(1m, r.ProrationFactor);
    }

    [Fact]
    public void Without_a_divisor_the_old_actual_days_behaviour_is_unchanged()
    {
        var r = ProrationCalculator.Calculate(Jul1, Jul31, joinDate: new DateOnly(2026, 7, 16), resignDate: null);
        Assert.Equal(Math.Round(16m / 31m, 4), r.ProrationFactor);
    }
}
