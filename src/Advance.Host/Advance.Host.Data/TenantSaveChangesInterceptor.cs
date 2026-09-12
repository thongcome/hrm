using Advance.Host.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Advance.Host.Data;

/// <summary>
/// Auto-stamps TenantId on every newly-Added <see cref="ITenantScoped"/>
/// entity, the write-side counterpart to the read-side global query filter
/// documented on <see cref="HostDbContext"/>. Mirrors the pattern HRM already
/// uses for auditing (Model/HRMContext.Audit.cs overrides SaveChangesAsync to
/// log every write with no call-site changes) — a consuming product's
/// DbContext registers this once via
/// <c>optionsBuilder.AddInterceptors(new TenantSaveChangesInterceptor(currentTenant))</c>
/// and no service/page anywhere has to remember to set TenantId itself.
///
/// Deliberately does NOT touch Modified/Deleted entries — TenantId is
/// immutable once a row is created (moving a row between tenants is not a
/// supported operation; it would be a data-migration script, not a save).
/// </summary>
public class TenantSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentTenant _currentTenant;

    public TenantSaveChangesInterceptor(ICurrentTenant currentTenant)
    {
        _currentTenant = currentTenant;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        StampTenantId(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        StampTenantId(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void StampTenantId(DbContext? context)
    {
        if (context is null)
            return;

        var tenantId = _currentTenant.TenantId;
        if (string.IsNullOrEmpty(tenantId))
            return; // fail-open on purpose here: a host still bootstrapping (e.g. seeding the first Tenant row itself) has no ambient tenant yet

        foreach (EntityEntry<ITenantScoped> entry in context.ChangeTracker.Entries<ITenantScoped>())
        {
            if (entry.State == EntityState.Added && string.IsNullOrEmpty(entry.Entity.TenantId))
                entry.Entity.TenantId = tenantId;
        }
    }
}
