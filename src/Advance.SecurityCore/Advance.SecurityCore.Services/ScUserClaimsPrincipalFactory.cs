namespace Advance.SecurityCore.Services;

using System.Security.Claims;
using Advance.SecurityCore.Data;
using Advance.SecurityCore.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

// Adapted from HRM's Services/Login/ScUserClaimsPrincipalFactory.cs. Bridges
// Microsoft.AspNetCore.Identity sign-in to the SC_* permission model —
// ApplicationUser.userid links 1:1 to sc_user.userid.
//
// Deliberately does NOT touch ClaimTypes.NameIdentifier — Identity's own
// UserManager/SignInManager/Manage-account pages resolve the current user
// via that claim expecting ApplicationUser.Id (the GUID); the sc_user link
// rides on separate "sc_userid" claim instead.
//
// Changes from the HRM original (see EXTRACTION-PLAN.md "claims
// classification" for the full generic-vs-host-specific table):
//   - sc_role_scope and sc_role_program claim emission REMOVED — both are
//     HRM-specific permission slices (data-scope restriction; the superseded
//     Program:XXX proof-of-concept) layered on top of the base role/menu
//     model that this extraction's Domain project does not carry.
//   - sc_user_session stamping REMOVED — that table was not part of this
//     extraction's requested entity list (see sc_user.cs). A host wanting
//     session-revocation tracking adds it via IHostClaimsEnricher or a
//     later SecurityCore version that adds the table back.
//   - "empno" / "payroll_company" claim emission REPLACED by a call to
//     IHostClaimsEnricher.EnrichAsync — see that file for why.
public class ScUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser>
{
    public const string ForcePasswordChangeClaimType = "pwd_change_required";

    private readonly IDbContextFactory<SecurityDbContext> _dbFactory;
    private readonly PasswordPolicyService _passwordPolicy;
    private readonly IHostClaimsEnricher _hostClaimsEnricher;

    public ScUserClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        IOptions<IdentityOptions> optionsAccessor,
        IDbContextFactory<SecurityDbContext> dbFactory,
        PasswordPolicyService passwordPolicy,
        IHostClaimsEnricher hostClaimsEnricher)
        : base(userManager, optionsAccessor)
    {
        _dbFactory = dbFactory;
        _passwordPolicy = passwordPolicy;
        _hostClaimsEnricher = hostClaimsEnricher;
    }

    public override async Task<ClaimsPrincipal> CreateAsync(ApplicationUser user)
    {
        var principal = await base.CreateAsync(user);
        var identity = (ClaimsIdentity)principal.Identity!;

        await using var context = await _dbFactory.CreateDbContextAsync();
        var scUser = await context.sc_users
            .Include(u => u.sc_user_roles).ThenInclude(ur => ur.role).ThenInclude(r => r.sc_role_menus).ThenInclude(rm => rm.menu)
            .FirstOrDefaultAsync(u => u.userid == user.userid);

        // No linked sc_user (or it's disabled) — Identity login still
        // succeeds, but the principal carries no SC_* role/menu claims.
        if (scUser is null || scUser.isdisable || scUser.iscancel || !scUser.isActivate)
            return principal;

        identity.AddClaim(new Claim("sc_userid", scUser.userid.ToString()));
        identity.AddClaim(new Claim("permversion", scUser.permversion.ToString()));

        // Password-policy gate — Middleware/ForcePasswordChangeMiddleware
        // pins any request carrying this claim to the change-password form.
        if (_passwordPolicy.MustChangePassword(scUser))
        {
            identity.AddClaim(new Claim(
                ForcePasswordChangeClaimType,
                scUser.isforcechanged ? "forced" : "expired"));
        }
        else if (_passwordPolicy.DaysUntilExpiryWarning(scUser) is int daysLeft)
        {
            // Informational only — read by the host's own layout to warn
            // before the hard gate above kicks in.
            identity.AddClaim(new Claim("pwd_expires_in_days", daysLeft.ToString()));
        }

        // Displayed in the host's top bar instead of the login/email.
        var fullName = $"{scUser.firstname} {scUser.lastname}".Trim();
        if (!string.IsNullOrWhiteSpace(fullName))
            identity.AddClaim(new Claim("fullname", fullName));

        foreach (var ur in scUser.sc_user_roles.Where(r => r.isactive))
        {
            if (!string.IsNullOrWhiteSpace(ur.role?.name))
                identity.AddClaim(new Claim(ClaimTypes.Role, ur.role!.name));
        }

        var activeGrants = scUser.sc_user_roles
            .Where(ur => ur.isactive)
            .SelectMany(ur => ur.role?.sc_role_menus ?? new List<sc_role_menu>())
            .Where(rm => rm.isactive && rm.menu != null && rm.menu.isactive)
            .Distinct();
        foreach (var grant in activeGrants)
        {
            if (string.IsNullOrWhiteSpace(grant.menu!.menucode)) continue;
            identity.AddClaim(new Claim("menu", grant.menu.menucode!));
            // Read-only lock: a grant can give menu access without edit
            // rights.
            if (grant.canedit)
                identity.AddClaim(new Claim("menu_edit", grant.menu.menucode!));
        }

        // Host-specific claims (empno, payroll_company, or whatever else a
        // particular product needs) — see IHostClaimsEnricher.cs.
        await _hostClaimsEnricher.EnrichAsync(identity, scUser);

        return principal;
    }
}
