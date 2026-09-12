using Advance.Host.Domain;
using Microsoft.EntityFrameworkCore;

namespace Advance.Host.Data;

/// <summary>
/// The Host shell's own tiny DbContext — owns only the Tenant table. A
/// standalone Advance.Payroll.Web or Advance.Workflow.Web app does NOT put
/// its own business entities (Pay_*, wf_*, ...) in here: this context is
/// deliberately separate from PayrollDbContext/WorkflowDbContext so the host
/// shell can be versioned/migrated independently of either product's schema
/// (same "own migration per module" convention HRM's CLAUDE.md already
/// documents for Pay_*/Wf_*/etc.).
///
/// HOW A CONSUMING DBCONTEXT APPLIES THE SAME TENANT FILTER
/// ----------------------------------------------------------------------
/// PayrollDbContext (or WorkflowDbContext) marks its own entities with
/// <see cref="ITenantScoped"/> and, in its own OnModelCreating, does the
/// equivalent of what this class does below for Tenant itself:
///
///   foreach (var entityType in modelBuilder.Model.GetEntityTypes())
///   {
///       if (!typeof(ITenantScoped).IsAssignableFrom(entityType.ClrType))
///           continue;
///
///       var parameter = Expression.Parameter(entityType.ClrType, "e");
///       var tenantIdProperty = Expression.Property(parameter, nameof(ITenantScoped.TenantId));
///       var currentTenantIdCall = Expression.Property(
///           Expression.Constant(_currentTenant), nameof(ICurrentTenant.TenantId));
///       var comparison = Expression.Equal(tenantIdProperty, currentTenantIdCall);
///       var lambda = Expression.Lambda(comparison, parameter);
///
///       modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
///   }
///
/// (EF Core's HasQueryFilter only accepts a compile-time generic lambda per
/// entity type — the reflection/Expression-tree dance above is the standard
/// way to apply the "same" filter across every entity implementing a marker
/// interface without hand-writing `HasQueryFilter&lt;T&gt;` once per table.)
/// _currentTenant is an injected <see cref="ICurrentTenant"/>, captured in the
/// DbContext constructor exactly like this class captures it below. The
/// matching write-side half (auto-stamping TenantId on Added entries, so
/// callers never have to set it themselves) is <see cref="TenantSaveChangesInterceptor"/>
/// — register it the same way on the consuming DbContext's
/// <c>optionsBuilder.AddInterceptors(...)</c>.
/// </summary>
public class HostDbContext : DbContext
{
    private readonly ICurrentTenant? _currentTenant;

    public HostDbContext(DbContextOptions<HostDbContext> options)
        : base(options)
    {
    }

    public HostDbContext(DbContextOptions<HostDbContext> options, ICurrentTenant currentTenant)
        : base(options)
    {
        _currentTenant = currentTenant;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Tenant>(e =>
        {
            e.ToTable("host_tenant");
            e.HasKey(t => t.Id);
            e.Property(t => t.Id).HasMaxLength(50);
            // Generous sizing per HRM's own standing rule (CLAUDE.md,
            // "String columns are sized generously, not tightly") — identifier
            // and name columns get 50/200 minimum rather than a guessed-tight
            // length.
            e.Property(t => t.Code).HasMaxLength(50).IsRequired();
            e.Property(t => t.Name).HasMaxLength(200).IsRequired();
            e.HasIndex(t => t.Code).IsUnique();
        });
    }
}
