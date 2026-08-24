using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

// Advance Security slice 2 (sc_user.permversion + sc_user_session):
// auto-bumps permversion for every user whose effective rights just
// changed — same "automatic, no per-page code" philosophy as
// HRMContext.Audit.cs's audit trail. Hooked from that file's two
// SaveChanges(Async) overrides rather than duplicating them here, since C#
// only allows one implementing body per virtual method override even
// across partial-class fragments. See ScUserClaimsPrincipalFactory (bakes
// the permversion/sessionid claims at sign-in) and
// IdentityRevalidatingAuthenticationStateProvider (compares them on
// revalidation, and forces a sign-out on mismatch or a revoked session) for
// the other two-thirds of this feature.
public partial class HRMContext
{
    public DbSet<sc_user_session> sc_user_sessions { get; set; } = null!;

    // Set by CollectDirectlyAffectedUserIds, consumed by
    // BumpAffectedPermVersionsAsync — role ids from sc_role_menu/
    // sc_role_scope changes, which need a live "who's currently in this
    // role" query that can only run after the collect step (it can't do
    // the DB round-trip itself, since it must stay usable from the sync
    // SaveChanges path too — see the comment on that path in
    // HRMContext.Audit.cs for why permversion bumping is async-only there).
    private HashSet<long>? _pendingAffectedRoleIds;

    // Called BEFORE the real save, while a Deleted/Modified entry's own CLR
    // properties (or OriginalValues) still reflect pre-save state — a
    // removed sc_user_role's userid, or a just-edited sc_role_menu's
    // roleid, must be read now, not after SaveChanges clears/reassigns
    // tracked state.
    private HashSet<long> CollectDirectlyAffectedUserIds()
    {
        var userIds = new HashSet<long>();
        var roleIds = new HashSet<long>();

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted)) continue;

            switch (entry.Entity)
            {
                case sc_user_role ur:
                    userIds.Add(ur.userid);
                    break;
                case sc_role_menu rm:
                    roleIds.Add(rm.roleid);
                    break;
                case sc_role_scope rs:
                    roleIds.Add(rs.roleid);
                    break;
            }
        }

        _pendingAffectedRoleIds = roleIds;
        return userIds;
    }

    // Expands roleIds (sc_role_menu/sc_role_scope changes) into the actual
    // members of those roles via a live query, unions with the userIds
    // already known directly (sc_user_role changes), then bumps
    // sc_user.permversion for every affected user. No-ops instantly if
    // nothing permission-related changed — this runs inside every save
    // across the whole app, so the common case must stay cheap.
    private async Task BumpAffectedPermVersionsAsync(HashSet<long> directUserIds, CancellationToken ct)
    {
        var userIds = new HashSet<long>(directUserIds);

        if (_pendingAffectedRoleIds is { Count: > 0 } roleIds)
        {
            var roleMemberIds = await sc_user_roles
                .Where(ur => roleIds.Contains(ur.roleid) && ur.isactive)
                .Select(ur => ur.userid)
                .ToListAsync(ct);
            foreach (var id in roleMemberIds) userIds.Add(id);
        }
        _pendingAffectedRoleIds = null;

        if (userIds.Count == 0) return;

        var users = await sc_users.Where(u => userIds.Contains(u.userid)).ToListAsync(ct);
        foreach (var u in users) u.permversion++;
    }
}
