using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace HRM.Models;

// Generic, system-wide Create/Update/Delete audit trail — automatic, so
// every future feature gets it for free without remembering to call
// IAuditLogger. Deliberately does NOT know who the current user is:
// HRMContext isn't request-scoped (it's created via IDbContextFactory from
// all over the app, often outside any HTTP request at all), so wiring
// IHttpContextAccessor into its constructor would be invasive and only
// half-work anyway. Actor-attributed logging comes from the explicit
// IAuditLogger calls at specific call sites instead (see Services/Audit/).
//
// This is the highest-risk file in the audit-log feature — it runs on
// every single save across the whole app. See HRMContextModelSnapshot.cs
// for reference field names; do not change the shape of AuditLog here
// without updating Model/AuditLog.cs to match.
public partial class HRMContext
{
    private record PendingAudit(EntityEntry Entry, AuditActionType Action, Dictionary<string, object?>? OldValues, Dictionary<string, object?>? NewValues);

    // Only the two acceptAllChangesOnSuccess overloads are overridden.
    // DbContext.SaveChanges()/SaveChangesAsync(CancellationToken) are NOT
    // overridden because their base implementations just forward to these
    // two via a virtual call on `this` — overriding all four caused each
    // save to run its capture-append cycle twice (verified empirically: a
    // single tracked change produced two AuditLog rows instead of one; base
    // .SaveChanges() → virtual dispatch back into this class's
    // SaveChanges(bool) override → forwards again).
    // Deliberately does NOT bump permversion (see the async override below
    // for that) — the only real caller of sync SaveChanges anywhere in this
    // app (Services/ActivityService.cs) never touches sc_user_role/
    // sc_role_menu/sc_role_scope, and permversion bumping needs an async DB
    // round-trip to expand a changed role into its members.
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        var pending = CapturePendingAudits();
        var result = base.SaveChanges(acceptAllChangesOnSuccess);
        if (pending.Count > 0)
        {
            AppendAuditRows(pending);
            base.SaveChanges(acceptAllChangesOnSuccess);
        }
        return result;
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        var pending = CapturePendingAudits();
        // Advance Security slice 2 — see HRMContext.PermVersion.cs. Must be
        // collected here too (before the real save), same reason as
        // CapturePendingAudits above.
        var directlyAffectedUserIds = CollectDirectlyAffectedUserIds();
        var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        if (pending.Count > 0)
        {
            AppendAuditRows(pending);
            await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        if (directlyAffectedUserIds.Count > 0 || _pendingAffectedRoleIds is { Count: > 0 })
        {
            await BumpAffectedPermVersionsAsync(directlyAffectedUserIds, cancellationToken);
            await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        return result;
    }

    // Captured BEFORE the real save — OriginalValues for an about-to-be
    // Deleted/Modified row are only meaningful pre-save. RecordId is
    // resolved afterward instead (see AppendAuditRows), since an Added
    // row's identity-column value doesn't exist yet at capture time.
    private List<PendingAudit> CapturePendingAudits()
    {
        var list = new List<PendingAudit>();

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is AuditLog) continue; // never audit the audit table itself
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted)) continue;

            var action = entry.State switch
            {
                EntityState.Added => AuditActionType.Create,
                EntityState.Deleted => AuditActionType.Delete,
                _ => AuditActionType.Update,
            };

            Dictionary<string, object?>? oldValues = null;
            if (action != AuditActionType.Create)
            {
                oldValues = new Dictionary<string, object?>();
                foreach (var prop in entry.OriginalValues.Properties)
                    oldValues[prop.Name] = entry.OriginalValues[prop];
            }

            Dictionary<string, object?>? newValues = null;
            if (action != AuditActionType.Delete)
            {
                newValues = new Dictionary<string, object?>();
                foreach (var prop in entry.CurrentValues.Properties)
                    newValues[prop.Name] = entry.CurrentValues[prop];
            }

            list.Add(new PendingAudit(entry, action, oldValues, newValues));
        }

        return list;
    }

    private void AppendAuditRows(List<PendingAudit> pending)
    {
        foreach (var p in pending)
        {
            AuditLogs.Add(new AuditLog
            {
                Action = p.Action,
                EntityType = p.Entry.Entity.GetType().Name,
                RecordId = ResolveRecordId(p.Entry),
                OldValuesJson = p.OldValues is null ? null : JsonSerializer.Serialize(p.OldValues),
                NewValuesJson = p.NewValues is null ? null : JsonSerializer.Serialize(p.NewValues),
                IsSensitiveDataAccess = false,
            });
        }
    }

    // Uses EF's own PK metadata rather than guessing property names — this
    // codebase's legacy tables use inconsistent PK names ("Id", "id",
    // "userid", ...), so a name-based guess (tried first, missed sc_user's
    // "userid") silently produced a null RecordId.
    private static string? ResolveRecordId(EntityEntry entry)
    {
        var keyProps = entry.Properties.Where(p => p.Metadata.IsPrimaryKey()).ToList();
        if (keyProps.Count == 0) return null;
        return string.Join(",", keyProps.Select(p => p.CurrentValue?.ToString() ?? "null"));
    }
}
