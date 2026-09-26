using HRM.Models;
using HRM.Services.Pay.Calculators;
using Xunit;

namespace HRM.Tests.Pay;

public class TaxDeductionRulesTests
{
    private static TaxDeductionRules.Election Fixed(string code, decimal paid, decimal max, string? group = null, decimal? groupCap = null, int sort = 0)
        => new(sort, code, code, TaxDeductionCalcMethod.FixedCap, paid, null, max, null, null, null, null, group, groupCap);

    private static TaxDeductionRules.Election PerPerson(string code, int persons, decimal each, int? maxPersons = null)
        => new(0, code, code, TaxDeductionCalcMethod.PerPerson, 0m, persons, 0m, each, maxPersons, null, null, null, null);

    private static TaxDeductionRules.Election Percent(string code, decimal paid, decimal pct, decimal max, TaxDeductionPercentBase b,
        string? group = null, decimal? groupCap = null, int sort = 0)
        => new(sort, code, code, TaxDeductionCalcMethod.PercentOfIncome, paid, null, max, null, null, pct, b, group, groupCap);

    [Fact]
    public void Fixed_cap_takes_the_amount_paid_up_to_the_cap()
    {
        var (items, _) = TaxDeductionRules.ResolveIncomeBased(new[] { Fixed("HOME_LOAN_INTEREST", 120000m, 100000m) }, 600000m);
        Assert.Equal(100000m, items.Single().Amount);
    }

    [Fact]
    public void Per_person_multiplies_by_people_and_respects_the_person_limit()
    {
        var (items, _) = TaxDeductionRules.ResolveIncomeBased(new[]
        {
            PerPerson("CHILD", 3, 30000m),
            PerPerson("PARENT", 6, 30000m, maxPersons: 4),
            PerPerson("SPOUSE", 1, 60000m, maxPersons: 1),
        }, 600000m);
        Assert.Equal(90000m, items.Single(i => i.Code == "CHILD").Amount);
        Assert.Equal(120000m, items.Single(i => i.Code == "PARENT").Amount);
        Assert.Equal(60000m, items.Single(i => i.Code == "SPOUSE").Amount);
    }

    [Fact]
    public void Percent_of_income_is_capped_by_percent_and_baht()
    {
        // RMF 30% of 600,000 = 180,000; paid 250,000 → 180,000
        var (items, _) = TaxDeductionRules.ResolveIncomeBased(new[] { Percent("RMF", 250000m, 30m, 500000m, TaxDeductionPercentBase.AssessableIncome) }, 600000m);
        Assert.Equal(180000m, items.Single().Amount);
    }

    [Fact]
    public void Group_cap_is_shared_in_sort_order_and_reported_for_pvd()
    {
        var (items, retirement) = TaxDeductionRules.ResolveIncomeBased(new[]
        {
            Percent("RMF", 400000m, 30m, 500000m, TaxDeductionPercentBase.AssessableIncome, "RETIREMENT", 500000m, sort: 1),
            Percent("SSF", 200000m, 30m, 200000m, TaxDeductionPercentBase.AssessableIncome, "RETIREMENT", 500000m, sort: 2),
        }, 2000000m);
        Assert.Equal(400000m, items.Single(i => i.Code == "RMF").Amount);
        Assert.Equal(100000m, items.Single(i => i.Code == "SSF").Amount);
        Assert.Equal(500000m, retirement);
    }

    [Fact]
    public void Life_and_health_share_one_hundred_thousand()
    {
        var (items, _) = TaxDeductionRules.ResolveIncomeBased(new[]
        {
            Fixed("LIFE_INSURANCE", 90000m, 100000m, "LIFE_HEALTH", 100000m, sort: 1),
            Fixed("HEALTH_INSURANCE", 25000m, 25000m, "LIFE_HEALTH", 100000m, sort: 2),
        }, 600000m);
        Assert.Equal(90000m, items.Single(i => i.Code == "LIFE_INSURANCE").Amount);
        Assert.Equal(10000m, items.Single(i => i.Code == "HEALTH_INSURANCE").Amount);
    }

    [Fact]
    public void Donation_is_ten_percent_of_net_income_and_only_resolved_in_the_net_pass()
    {
        var donation = Percent("DONATION", 50000m, 10m, 0m, TaxDeductionPercentBase.NetIncomeAfterDeductions);
        var (incomeItems, _) = TaxDeductionRules.ResolveIncomeBased(new[] { donation }, 600000m);
        Assert.Empty(incomeItems);
        Assert.Equal(30000m, TaxDeductionRules.ResolveNetBased(new[] { donation }, 300000m).Single().Amount);
        Assert.Equal(0m, TaxDeductionRules.ResolveNetBased(new[] { donation }, -5000m).Single().Amount);
    }
}
