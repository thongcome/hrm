using HRM.Models;
using HRM.Services.Pay;
using Xunit;

namespace HRM.Tests.Pay;

// audit H-03: the wage cap changes in steps, so the rate row is chosen by the period's start date.
public class SocialSecurityRateInForceTests
{
    private static Hrucfsecurity Row(string co, DateOnly? from, decimal cap, string code = HrucfsecurityRateProvider.CurrentEmployeeSecurityCode) =>
        new() { companyid = co, SecurityCode = code, EffectiveFrom = from, SecurityMoney = cap, PercenSecurity = 5m };

    private static decimal? CapOn(IEnumerable<Hrucfsecurity> rows, string co, DateOnly date) =>
        HrucfsecurityRateProvider.InForce(rows.AsQueryable(), co, date).FirstOrDefault()?.SecurityMoney;

    [Fact]
    public void A_single_undated_row_applies_to_every_date()
    {
        var rows = new[] { Row("C1", null, 15000m) };
        Assert.Equal(15000m, CapOn(rows, "C1", new DateOnly(2020, 1, 1)));
        Assert.Equal(15000m, CapOn(rows, "C1", new DateOnly(2030, 1, 1)));
    }

    [Fact]
    public void The_latest_row_not_after_the_date_wins()
    {
        var rows = new[]
        {
            Row("C1", null, 15000m),
            Row("C1", new DateOnly(2026, 1, 1), 17500m),
            Row("C1", new DateOnly(2029, 1, 1), 20000m),
        };
        Assert.Equal(15000m, CapOn(rows, "C1", new DateOnly(2025, 12, 31)));
        Assert.Equal(17500m, CapOn(rows, "C1", new DateOnly(2026, 1, 1)));
        Assert.Equal(17500m, CapOn(rows, "C1", new DateOnly(2028, 12, 31)));
        Assert.Equal(20000m, CapOn(rows, "C1", new DateOnly(2029, 1, 1)));
    }

    [Fact]
    public void Nothing_applies_before_the_first_dated_row_when_there_is_no_undated_row()
    {
        var rows = new[] { Row("C1", new DateOnly(2026, 1, 1), 17500m) };
        Assert.Null(CapOn(rows, "C1", new DateOnly(2025, 12, 31)));
    }

    [Fact]
    public void Other_companies_and_other_security_codes_are_ignored()
    {
        var rows = new[]
        {
            Row("C2", null, 99999m),
            Row("C1", null, 88888m, code: "02"),
            Row("C1", null, 15000m),
        };
        Assert.Equal(15000m, CapOn(rows, "C1", new DateOnly(2026, 6, 1)));
    }
}
