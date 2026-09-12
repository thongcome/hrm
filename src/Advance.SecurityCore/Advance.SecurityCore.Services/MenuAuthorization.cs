namespace Advance.SecurityCore.Services;

using Microsoft.AspNetCore.Authorization;

// Copied from HRM's Services/Login/MenuAuthorization.cs, with ONE deliberate
// omission: the "Program:" policy branch (ProgramRequirement /
// ProgramAuthorizationHandler) is NOT carried over. CLAUDE.md calls that
// mechanism (Services/Login/ProgramAuthorization.cs in HRM) "a superseded
// proof-of-concept — don't extend it to new pages"; AD.CRUDManage
// (ProgramRoleService, copied separately) is the mechanism that replaced it.
// Carrying the POC into a brand-new package would re-legitimize exactly the
// pattern CLAUDE.md says not to extend. See EXTRACTION-PLAN.md.
//
// A page declares what it needs once:
//
//   @attribute [Authorize(Policy = "Menu:PAY_RUNS")]
//
// and MenuPolicyProvider builds the requirement dynamically from the policy
// name — no need to pre-register a policy per menu code. The actual
// permission data (which role sees which menucode) lives in
// sc_menu/sc_role_menu, managed through admin pages; this only enforces
// what's already stored there against the "menu" claims
// ScUserClaimsPrincipalFactory attaches at sign-in.
public class MenuRequirement : IAuthorizationRequirement
{
    public string MenuCode { get; }
    public MenuRequirement(string menuCode) => MenuCode = menuCode;
}

public class MenuAuthorizationHandler : AuthorizationHandler<MenuRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, MenuRequirement requirement)
    {
        if (context.User.HasClaim("menu", requirement.MenuCode))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}

public class MenuPolicyProvider : IAuthorizationPolicyProvider
{
    private const string MenuPrefix = "Menu:";
    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public MenuPolicyProvider(Microsoft.Extensions.Options.IOptions<AuthorizationOptions> options)
    {
        _fallback = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(MenuPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var menuCode = policyName[MenuPrefix.Length..];
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new MenuRequirement(menuCode))
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallback.GetPolicyAsync(policyName);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();
}
