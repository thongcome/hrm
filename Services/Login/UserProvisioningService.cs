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
    private readonly IDbContextFactory<HRMContext> _dbFactory;
    private readonly ILogger<UserProvisioningService> _logger;

    public UserProvisioningService(
        UserManager<ApplicationUser> userManager,
        IDbContextFactory<HRMContext> dbFactory,
        ILogger<UserProvisioningService> logger)
    {
        _userManager = userManager;
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public record ProvisionResult(bool Succeeded, string? Error);

    // Idempotent: if an ApplicationUser is already linked to this userid
    // (e.g. someone used /admin/link-identity-account first), this is a
    // successful no-op — callers can call it unconditionally right after
    // saving a new sc_user without checking first.
    public async Task<ProvisionResult> EnsureIdentityLinkedAsync(sc_user scUser, string password, string? email = null, CancellationToken ct = default)
    {
        var existing = await _userManager.Users.FirstOrDefaultAsync(u => u.userid == scUser.userid, ct);
        if (existing is not null)
        {
            // Still a "set user ครั้งแรก" opportunity: the guard inside skips
            // anyone who already has a role row, so this stays idempotent.
            await EnsureDefaultRoleAsync(scUser, ct);
            return new ProvisionResult(true, null);
        }

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
        if (!result.Succeeded)
            return new ProvisionResult(false, string.Join(", ", result.Errors.Select(e => e.Description)));

        await EnsureDefaultRoleAsync(scUser, ct);
        return new ProvisionResult(true, null);
    }

    // CEO access-control step 3 (2026-09-01): ตอน set user ครั้งแรก ให้ role
    // อัตโนมัติตาม employeetype — HREMPLOYEE.EMPTYPE_CODE matched against
    // sc_role.employeetype_code ('01' → Employee, '02' → กรรมการ, per the
    // EmployeeTypeRoleSeeder defaults; the mapping itself is admin-editable
    // on /admin/system/roles).
    //
    // Deliberately conservative: a user who already has ANY sc_user_role row
    // (active or not — a deactivated role is a human decision, not a gap) is
    // never touched, and every fallback path degrades to the role named
    // 'Employee' or to a logged no-op rather than an error — role assignment
    // must never break account provisioning itself.
    private async Task EnsureDefaultRoleAsync(sc_user scUser, CancellationToken ct)
    {
        try
        {
            await using var context = await _dbFactory.CreateDbContextAsync(ct);

            var hasAnyRole = await context.sc_user_roles
                .AnyAsync(ur => ur.userid == scUser.userid, ct);
            if (hasAnyRole) return;

            // sc_user.empid = Hremployee.EmpNo (same linkage
            // ScUserClaimsPrincipalFactory resolves the empno claim through).
            string? emptypeCode = null;
            if (!string.IsNullOrWhiteSpace(scUser.empid))
            {
                emptypeCode = await context.Hremployee
                    .Where(e => e.EmpNo == scUser.empid)
                    .Select(e => e.EmptypeCode)
                    .FirstOrDefaultAsync(ct);
            }

            sc_role? role = null;
            if (!string.IsNullOrWhiteSpace(emptypeCode))
            {
                role = await context.sc_roles
                    .FirstOrDefaultAsync(r => r.isactive && r.employeetype_code == emptypeCode, ct);
            }

            // Fallback chain: employee not found / EMPTYPE_CODE null / no role
            // mapped to that code → the plain 'Employee' role if it exists.
            role ??= await context.sc_roles
                .FirstOrDefaultAsync(r => r.isactive && r.name == "Employee", ct);

            if (role is null)
            {
                _logger.LogWarning(
                    "Auto-role skipped for sc_user {UserId} (empid {EmpId}): no sc_role mapped to employeetype '{EmptypeCode}' and no 'Employee' role exists",
                    scUser.userid, scUser.empid, emptypeCode);
                return;
            }

            // Row shape mirrors existing sc_user_role rows (e.g. AutoxDemoSeeder's
            // and UserAdmin's): identity user_roleID, isactive=1, empid carried
            // over, startdate/enddate left NULL.
            context.sc_user_roles.Add(new sc_user_role
            {
                roleid = role.roleid,
                userid = scUser.userid,
                empid = scUser.empid,
                isactive = true,
                modate = DateTime.Now,
                modby = "UserProvisioningService",
            });
            await context.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            // Never let auto-role failure surface as a provisioning failure —
            // the identity link already succeeded and the admin can assign a
            // role by hand on /admin/system/users.
            _logger.LogWarning(ex, "Auto-role assignment failed for sc_user {UserId} (empid {EmpId})", scUser.userid, scUser.empid);
        }
    }
}
