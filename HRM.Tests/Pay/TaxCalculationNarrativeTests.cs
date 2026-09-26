using HRM.Services.Pay;
using Xunit;

namespace HRM.Tests.Pay;

// ปุ่ม "ดูการคำนวณ" ที่บรรทัดภาษีต้องกางตัวเลขจริงให้เห็น ไม่ใช่ชี้ไปดูที่อื่น
public class TaxCalculationNarrativeTests
{
    private const string Sample = """
    {
      "EmpNo": "E001",
      "GrossEarnings": 41600.00,
      "YtdIncomeBeforeThisPeriod": 332800.00,
      "OneOffTaxableIncome": 0,
      "RemainingPeriods": 4,
      "RemainingMonths": 4,
      "PeriodsPerMonth": 1,
      "PriorEmployerIncomeIncluded": { "PriorEmployerName": "บริษัทเดิม", "IncomeAmount": 120000.00, "DeductionAmount": 3000.00, "TaxWithheldAmount": 5000.00 },
      "DeductionBreakdown": {
        "PersonalAllowancePerYear": 60000.00,
        "SocialSecurity": 750.00,
        "ProvidentFund": 0,
        "ElectedAnnualDeductions": 0,
        "ExpenseDeductionRate": 50.0,
        "ExpenseDeductionCap": 100000.00
      },
      "AnnualCalculation": {
        "TotalAnnualTax": 10520.00,
        "Breakdown": [
          { "Step": 1, "MinIncome": 0, "MaxIncome": 150000.00, "RatePercent": 0, "TaxableInBracket": 150000.00, "TaxInBracket": 0 },
          { "Step": 2, "MinIncome": 150000.00, "MaxIncome": 300000.00, "RatePercent": 5.0, "TaxableInBracket": 150000.00, "TaxInBracket": 7500.00 },
          { "Step": 3, "MinIncome": 300000.00, "MaxIncome": null, "RatePercent": 10.0, "TaxableInBracket": 30200.00, "TaxInBracket": 3020.00 }
        ]
      },
      "MonthlyWithholding": 876.67
    }
    """;

    [Fact]
    public void Shows_the_real_numbers_in_the_order_the_law_computes_them()
    {
        Assert.True(TaxCalculationNarrative.TryBuild(Sample, out var html));

        Assert.Contains("876.67", html);                    // ภาษีงวดนี้
        Assert.Contains("41,600.00", html);                 // เงินได้งวดนี้
        Assert.Contains("332,800.00", html);                // สะสมถึงงวดก่อน
        Assert.Contains("60,000.00", html);                 // ลดหย่อนส่วนตัว
        Assert.Contains("10,520.00", html);                 // ภาษีทั้งปี
        Assert.Contains("บริษัทเดิม", html);                 // นายจ้างเดิม
        Assert.Contains("5,000.00", html);                  // ภาษีที่นายจ้างเดิมหักไว้

        // ขั้นบันไดต้องมาครบทุกขั้นพร้อมอัตราและภาษีของแต่ละขั้น
        Assert.Contains("5%", html);
        Assert.Contains("7,500.00", html);
        Assert.Contains("10%", html);
        Assert.Contains("3,020.00", html);
        Assert.Contains("ขึ้นไป", html);                     // ขั้นบนสุดไม่มีเพดาน

        // ลำดับต้องเป็น เงินได้ → หัก → ขั้นบันได → เฉลี่ยลงงวด
        Assert.True(html.IndexOf("1. เงินได้") < html.IndexOf("2. หัก"));
        Assert.True(html.IndexOf("2. หัก") < html.IndexOf("3. ภาษีทั้งปี"));
        Assert.True(html.IndexOf("3. ภาษีทั้งปี") < html.IndexOf("4. เฉลี่ย"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ไม่ใช่ json")]
    [InlineData("{ \"MonthlyWithholding\": ")]
    public void Bad_or_missing_record_falls_back_instead_of_breaking_the_page(string? json)
        => Assert.False(TaxCalculationNarrative.TryBuild(json, out _));

    [Fact]
    public void Old_record_without_the_optional_parts_still_renders()
    {
        const string minimal = """{ "GrossEarnings": 30000, "MonthlyWithholding": 250 }""";
        Assert.True(TaxCalculationNarrative.TryBuild(minimal, out var html));
        Assert.Contains("250.00", html);
        Assert.Contains("30,000.00", html);
        Assert.DoesNotContain("นายจ้างเดิม", html);
    }

    // ภาษีเป็น 0 ต้องกางให้ดูได้และบอกเหตุผล (ทีม HRM เจอก่อน 26 ก.ย. 2569) — คำถามที่ HR
    // โดนถามบ่อยที่สุดคือ "ทำไมงวดนี้ไม่ถูกหักภาษี" และคำตอบมีสองแบบที่ต่างกันมาก
    [Fact]
    public void ภาษีเป็นศูนย์เพราะยังไม่ถึงเกณฑ์_บอกเหตุผลนั้น()
    {
        const string json = """
        {
          "GrossEarnings": 15000,
          "YtdIncomeBeforeThisPeriod": 120000,
          "AnnualCalculation": { "TotalAnnualTax": 0, "Breakdown": [] },
          "YtdTaxBeforeThisPeriod": 0,
          "MonthlyWithholding": 0
        }
        """;

        Assert.True(TaxCalculationNarrative.TryBuild(json, out var html));
        Assert.Contains("ทำไมงวดนี้ไม่ถูกหักภาษี", html);
        Assert.Contains("ยังไม่ถึงเกณฑ์", html);
        Assert.Contains("15,000.00", html);   // ยังกางตัวเลขให้ดูครบเหมือนเดิม
    }

    [Fact]
    public void ภาษีเป็นศูนย์เพราะหักครบทั้งปีแล้ว_บอกยอดที่หักไปแล้ว()
    {
        const string json = """
        {
          "GrossEarnings": 41600,
          "AnnualCalculation": { "TotalAnnualTax": 10520, "Breakdown": [] },
          "YtdTaxBeforeThisPeriod": 10520,
          "MonthlyWithholding": 0
        }
        """;

        Assert.True(TaxCalculationNarrative.TryBuild(json, out var html));
        Assert.Contains("ครบทั้งปีแล้ว", html);
        Assert.Contains("10,520.00", html);
        Assert.DoesNotContain("ยังไม่ถึงเกณฑ์", html);
    }

    [Fact]
    public void ภาษีไม่เป็นศูนย์_ไม่ต้องมีกล่องอธิบายว่าทำไมไม่ถูกหัก()
    {
        Assert.True(TaxCalculationNarrative.TryBuild(Sample, out var html));
        Assert.DoesNotContain("ทำไมงวดนี้ไม่ถูกหักภาษี", html);
    }
}
