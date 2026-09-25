using HRM.Services.Pay.Calculators;
using Xunit;

namespace HRM.Tests.Pay;

public class Section76CheckTests
{
    [Fact]
    public void Within_both_limits_is_fine()
    {
        var byCategory = new Dictionary<string, decimal> { ["กองทุน"] = 1500m, ["เงินกู้"] = 2000m };
        Assert.Null(Section76Check.Evaluate(30000m, byCategory));
    }

    [Fact]
    public void One_category_above_ten_percent_is_flagged()
    {
        var byCategory = new Dictionary<string, decimal> { ["เงินกู้"] = 3500m };
        var finding = Section76Check.Evaluate(30000m, byCategory);
        Assert.NotNull(finding);
        Assert.Equal(new[] { "เงินกู้" }, finding!.CategoriesOverTenPercent);
    }

    [Fact]
    public void Total_above_one_fifth_is_flagged_even_when_each_is_under_ten_percent()
    {
        // 2,900 × 3 = 8,700 > 6,000 (1/5 of 30,000); each is under 3,000
        var byCategory = new Dictionary<string, decimal> { ["a"] = 2900m, ["b"] = 2900m, ["c"] = 2900m };
        var finding = Section76Check.Evaluate(30000m, byCategory);
        Assert.NotNull(finding);
        Assert.Empty(finding!.CategoriesOverTenPercent);
        Assert.Equal(6000m, finding.Limit);
    }

    [Fact]
    public void No_pay_means_nothing_to_check()
    {
        Assert.Null(Section76Check.Evaluate(0m, new Dictionary<string, decimal> { ["เงินกู้"] = 100m }));
    }
}
