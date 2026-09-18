using HRM.Services.Pay.Calculators;
using Xunit;

namespace HRM.Tests.Pay;

public class SocialSecurityCalculatorTests
{
    [Fact]
    public void Wage_below_cap_is_taxed_on_full_wage()
    {
        var amount = SocialSecurityCalculator.Calculate(grossWage: 10000m, ratePercent: 5m, wageCap: 15000m);
        Assert.Equal(500m, amount);
    }

    [Fact]
    public void Wage_above_cap_is_taxed_only_up_to_the_cap()
    {
        var amount = SocialSecurityCalculator.Calculate(grossWage: 50000m, ratePercent: 5m, wageCap: 15000m);
        Assert.Equal(750m, amount); // 15,000 * 5%, not 50,000 * 5%
    }

    [Fact]
    public void The_2026_ceiling_of_17500_gives_875()
    {
        Assert.Equal(875m, SocialSecurityCalculator.Calculate(grossWage: 50000m, ratePercent: 5m, wageCap: 17500m));
    }

    [Theory]
    [InlineData(1000)]
    [InlineData(1649.99)]
    [InlineData(1650)]
    public void Wage_below_1650_is_floored_at_1650(decimal wage)
    {
        // พ.ร.บ.ประกันสังคม: ฐานค่าจ้างต่ำสุด 1,650 → 5% = 82.50
        Assert.Equal(82.50m, SocialSecurityCalculator.Calculate(grossWage: wage, ratePercent: 5m, wageCap: 15000m));
    }

    [Fact]
    public void No_wage_means_no_contribution()
    {
        Assert.Equal(0m, SocialSecurityCalculator.Calculate(grossWage: 0m, ratePercent: 5m, wageCap: 15000m));
    }

    [Fact]
    public void Missing_ceiling_stops_the_calculation_instead_of_charging_the_full_salary()
    {
        Assert.Throws<InvalidOperationException>(() => SocialSecurityCalculator.Calculate(grossWage: 50000m, ratePercent: 5m, wageCap: 0m));
    }
}
