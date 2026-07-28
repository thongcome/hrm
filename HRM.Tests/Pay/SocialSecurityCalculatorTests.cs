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
    public void Zero_cap_means_uncapped()
    {
        var amount = SocialSecurityCalculator.Calculate(grossWage: 50000m, ratePercent: 5m, wageCap: 0m);
        Assert.Equal(2500m, amount);
    }
}
