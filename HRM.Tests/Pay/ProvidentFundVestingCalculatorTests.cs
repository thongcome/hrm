using HRM.Models;
using HRM.Services.Pay.Calculators;
using Xunit;

namespace HRM.Tests.Pay;

public class ProvidentFundVestingCalculatorTests
{
    private static readonly DateOnly HireDate = new(2020, 1, 1);

    private static List<Pay_ProvidentFundVestingTier> SampleTiers() =>
    [
        new() { Id = 1, PolicyId = 1, MinYearsOfService = 0, MaxYearsOfService = 1, VestingPercent = 10m, SortOrder = 1 },
        new() { Id = 2, PolicyId = 1, MinYearsOfService = 1, MaxYearsOfService = 3, VestingPercent = 50m, SortOrder = 2 },
        new() { Id = 3, PolicyId = 1, MinYearsOfService = 3, MaxYearsOfService = 5, VestingPercent = 80m, SortOrder = 3 },
        new() { Id = 4, PolicyId = 1, MinYearsOfService = 5, MaxYearsOfService = null, VestingPercent = 100m, SortOrder = 4 },
    ];

    [Fact]
    public void No_tiers_configured_means_full_vesting_not_a_silent_zero()
    {
        var result = ProvidentFundVestingCalculator.ResolveVesting(HireDate, HireDate.AddYears(2), tiers: []);

        Assert.Equal(100m, result.VestingPercent);
        Assert.Null(result.MatchedTierNote);
    }

    [Fact]
    public void Employee_within_the_first_tier_gets_that_tiers_percent()
    {
        var asOfDate = HireDate.AddMonths(6); // ~0.5 years -> tier 1 (0-1 year, 10%)

        var result = ProvidentFundVestingCalculator.ResolveVesting(HireDate, asOfDate, SampleTiers());

        Assert.Equal(10m, result.VestingPercent);
        Assert.NotNull(result.MatchedTierNote);
    }

    [Fact]
    public void Employee_in_a_middle_tier_gets_that_tiers_percent()
    {
        var asOfDate = HireDate.AddYears(2); // within [1,3) -> 50%

        var result = ProvidentFundVestingCalculator.ResolveVesting(HireDate, asOfDate, SampleTiers());

        Assert.Equal(50m, result.VestingPercent);
    }

    [Fact]
    public void Service_exceeding_every_configured_tier_ceiling_is_fully_vested()
    {
        var asOfDate = HireDate.AddYears(10); // beyond the last tier's MaxYearsOfService=null upper-open tier still matches at 100%

        var result = ProvidentFundVestingCalculator.ResolveVesting(HireDate, asOfDate, SampleTiers());

        Assert.Equal(100m, result.VestingPercent);
    }

    [Fact]
    public void AsOfDate_before_hireDate_throws()
    {
        Assert.Throws<ArgumentException>(() => ProvidentFundVestingCalculator.ResolveVesting(HireDate, HireDate.AddDays(-1), SampleTiers()));
    }
}
