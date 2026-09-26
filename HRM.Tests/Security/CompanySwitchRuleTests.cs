using System.Security.Claims;
using HRM.Services.Security;
using Xunit;
using Company = HRM.Services.Security.CompanySwitchService.SwitchableCompany;

namespace HRM.Tests.Security;

// Decision D3 (CEO 26 ก.ย. 2569): a role with no Company scope may no longer
// switch to every company. Found by a UI test: uitest.checker
// (PAYROLL_APPROVER of UITEST, no scope rows) could open ADVD's payroll runs.
public class CompanySwitchRuleTests
{
    private static readonly Company[] All =
    [
        new("ADVD", "Advance Digital"),
        new("UITEST", "UI Test Co"),
        new("PTEST", "Payroll Test"),
    ];

    private static ClaimsPrincipal User(string? current, params (string Type, string Value)[] claims)
    {
        var list = claims.Select(c => new Claim(c.Type, c.Value)).ToList();
        if (current is not null) list.Add(new Claim("payroll_company", current));
        return new ClaimsPrincipal(new ClaimsIdentity(list, "test"));
    }

    private static string[] Codes(List<Company> companies) => companies.Select(c => c.Code).ToArray();

    [Fact]
    public void Role_without_company_scope_sees_only_its_own_company()
    {
        var user = User("UITEST", (ClaimTypes.Role, "PAYROLL_APPROVER"), ("scope_unrestricted", "1"));

        Assert.Equal(["UITEST"], Codes(CompanySwitchService.Allowed(All, user, homeCompany: "UITEST")));
    }

    [Fact]
    public void Admin_role_sees_every_company_whatever_the_case_of_the_name()
    {
        var user = User("ADVD", (ClaimTypes.Role, "admin"));

        Assert.Equal(["ADVD", "PTEST", "UITEST"], Codes(CompanySwitchService.Allowed(All, user, homeCompany: "ADVD")));
    }

    [Fact]
    public void Company_scope_grants_those_companies_even_alongside_an_unscoped_role()
    {
        // scope_unrestricted from another role must not widen switching to everything,
        // and must not hide the companies a scoped role grants either.
        var user = User("ADVD",
            (ClaimTypes.Role, "HR_GROUP"), ("scope_company", "PTEST"),
            (ClaimTypes.Role, "Employee"), ("scope_unrestricted", "1"));

        Assert.Equal(["ADVD", "PTEST"], Codes(CompanySwitchService.Allowed(All, user, homeCompany: "ADVD")));
    }

    [Fact]
    public void Home_company_stays_listed_after_switching_away_so_the_user_can_go_back()
    {
        var user = User("PTEST", (ClaimTypes.Role, "HR_GROUP"), ("scope_company", "PTEST"));

        Assert.Equal(["ADVD", "PTEST"], Codes(CompanySwitchService.Allowed(All, user, homeCompany: "ADVD")));
    }
}
