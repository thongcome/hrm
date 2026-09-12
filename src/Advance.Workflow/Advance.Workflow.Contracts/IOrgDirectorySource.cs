namespace Advance.Workflow.Contracts;

// ============================================================================
//  IOrgDirectorySource — the seam between "wherever people/org data really
//  lives" and the workflow engine's approver resolution.
//
//  Today (inside HRM) WorkflowService / wf_sub_workflow_master.Behavior.cs
//  read com_organizations and Hremployee directly (see
//  ResolveOrgChainAsync, IsApproverSameCostCenterAsync, IsApproverSameOrgAsync
//  in Model/wf_sub_workflow_master.Behavior.cs, and GetApproverPlanAsync /
//  NotifyRecipientsAsync in Services/Workflow/WorkflowService.cs). Both of
//  those tables belong to HRM's own HR module, not to Workflow.
//
//  Per the split plan (docs/Plan_Split_Payroll_Workflow_v1.1.md, section 3):
//  Advance.Workflow gets its own people/org-chart component, Workflow.Org
//  (Advance.Workflow.Org project — reviving the dormant wf_org_type/wf_employee
//  tables). Sold standalone, Workflow.Org is the source of truth and a
//  customer maintains it directly (its own admin pages, or Excel import).
//  Embedded in HumanOk, HRM keeps owning com_organization/Hremployee as the
//  real source and must push one-way updates into Workflow.Org — this
//  interface is exactly that push contract. HRM implements it (reading its
//  own com_organization/Hremployee) and Workflow.Org's sync job calls it
//  whenever the org chart or employee roster changes.
//
//  The workflow engine's own approver resolution never calls this interface
//  directly at request time — see the seam TODOs in
//  Advance.Workflow.Engine/WorkflowOrgChainResolver.cs, which reads
//  Workflow.Org's own already-synced tables instead. This interface is the
//  ONE-WAY feed that keeps those tables current.
// ============================================================================

public interface IOrgDirectorySource
{
    /// <summary>Full org-chart snapshot, for an initial or scheduled full sync into Workflow.Org.</summary>
    Task<IReadOnlyList<OrgNode>> GetOrganizationTreeAsync(CancellationToken ct = default);

    /// <summary>Full employee roster snapshot, for an initial or scheduled full sync into Workflow.Org.</summary>
    Task<IReadOnlyList<WorkflowEmployee>> GetEmployeesAsync(CancellationToken ct = default);

    /// <summary>One org unit, for an incremental sync when a single unit changes.</summary>
    Task<OrgNode?> GetOrganizationAsync(string orgCode, CancellationToken ct = default);

    /// <summary>One employee, for an incremental sync when a single record changes.</summary>
    Task<WorkflowEmployee?> GetEmployeeAsync(string empId, CancellationToken ct = default);
}

// Mirrors the columns wf_sub_workflow_master.Behavior.cs's ResolveOrgChainAsync
// and IsApproverSameCostCenterAsync actually read off com_organization today:
// code / parent_code (the tree), approver_empid (who signs for this unit),
// CostCenterCode (for isApproverSameCostCenter), isActive.
public sealed record OrgNode(
    string Code,
    string? ParentCode,
    string? ApproverEmpId,
    string? CostCenterCode,
    bool IsActive,
    string? Name = null);

// Mirrors the Hremployee columns actually read in WorkflowService/Behavior:
// EmpNo (key), EmpName+EmpSurname (display name), orgcode (isApproverSameOrg),
// plus an email for notifications (today resolved via EmployeeEmailResolver).
public sealed record WorkflowEmployee(
    string EmpId,
    string FullName,
    string? OrgCode,
    string? Email,
    bool IsActive);
