using Advance.Workflow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Advance.Workflow.Engine;

// ============================================================================
//  WorkflowDbContext — the wf_*/job_* slice of HRM's HRMContext, copied out
//  standalone. Every DbSet name and every fluent config block below is
//  copied from Model/HRMContext.cs (search for the same entity type there to
//  cross-check) — same table names, same explicit PK names
//  (e.g. "PK_JOBMASTER", "PK__wf_subwo__0D16B5CDB3240F76"), same
//  ValueGeneratedNever()/HasDefaultValueSql() quirks. This is why a Phase 1
//  cutover needs no new migration inside HRM: this context maps the exact
//  same physical tables/columns HRMContext already maps.
//
//  Two differences from HRMContext's config for these same entities, both
//  because the FK target now lives outside this project:
//    - job_user_list.user / wf_adhoc_user.user / wf_custom_user.user (all ->
//      sc_user) have no HasOne(...) here — the navigation property itself
//      was dropped when the entities were copied into Advance.Workflow.Domain
//      (see the TODO(seam) comment on each). The scalar `userid` column is
//      unchanged.
//    - wf_employee / wf_org_type configs live in
//      Advance.Workflow.Org/WorkflowOrgDbContext.cs instead (they moved to a
//      different project, not this one).
// ============================================================================

public class WorkflowDbContext : DbContext
{
    public WorkflowDbContext(DbContextOptions<WorkflowDbContext> options) : base(options)
    {
    }

    public DbSet<job_loa> job_loas => Set<job_loa>();
    public DbSet<job_master> job_masters => Set<job_master>();
    public DbSet<job_status> job_statuses => Set<job_status>();
    public DbSet<job_subworkflow_master> job_subworkflow_masters => Set<job_subworkflow_master>();
    public DbSet<job_user_list> job_user_lists => Set<job_user_list>();

    public DbSet<wf_adhoc_user> wf_adhoc_users => Set<wf_adhoc_user>();
    public DbSet<wf_budget> wf_budgets => Set<wf_budget>();
    public DbSet<wf_button> wf_buttons => Set<wf_button>();
    public DbSet<wf_button_master> wf_button_masters => Set<wf_button_master>();
    public DbSet<wf_checklist> wf_checklists => Set<wf_checklist>();
    public DbSet<wf_condition> wf_conditions => Set<wf_condition>();
    public DbSet<wf_custom_role> wf_custom_roles => Set<wf_custom_role>();
    public DbSet<wf_custom_user> wf_custom_users => Set<wf_custom_user>();
    public DbSet<wf_customer_approver> wf_customer_approvers => Set<wf_customer_approver>();
    public DbSet<wf_decision_status> wf_decision_statuses => Set<wf_decision_status>();
    public DbSet<wf_emailTemplate> wf_emailTemplates => Set<wf_emailTemplate>();
    public DbSet<wf_loa> wf_loas => Set<wf_loa>();
    public DbSet<wf_loa_user> wf_loa_users => Set<wf_loa_user>();
    public DbSet<wf_mas_reason> wf_mas_reasons => Set<wf_mas_reason>();
    public DbSet<wf_organize> wf_organizes => Set<wf_organize>();
    public DbSet<wf_role_authority> wf_role_authorities => Set<wf_role_authority>();
    public DbSet<wf_sub_workflow_master> wf_sub_workflow_masters => Set<wf_sub_workflow_master>();
    public DbSet<wf_workflow> wf_workflows => Set<wf_workflow>();
    public DbSet<wf_workflow_in_workflow> wf_workflow_in_workflows => Set<wf_workflow_in_workflow>();

    // mas_reason isn't wf_-prefixed (a naming leftover from the legacy schema)
    // but it is workflow-owned — it carries workflowid/wlevel columns and is
    // only ever read by job_user_list.mas_reason_id (approval reason codes,
    // /wf/reasons admin page). Copied into Domain alongside the wf_*/job_*
    // entities for that reason.
    public DbSet<mas_reason> mas_reasons => Set<mas_reason>();

    // Approval delegation ("มอบฉันทะ") — copied from Model/Wf_ApproverDelegation.cs.
    // Missed on the first entity-copy pass because it's PascalCase
    // (Wf_ApproverDelegation) unlike every other wf_*/job_* table, so a
    // case-sensitive `wf_*.cs` glob skips it — worth remembering for anyone
    // re-running this extraction later.
    public DbSet<Wf_ApproverDelegation> Wf_ApproverDelegations => Set<Wf_ApproverDelegation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<job_master>(entity =>
        {
            entity.HasKey(e => e.jobmasterid)
                .HasName("PK_JOBMASTER")
                .IsClustered(false);

            entity.HasOne(d => d.workflow).WithMany(p => p.job_masters)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_job_master_wf_workflow");
        });

        modelBuilder.Entity<job_status>(entity =>
        {
            entity.HasKey(e => e.jobstatusid).HasName("PK_JobStatus");
        });

