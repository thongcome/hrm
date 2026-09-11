using HRM.Models;
using HRM.Services.Pay.Calculators;
using Xunit;

namespace HRM.Tests.Pay;

// เงินได้จ่ายครั้งเดียวในรอบปกติ (ค่าคอมมิชชัน/โบนัสที่ HR ใส่เป็นรายการเฉพาะกิจ) ต้องไม่ถูกคูณเดือนที่เหลือ
// พบจากเทสเงินเดือนทั้งปี 2568: ค่าคอมฯ 20,000 ใน มิ.ย. ทำภาษีเดือนนั้นกระโดดเกือบเท่าตัว
public class OneOffIncomeWithholdingTests
{
    private static List<Pay_TaxBracket> Brackets() => new()
    {
        new() { Step = 1, MinIncome = 0, MaxIncome = 150000, RatePercent = 0, IsActive = true },
        new() { Step = 2, MinIncome = 150000, MaxIncome = 300000, RatePercent = 5, IsActive = true },
        new() { Step = 3, MinIncome = 300000, MaxIncome = 500000, RatePercent = 10, IsActive = true },
        new() { Step = 4, MinIncome = 500000, MaxIncome = 750000, RatePercent = 15, IsActive = true },
        new() { Step = 5, MinIncome = 750000, MaxIncome = 1000000, RatePercent = 20, IsActive = true },
        new() { Step = 6, MinIncome = 1000000, MaxIncome = null, RatePercent = 25, IsActive = true },
    };

    [Fact]
    public void One_off_income_is_taxed_by_difference_not_annualised()
    {
        // มิ.ย.: เงินเดือน 80,000 + ค่าคอมฯ 20,000, สะสม 5 เดือน = 400,000, หัก 3,150/เดือน, ลดหย่อน 60,000
        var (withOneOff, _) = TaxBracketCalculator.CalculatePeriodWithholding(
            400000m, 100000m, 15750m, 3150m, 0.5m, 100000m, 7m, 7, 1, 20000m, Brackets(), 60000m, thisPeriodOneOffIncome: 20000m);
        var (regularOnly, regularAnnual) = TaxBracketCalculator.CalculatePeriodWithholding(
            400000m, 80000m, 15750m, 3150m, 0.5m, 100000m, 7m, 7, 1, 20000m, Brackets(), 60000m);
        var (annualised, _) = TaxBracketCalculator.CalculatePeriodWithholding(
            400000m, 100000m, 15750m, 3150m, 0.5m, 100000m, 7m, 7, 1, 20000m, Brackets(), 60000m);

        // ประมาณการทั้งปี = 400,000 + 80,000 × 7 = 960,000; หัก 15,750 + 22,050 + 60,000 + 100,000 → สุทธิ 762,200
        // ส่วนต่างของ 20,000 ที่ขั้น 20% = 4,000 (762,200 → 782,200 อยู่ในขั้น 750,000–1,000,000 ทั้งก้อน)
        Assert.Equal(regularOnly + 4000m, withOneOff);
        // ถ้าทั้งก้อนอยู่ในขั้นภาษีเดียวกัน สองวิธีให้ตัวเลขเท่ากันพอดี (28,000 ÷ 7 = 4,000) — ต่างกันเมื่อการคูณเดือนดันข้ามขั้น
        Assert.True(annualised >= withOneOff, "แบบเดิมคูณ 7 เดือนต้องไม่หักน้อยกว่าแบบส่วนต่าง");
        Assert.Equal(762200m, regularAnnual.Breakdown.Sum(b => b.TaxableAmountInBracket));
    }

    [Fact]
    public void Zero_one_off_is_identical_to_the_plain_formula()
    {
        var a = TaxBracketCalculator.CalculatePeriodWithholding(100000m, 50000m, 5000m, 2000m, 0.5m, 100000m, 10m, 10, 1, 1000m, Brackets(), 60000m);
        var b = TaxBracketCalculator.CalculatePeriodWithholding(100000m, 50000m, 5000m, 2000m, 0.5m, 100000m, 10m, 10, 1, 1000m, Brackets(), 60000m, thisPeriodOneOffIncome: 0m);
        Assert.Equal(a.MonthlyWithholding, b.MonthlyWithholding);
        Assert.Equal(a.AnnualCalculation.TotalAnnualTax, b.AnnualCalculation.TotalAnnualTax);
    }
}
