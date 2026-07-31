namespace HRM.Services.Login;

using System.Security.Claims;
using HRM.Data;
using HRM.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

// Bridges Microsoft.AspNetCore.Identity sign-in to the SC_* permission model.
// ApplicationUser.userid (added via AddApplicationUserIdentityFields
// migration) links 1:1 to sc_user.userid — the same PK the legacy
// /login-handler + CustomAuthStateProvider path already uses for role/menu
// claims. sc_user.empid (never populated before this) optionally links on to
// Hremployee.EmpNo for employee-scoped (ESS) accounts.
//
// Deliberately does NOT touch ClaimTypes.NameIdentifier — Identity's own
// UserManager/SignInManager/Manage-account pages resolve the current user
// via that claim expecting ApplicationUser.Id (the GUID), and overriding it
// would break those. The sc_user link rides on separate "sc_userid"/"empno"
// claims instead.
public class ScUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser>
{
    private readonly IDbContextFactory<HRMContext> _hrmDbFactory;

    public ScUserClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        IOptions<IdentityOptions> optionsAccessor,
        IDbContextFactory<HRMContext> hrmDbFactory)
        : base(userManager, optionsAccessor)
    {
        _hrmDbFactory = hrmDbFactory;
    }

    public override async Task<ClaimsPrincipal> CreateAsync(ApplicationUser user)
    {
        var principal = await base.CreateAsync(user);
        var identity = (ClaimsIdentity)principal.Identity!;

        await using var context = await _hrmDbFactory.CreateDbContextAsync();
        var scUser = await context.sc_users
            .Include(u => u.sc_user_roles).ThenInclude(ur => ur.role).ThenInclude(r => r.sc_role_menus).ThenInclude(rm => rm.menu)
            .FirstOrDefaultAsync(u => u.userid == user.userid);

        // No linked sc_user (or it's disabled) — Identity login still
        // succeeds, but the principal carries no SC_* role/menu claims, so
        // [Authorize(Roles=...)] checks and menu rendering see nothing.
        if (scUser is null || scUser.isdisable || scUser.iscancel || !scUser.isActivate)
            return principal;

        identity.AddClaim(new Claim("sc_userid", scUser.userid.ToString()));

        // Displayed in MainLayout's top bar instead of the login/email —
        // ClaimTypes.Name there is the ApplicationUser's UserName (often a
        // synthetic "{loginname}@local.humanok" placeholder for accounts
        // created without a real email), not a name a person recognizes.
        var fullName = $"{scUser.firstname} {scUser.lastname}".Trim();
        if (!string.IsNullOrWhiteSpace(fullName))
            identity.AddClaim(new Claim("fullname", fullName));

        if (!string.IsNullOrWhiteSpace(scUser.empid))
        {
            identity.AddClaim(new Claim("empno", scUser.empid));

            var companyId = await PayrollCompanyResolver.ResolveAsync(context, scUser.empid);
            if (!string.IsNullOrWhiteSpace(companyId))
                identity.AddClaim(new Claim("payroll_company", companyId));
        }

        foreach (var ur in scUser.sc_user_roles.Where(r => r.isactive))
        {
            if (!string.IsNullOrWhiteSpace(ur.role?.name))
                identity.AddClaim(new Claim(ClaimTypes.Role, ur.role!.name));
        }

        var menus = scUser.sc_user_roles
            .Where(ur => ur.isactive)
            .SelectMany(ur => ur.role?.sc_role_menus ?? new List<sc_role_menu>())
            .Where(rm => rm.isactive && rm.menu != null && rm.menu.isactive)
            .Select(rm => rm.menu!)
            .Distinct();
        foreach (var menu in menus)
        {
            if (!string.IsNullOrWhiteSpace(menu.menucode))
                identity.AddClaim(new Claim("menu", menu.menucode!));
        }

        return principal;
    }
}
