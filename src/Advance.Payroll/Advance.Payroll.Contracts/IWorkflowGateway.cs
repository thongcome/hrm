namespace Advance.Payroll.Contracts;

// Replaces the direct call from Services/Pay/ProvidentFundExitCaseService.cs and
// ProvidentFundRateChangeRequestService.cs into HRM's workflow engine
// (Services/Workflow/WorkflowEngineService.cs's StartJobAsync(workflowId, reftable, refid,
// requesterUserId, requesterEmpId, subject, amount, ct) — see CLAUDE.md's workflow
// paragraph) for the provident-fund-exit-case / rate-change-request approval trigger.
// This is the ONE place Advance.Payroll needs Advance.Workflow (plan section 2.1's
// wf_workflow/job_master row) — everything else in Payroll is workflow-free.
//
// Advance.Payroll Lite (plan section 5, "ไม่มี": "workflow ปรับแต่งได้") ships a fixed
// two-step chain (ผู้คำนวณ -> ผู้อนุมัติ) — its IWorkflowGateway implementation can be a
// thin one-approver stub rather than a real engine, since Lite doesn't sell configurable
// workflow at all.
public interface IWorkflowGateway
{
    Task<long> StartApprovalAsync(
        string workflowCode, string refTable, long refId,
        long requesterUserId, long requesterEmpId, string subject, decimal? amount,
        CancellationToken ct = default);

    Task<WorkflowJobStatus> GetStatusAsync(string refTable, long refId, CancellationToken ct = default);
}

public enum WorkflowJobStatus
{
    NotStarted = 0,
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Cancelled = 4,
}
