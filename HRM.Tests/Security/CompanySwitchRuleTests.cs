using HRM.Services.Security;
using Xunit;

namespace HRM.Tests.Security;

// Company switcher is fail-closed (CEO, 26 ก.ย. 2569) — found by screen-testing: a UITEST payroll approver
// switched into ADVD and saw its payroll, then could not switch back home.
public class CompanySwitchRuleTests
{
    private static readonly List<CompanySwitchService.SwitchableCompany> Known = new()
    {
        new("ADVD", "Advance Digital"),
        new("ADHOLD", "AD Holding"),
        new("ADDIGITAL", "AD.Digital"),
        new("UITEST", "UI test"),
    };

    private static List<string> Codes(IEnumerable<CompanySwitchService.SwitchableCompany> list) => list.Select(c => c.Code).ToList();

    [Fact]
    public void A_role_with_no_company_scope_sees_only_its_home_company()
        => Assert.Equal(new[] { "UITEST" }, Codes(CompanySwitchService.Allowed(Known, isAdmin: false, Array.Empty<string>(), "UITEST", "UITEST")));

    [Fact]
    public void Admin_sees_every_company()
        => Assert.Equal(new[] { "ADDIGITAL", "ADHOLD", "ADVD", "UITEST" }, Codes(CompanySwitchService.Allowed(Known, isAdmin: true, Array.Empty<string>(), "ADVD", "ADVD")));

    [Fact]
    public void A_company_scope_grant_adds_exactly_those_companies()
        => Assert.Equal(new[] { "ADDIGITAL", "ADHOLD" }, Codes(CompanySwitchService.Allowed(Known, false, new[] { "ADDIGITAL" }, "ADHOLD", "ADHOLD")));

    [Fact]
    public void After_switching_away_the_home_company_is_still_offered()
    {
        var list = Codes(CompanySwitchService.Allowed(Known, false, new[] { "ADDIGITAL" }, homeCompany: "ADHOLD", currentCompany: "ADDIGITAL"));
        Assert.Contains("ADHOLD", list);
        Assert.Contains("ADDIGITAL", list);
        Assert.DoesNotContain("ADVD", list);
    }

    [Fact]
    public void A_company_without_a_master_row_is_listed_by_its_code()
    {
        var only = Assert.Single(CompanySwitchService.Allowed(Known, false, Array.Empty<string>(), "NEWCO", "NEWCO"));
        Assert.Equal("NEWCO", only.Name);
    }
}
