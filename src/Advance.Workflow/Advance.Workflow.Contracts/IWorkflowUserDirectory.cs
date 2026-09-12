namespace Advance.Workflow.Contracts;

// ============================================================================
//  IWorkflowUserDirectory — the seam for "who can log in and act as an
//  approver", as distinct from IOrgDirectorySource ("what does the org chart
//  look like"). Split into a separate interface because today they come from
//  two different HRM tables with two different lifecycles: sc_user (login
//  identity — Services/Login/ScUserClaimsPrincipalFactory.cs, ASP.NET
//  Identity) vs. Hremployee/com_organization (HR master data).
//
//  Every call site this mirrors, all in Services/Workflow/WorkflowService.cs
//  and Model/wf_sub_workflow_master.Behavior.cs today:
//    - GetUserAsync            <- db.sc_users.FirstOrDefaultAsync(u.userid == id)
//    - GetUsersAsync           <- db.sc_users.Where(ids.Contains(u.userid))
//    - GetUsersInOrgAsync      <- db.sc_users.Where(u.orgcode == ... ) (IsApproverSameOrgAsync)
//    - GetUsersInRoleAsync     <- db.sc_user_roles join db.sc_users (IsCustomRoleAsync)
//    - GetUserByEmpIdAsync     <- db.sc_users.Where(u.empid == org.approver_empid) (ResolveOrgChainAsync,
//                                 IsApproverSameCostCenterAsync)
//
//  A standalone Advance.Workflow deployment (no HumanOk) implements this
//  against Workflow.Org's own user/role tables (Advance.Workflow.Org —
//  "ผู้ใช้/บทบาทของ Workflow" from the plan's architecture diagram, still to
//  be designed); embedded in HRM, HRM implements it directly against sc_user/
//  sc_user_roles — no data copy needed since HRM's copy IS the login system.
// ============================================================================

public interface IWorkflowUserDirectory
{
    Task<WorkflowUser?> GetUserAsync(long userId, CancellationToken ct = default);

    Task<IReadOnlyList<WorkflowUser>> GetUsersAsync(IEnumerable<long> userIds, CancellationToken ct = default);

    /// <summary>The (disabled-filtered) user holding this employee code — used to turn an
    /// org chart's approver_empid into a userid who can actually log in and act.</summary>
    Task<WorkflowUser?> GetUserByEmpIdAsync(string empId, CancellationToken ct = default);

    /// <summary>Every enabled user whose profile orgcode matches — isApproverSameOrgAsync.</summary>
    Task<IReadOnlyList<WorkflowUser>> GetUsersInOrgAsync(string orgCode, CancellationToken ct = default);

    /// <summary>Every enabled user holding any of the given role ids — IsCustomRoleAsync.</summary>
    Task<IReadOnlyList<long>> GetUserIdsInRolesAsync(IEnumerable<long> roleIds, CancellationToken ct = default);
}

// Mirrors the sc_user columns actually read: userid (key), empid, firstname/
// lastname (concatenated to username on job_user_list rows — audit M3 snapshot),
// orgcode, isdisable.
public sealed record WorkflowUser(
    long UserId,
    string? EmpId,
    string FullName,
    string? OrgCode,
    bool IsDisabled);
