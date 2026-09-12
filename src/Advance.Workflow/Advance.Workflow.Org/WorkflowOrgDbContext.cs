using Advance.Workflow.Org.Entities;
using Microsoft.EntityFrameworkCore;

namespace Advance.Workflow.Org;

// ============================================================================
//  WorkflowOrgDbContext — stub DbContext for Workflow.Org (see README.md in
//  this folder for what these two tables are and why they already exist).
//
//  Phase 0 status: this context is NOT wired into anything yet. It exists so
//  the project compiles standalone and to prove the table mapping is
//  self-consistent. Two real integration paths, neither built yet:
//
//    1. Embedded in HumanOk: HRM's own HRMContext already has DbSet<wf_employee>
//       / DbSet<wf_org_type> (Model/HRMContext.cs lines ~331, ~341) pointed at
//       the SAME physical tables this context maps. Do NOT run this context's
//       migrations against an HRM database — the tables already exist. HRM
//       would keep these rows in sync by implementing
//       Advance.Workflow.Contracts.IOrgDirectorySource and calling into
//       whatever sync job populates wf_employee/wf_org_type from
//       Hremployee/com_organization (not yet built — see EXTRACTION-PLAN.md).
//
//    2. Standalone Advance.Workflow deployment: this context (or its own
//       migrations, once written) creates wf_employee/wf_org_type fresh in a
//       brand-new database, and a customer's admin pages (ported from
//       Components/Pages/Wf/WfEmployeeAdmin.razor / WfOrgTypeAdmin.razor —
//       see Advance.Workflow.Blazor) become the actual source of truth.
// ============================================================================

public class WorkflowOrgDbContext : DbContext
{
    public WorkflowOrgDbContext(DbContextOptions<WorkflowOrgDbContext> options) : base(options)
    {
    }

    public DbSet<wf_employee> wf_employees => Set<wf_employee>();

    public DbSet<wf_org_type> wf_org_types => Set<wf_org_type>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Neither table has a real IDENTITY column in the live HRM database
        // (verified via sys.columns.is_identity = 0 — see CLAUDE.md's note on
        // wf_org_type/wf_employee) despite id looking like a normal PK. Both
        // entity classes already say DatabaseGeneratedOption.None on `id`
        // (copied verbatim from Model/wf_employee.cs / Model/wf_org_type.cs) —
        // callers must assign the next id themselves before insert, same as
        // HRM's WfEmployeeAdmin.razor / WfOrgTypeAdmin.razor do today via
        // Services/Shared/EntitySearchHelper.NextIdAsync. A standalone
        // deployment's equivalent helper still needs to be written (Phase 1).
        modelBuilder.Entity<wf_employee>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK_wf_employee");
        });

        modelBuilder.Entity<wf_org_type>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK_wf_OrgType");
        });
    }
}