        modelBuilder.Entity<job_subworkflow_master>(entity =>
        {
            entity.HasKey(e => e.jobsubworkflowid).HasName("PK_JobSubworkflowmaster");
        });

        modelBuilder.Entity<job_user_list>(entity =>
        {
            entity.HasKey(e => e.jobapproverid).HasName("PK_JobApprover");

            entity.HasOne(d => d.jobmaster).WithMany(p => p.job_user_lists)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_job_user_list_job_master");

            entity.HasOne(d => d.mas_reason).WithMany(p => p.job_user_lists).HasConstraintName("FK_job_user_list_mas_reason1");

            entity.HasOne(d => d.subworkflowmaster).WithMany(p => p.job_user_lists).HasConstraintName("FK_job_user_list_wf_sub_workflow_master");

            // TODO(seam): HRMContext also declares
            // entity.HasOne(d => d.user).WithMany(...).HasConstraintName("FK_job_user_list_sc_user")
            // here — dropped along with the `user` navigation property (see
            // job_user_list.cs). The physical FK constraint still exists on
            // the real table; only the EF-side navigation is gone.
        });

        modelBuilder.Entity<wf_adhoc_user>(entity =>
        {
            entity.HasOne(d => d.jobmaster).WithMany(p => p.wf_adhoc_users).HasConstraintName("FK_wf_adhoc_user_job_master");

            // TODO(seam): FK_wf_adhoc_user_sc_user dropped along with the `user` navigation.
        });

        modelBuilder.Entity<wf_budget>(entity =>
        {
            entity.HasKey(e => e.wfcondiionid).HasName("PK_WF_CONDITION");

            entity.HasOne(d => d.subworkflow).WithMany(p => p.wf_budgets).HasConstraintName("FK_WF_CONDI_REFERENCE_WF_SUBWO");
        });

        modelBuilder.Entity<wf_button>(entity =>
        {
            entity.HasKey(e => e.wfbuttonid).HasName("PK_WF_Button");

            entity.HasOne(d => d.button_master).WithMany(p => p.wf_buttons)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_wf_button_wf_button_master");
        });

        modelBuilder.Entity<wf_checklist>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK_WF_CheckList");

            entity.Property(e => e.id).ValueGeneratedNever();
        });

        modelBuilder.Entity<wf_condition>(entity =>
        {
            entity.HasKey(e => e.subworkflowid).HasName("PK_WF_Condition_1");

            entity.Property(e => e.subworkflowid).ValueGeneratedNever();
            entity.Property(e => e.wfcondiionid).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<wf_custom_role>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK_WF_CUSTOMROLE");

            entity.Property(e => e.enddate).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.isactive).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.modate).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.modby).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.startdate).HasDefaultValueSql("(NULL)");

            entity.HasOne(d => d.subworkflow).WithMany(p => p.wf_custom_roles).HasConstraintName("FK_WF_CUSTO_REFERENCE_WF_SUBWO");
        });

        modelBuilder.Entity<wf_custom_user>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK_WF_Customuser_1");

            entity.Property(e => e.modate).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.modby).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.userid).HasDefaultValueSql("(NULL)");

            entity.HasOne(d => d.subworkflow).WithMany(p => p.wf_custom_users)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WF_CUSTO_USR_REFERENCE_WF_SUBWO");

            // TODO(seam): FK_wf_custom_user_sc_user dropped along with the `user` navigation.
        });

        modelBuilder.Entity<wf_customer_approver>(entity =>
        {
            entity.HasKey(e => e.subworkflowid).HasName("PK_WF_CustomApprover");
        });

        modelBuilder.Entity<wf_decision_status>(entity =>
        {
            entity.HasKey(e => e.workflowstatusid).HasName("PK_WorkflowDecisionStatus");

            entity.HasOne(d => d.subworkflow).WithMany(p => p.wf_decision_statuses).HasConstraintName("FK_WF_DecisionStatus_ToSWF");
        });

        modelBuilder.Entity<wf_loa>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK_WF_LOA");
        });

        modelBuilder.Entity<wf_loa_user>(entity =>
        {
            entity.HasKey(e => e.id);
            entity.Property(e => e.id).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<wf_organize>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK_WF_Organize");

            entity.Property(e => e.istop).IsFixedLength();
        });

        modelBuilder.Entity<wf_sub_workflow_master>(entity =>
        {
            entity.HasKey(e => e.subworkflowid).HasName("PK__wf_subwo__0D16B5CDB3240F76");

            entity.Property(e => e.andpercent).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.status).HasDefaultValueSql("(NULL)");
        });

        modelBuilder.Entity<wf_workflow>(entity =>
        {
            entity.HasKey(e => e.workflowid).HasName("PK__wf_workf__D00EA011B75E9C00");

            entity.Property(e => e.c_create).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.c_detail).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.c_edit).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.c_list).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.code).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.columnref).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.isactive).HasDefaultValueSql("(NULL)");
        });
    }
}
