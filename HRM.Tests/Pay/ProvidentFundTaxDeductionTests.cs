using HRM.Services.Pay.Calculators;
using Xunit;

namespace HRM.Tests.Pay;

public class ProvidentFundTaxDeductionTests
{
    [Fact]
    public void Normal_contribution_is_fully_deductible()
    {
        var room = ProvidentFundTaxDeduction.AnnualRoom(500000m, 0m, 0m);
        Assert.Equal(1500m, ProvidentFundTaxDeduction.Deductible(1500m, wageBase: 30000m, room));
    }

    [Fact]
    public void Contribution_above_fifteen_percent_of_wage_is_capped()
    {
        // 20% of 30,000 = 6,000 contributed; only 15% = 4,500 lowers tax
        var room = ProvidentFundTaxDeduction.AnnualRoom(500000m, 0m, 0m);
        Assert.Equal(4500m, ProvidentFundTaxDeduction.Deductible(6000m, 30000m, room));
    }

    [Fact]
    public void Elected_rmf_ssf_shares_the_500k_cap()
    {
        // 400,000 RMF/SSF elected + 90,000 PVD already deducted → 10,000 left
        var room = ProvidentFundTaxDeduction.AnnualRoom(500000m, retirementGroupElected: 400000m, ytdPvdDeducted: 90000m);
        Assert.Equal(10000m, room);
        Assert.Equal(10000m, ProvidentFundTaxDeduction.Deductible(45000m, wageBase: 300000m, room));
    }

    [Fact]
    public void No_room_left_means_nothing_deductible()
    {
        var room = ProvidentFundTaxDeduction.AnnualRoom(500000m, 500000m, 0m);
        Assert.Equal(0m, room);
        Assert.Equal(0m, ProvidentFundTaxDeduction.Deductible(3000m, 30000m, room));
    }

    [Fact]
    public void Room_never_goes_negative()
    {
        Assert.Equal(0m, ProvidentFundTaxDeduction.AnnualRoom(500000m, 450000m, 100000m));
    }
}
