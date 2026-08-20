namespace HRM.Services.Login;

using HRM.Data;
using HRM.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

// Single place that creates the ApplicationUser linked to a newly-created
// sc_user. Before this existed, LoginEndpoints.cs's requirement that every
// sc_user have a linked ApplicationUser before it can sign in (see its
// comment on the "no linked ApplicationUser yet" fallthrough) was satisfied
// only by whichever creation path happened to build both rows itself
// (PayrollEmployeeAdmin.razor's CreateUserAccountAsync did; UserAdmin.razor
// and the sc_userPages scaffold did not), leaving accounts created the other
// way unable to log in with no obvious error pointing at the real cause.
//
// Mirrors the shape both existing manual paths already use —
// LinkIdentityAccount.razor and PayrollEmployeeAdmin.razor's
// CreateUserAccountAsync — rather than inventing a new one:
// UserManager.CreateAsync(user, password) single-step, EmailConfirmed=true
// set manually (no confirmation email flow exists), userid FK set directly
// to the sc_user this is linked to. UserName is a synthetic
// "{loginname}@local.humanok" value rather than a real email: LoginEndpoints
// looks accounts up by sc_user.loginname only, never by
// ApplicationUser.UserName/.Email, so this only needs to be unique within
// Identity's own storage — a synthetic value sidesteps collisions between
// two employees who share (or both lack) a real email address.
public class UserProvisioningService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserProvisioningService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public record ProvisionResult(bool Succeeded, string? Error);

    // Idempotent: if an ApplicationUser is already linked to this userid
    // (e.g. someone used /admin/link-identity-account first), this is a
    // successful no-op — callers can call it unconditionally right after
    // saving a new sc_user without checking first.
    public async Task<ProvisionResult> EnsureIdentityLinkedAsync(sc_user scUser, string password, string? email = null, CancellationToken ct = default)
    {
        var existing = await _userManager.Users.FirstOrDefaultAsync(u => u.userid == scUser.userid, ct);
        if (existing is not null) return new ProvisionResult(true, null);

        var appUser = new ApplicationUser
        {
            UserName = $"{scUser.loginname}@local.humanok",
            Email = string.IsNullOrWhiteSpace(email) ? $"{scUser.loginname}@local.humanok" : email,
            EmailConfirmed = true,
            FirstName = scUser.firstname,
            LastName = scUser.lastname,
            userid = scUser.userid,
        };

        var result = await _userManager.CreateAsync(appUser, password);
        return result.Succeeded
            ? new ProvisionResult(true, null)
            : new ProvisionResult(false, string.Join(", ", result.Errors.Select(e => e.Description)));
    }
}
