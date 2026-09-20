using HRM.Services.Pay.Calculators;
using Xunit;

namespace HRM.Tests.Pay;

public class SeveranceTaxCalculatorTests
{
    // ตัวเลขตั้งต้นตามกติกาตั้งแต่ปีภาษี 2566 ที่ seed ไว้ (400 วัน / 600,000 / 7,000 ต่อปี / 50%)
    private static readonly SeveranceTaxCalculator.RuleValues New = new(400, 600_000m, 7_000m, 0.5m, null);
    private static readonly SeveranceTaxCalculator.RuleValues Old = new(300, 300_000m, 7_000m, 0.5m, null);

    [Fact]
    public void Amount_within_exemption_is_fully_exempt()
    {
        // ค่าจ้าง 1,000/วัน × 400 = 400,000 ยังไม่ชนเพดาน 600,000; ยอด 300,000 ยกเว้นทั้งก้อน
        var r = SeveranceTaxCalculator.Calculate(300_000m, 1_000m, new DateOnly(2020, 1, 1), new DateOnly(2029, 12, 31), New);
        Assert.Equal(300_000m, r.Exempt);
        Assert.Equal(0m, r.Excess);
        Assert.Equal(0m, r.ExpenseDeduction);
        Assert.Equal(0m, r.TaxableBasis);
    }

    [Fact]
    public void Exemption_is_capped_and_excess_takes_both_expense_deductions()
    {
        // 900,000: ยกเว้น 600,000 (เพดาน) ส่วนเกิน 300,000; 10 ปี → ค่าใช้จ่าย 1 = 70,000; เหลือ 230,000 หัก 50% = 115,000
        var r = SeveranceTaxCalculator.Calculate(900_000m, 3_333.33m, new DateOnly(2020, 1, 1), new DateOnly(2029, 12, 31), New);
        Assert.Equal(600_000m, r.Exempt);
        Assert.Equal(300_000m, r.Excess);
        Assert.Equal(10, r.YearsCounted);
        Assert.Equal(185_000m, r.ExpenseDeduction);
        Assert.Equal(115_000m, r.TaxableBasis);
    }

    [Fact]
    public void Exemption_is_limited_by_days_times_daily_wage_when_that_is_lower()
    {
        // 1,000/วัน × 400 = 400,000 < 600,000; ยอด 500,000 → ยกเว้น 400,000 ส่วนเกิน 100,000; 1 ปี → 7,000 + 50%×93,000 = 53,500
        var r = SeveranceTaxCalculator.Calculate(500_000m, 1_000m, new DateOnly(2025, 3, 1), new DateOnly(2025, 9, 1), New);
        Assert.Equal(400_000m, r.Exempt);
        Assert.Equal(100_000m, r.Excess);
        Assert.Equal(1, r.YearsCounted);
        Assert.Equal(53_500m, r.ExpenseDeduction);
        Assert.Equal(46_500m, r.TaxableBasis);
    }

    [Fact]
    public void Older_rule_uses_its_own_days_and_cap()
    {
        // อัตราเดิม 300 วัน / 300,000: ยอด 400,000 ค่าจ้าง 2,000/วัน → ยกเว้น 300,000 ส่วนเกิน 100,000
        var r = SeveranceTaxCalculator.Calculate(400_000m, 2_000m, new DateOnly(2010, 1, 1), new DateOnly(2021, 1, 1), Old);
        Assert.Equal(300_000m, r.Exempt);
        Assert.Equal(100_000m, r.Excess);
    }

    [Fact]
    public void Expense_deduction_never_exceeds_the_excess()
    {
        // ส่วนเกินเล็กกว่า 7,000 × ปี: ค่าใช้จ่ายส่วนที่ 1 = ส่วนเกินทั้งหมด ฐานภาษี 0 ไม่ติดลบ
        var r = SeveranceTaxCalculator.Calculate(410_000m, 1_000m, new DateOnly(2000, 1, 1), new DateOnly(2025, 1, 1), New);
        Assert.Equal(400_000m, r.Exempt);
        Assert.Equal(10_000m, r.Excess);
        Assert.Equal(10_000m, r.ExpenseDeduction);
        Assert.Equal(0m, r.TaxableBasis);
    }

    [Fact]
    public void Remainder_expense_cap_is_applied_when_configured()
    {
        var capped = new SeveranceTaxCalculator.RuleValues(400, 600_000m, 7_000m, 0.5m, 100_000m);
        // ส่วนเกิน 1,000,000, 1 ปี: ค่าใช้จ่าย 1 = 7,000; เหลือ 993,000 × 50% = 496,500 แต่เพดาน 100,000
        var r = SeveranceTaxCalculator.Calculate(1_600_000m, 10_000m, new DateOnly(2025, 1, 1), new DateOnly(2025, 6, 1), capped);
        Assert.Equal(600_000m, r.Exempt);
        Assert.Equal(1_000_000m, r.Excess);
        Assert.Equal(107_000m, r.ExpenseDeduction);
        Assert.Equal(893_000m, r.TaxableBasis);
    }

    [Theory]
    [InlineData("2020-01-01", "2029-12-31", 10)]   // ครบ 10 ปีพอดี
    [InlineData("2020-01-01", "2020-03-01", 1)]    // ไม่ถึงปี = 1
    [InlineData("2015-06-01", "2020-09-15", 6)]    // 5 ปีเต็ม + เศษ = 6
    [InlineData("2020-01-01", "2022-12-31", 3)]    // ครบ 3 ปีพอดี ไม่มีเศษ
    public void Years_counted_rounds_a_fraction_up(string hire, string last, int expected)
        => Assert.Equal(expected, SeveranceTaxCalculator.YearsCounted(DateOnly.Parse(hire, System.Globalization.CultureInfo.InvariantCulture), DateOnly.Parse(last, System.Globalization.CultureInfo.InvariantCulture)));
}
