using HRM.Services.Security;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

// Advance Security slice 1 (sc_role_scope): data-scope enforcement,
// anchored on Hremployee — the entity most sensitive HRM data ultimately
// joins back to. Filters on descendant navigations/Includes too (EF query
// filters apply at the entity-type level, not just the top-level DbSet).
//
// CurrentScope defaults to Unrestricted for any context created via the
// plain IDbContextFactory<HRMContext>.CreateDbContextAsync() every existing
// page already calls — meaning it behaves identically to today's behavior
// until a page is deliberately switched to
// ScopedDbContextExtensions.CreateScopedDbContextAsync (see that file).
// That's a one-line opt-in per page, rolled out gradually — not automatic
// for the ~150 pages that existed before this slice. See that file's
// header comment for exactly why "automatic for every page, zero changes"
// turned out not to be achievable given how IDbContextFactory<T> and
// Blazor Server's per-circuit dispatch actually work.
public partial class HRMContext
{
    public DbSet<sc_role_scope> sc_role_scopes { get; set; } = null!;

    // Deliberately non-nullable, defaulting to Unrestricted. A query filter
    // lambda's captured member accesses (CurrentScope.IsUnrestricted etc.)
    // get evaluated as closures during query translation regardless of
    // where they sit relative to a "CurrentScope == null" check earlier in
    // the same C# ||-expression — EF Core's query filter mechanism doesn't
    // short-circuit that the way plain C# would, so a nullable CurrentScope
    // throws NullReferenceException on the very first query against
    // Hremployee for EVERY context, scoped or not (found by running the
    // diagnostic block in Program.cs against the real DB before committing
    // — see DIAG_ROLE_SCOPE_CHECK). A non-null default sidesteps the whole
    // problem instead of trying to out-clever EF's evaluation order.
    public RoleScopeSnapshot CurrentScope { get; set; } = RoleScopeSnapshot.Unrestricted;

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<sc_role_scope>(entity =>
        {
            entity.HasKey(e => e.scopeid);
            entity.HasOne(e => e.role).WithMany(r => r.sc_role_scopes)
                .HasForeignKey(e => e.roleid).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Hremployee>().HasQueryFilter(e =>
            CurrentScope.IsUnrestricted ||
            CurrentScope.CompanyIds.Contains(e.companyid) ||
            (e.orgcodefull != null && CurrentScope.OrgExactCodes.Contains(e.orgcodefull)) ||
            (e.orgcodefull != null && CurrentScope.OrgLikePatterns.Any(p => EF.Functions.Like(e.orgcodefull, p))) ||
            (e.CostCenterCode != null && CurrentScope.CostCenterCodes.Contains(e.CostCenterCode)));
    }
}
