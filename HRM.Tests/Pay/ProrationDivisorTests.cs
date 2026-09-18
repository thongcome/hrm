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

    [Fact]
    public void Half_month_period_is_not_prorated_twice()
    {
        // audit H-06: เงินเดือน 30,000 จ่ายครึ่งเดือน เข้างาน 6 ม.ค. รอบ 1–15 ม.ค. = ทำงาน 10 วัน → 30,000 ÷ 30 × 10 = 10,000
        // (เดิม: 30,000 × ½ × 10/30 = 4,999.50)
        var r = ProrationCalculator.Calculate(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 15),
            joinDate: new DateOnly(2026, 1, 6), resignDate: null, daysPerMonthDivisor: 30, monthFraction: 0.5m);
        Assert.Equal(10, r.ActualWorkingDays);
        Assert.Equal(10000m, Math.Round(30000m * 0.5m * r.ExactFactor, 2, MidpointRounding.AwayFromZero));
    }

    [Fact]
    public void Money_uses_the_exact_factor_not_the_rounded_display_factor()
    {
        // audit L-02: 30,000 × 10/30 = 10,000 — not 30,000 × 0.3333 = 9,999
        var r = ProrationCalculator.Calculate(Jul1, Jul31, joinDate: new DateOnly(2026, 7, 22), resignDate: null, daysPerMonthDivisor: 30);
        Assert.Equal(0.3333m, r.ProrationFactor);
        Assert.Equal(10000m, Math.Round(30000m * r.ExactFactor, 2, MidpointRounding.AwayFromZero));
    }
}
