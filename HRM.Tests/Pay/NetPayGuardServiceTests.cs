using HRM.Services.Pay.Calculators;
using Xunit;

namespace HRM.Tests.Pay;

public class NetPayGuardServiceTests
{
    [Fact]
    public void Positive_net_pay_passes_through_unchanged()
    {
        var result = NetPayGuardService.Ensure(15000m);

        Assert.Equal(15000m, result.AdjustedNetPay);
        Assert.False(result.WasNegative);
        Assert.Equal(0m, result.ShortfallAmount);
    }

    [Fact]
    public void Negative_net_pay_is_clipped_to_zero_and_flagged()
    {
        // e.g. deductions (loan + tax + SSO) exceeded gross earnings this period
        var result = NetPayGuardService.Ensure(-2500m);

        Assert.Equal(0m, result.AdjustedNetPay);
        Assert.True(result.WasNegative);
        Assert.Equal(2500m, result.ShortfallAmount);
    }

    [Fact]
    public void Exactly_zero_net_pay_is_not_flagged_as_negative()
    {
        var result = NetPayGuardService.Ensure(0m);
        Assert.False(result.WasNegative);
    }
}
