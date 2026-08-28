namespace HRM.Services.Login;

using Microsoft.AspNetCore.Authorization;

// Policy-based replacement for hand-written "does this user have menu X"
// checks scattered per page. A page declares what it needs once:
//
//   @attribute [Authorize(Policy = "Menu:PAY_RUNS")]
//
// and MenuPolicyProvider builds the requirement dynamically from the policy
// name — no need to pre-register a policy per menu code in Program.cs every
// time a new page is added. The actual permission data (which role sees
// which menucode) still lives entirely in sc_menu/sc_role_menu, managed
// through the existing sc_role_menu admin pages; this only enforces what's
// already stored there against the "menu" claims ScUserClaimsPrincipalFactory
// attaches at sign-in.
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
    // Only one IAuthorizationPolicyProvider can be registered at a time
    // (Program.cs: AddSingleton<IAuthorizationPolicyProvider, ...> — a
    // second registration would just replace this one), so the sibling
    // "Program:" prefix (per-action Create/Edit/Delete checks, see
    // ProgramAuthorization.cs) is resolved from here too rather than from a
    // second provider class.
    private const string ProgramPrefix = "Program:";
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

        if (policyName.StartsWith(ProgramPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var programCode = policyName[ProgramPrefix.Length..];
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new ProgramRequirement(programCode))
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallback.GetPolicyAsync(policyName);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();
}
