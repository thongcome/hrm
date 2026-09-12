using Advance.Workflow.Domain.Entities;

namespace Advance.Workflow.Contracts;

// ============================================================================
//  IWorkflowEngine — the public surface HRM.Services.Workflow.WorkflowEngineService
//  already exposes to its ~21 calling modules today (Services/Workflow/
//  WorkflowEngineService.cs, ~820 lines after the old-engine retirement on
//  2026-09-12). Every member below is copied signature-for-signature from
//  that class's public methods — this interface changes NOTHING about the
//  calling convention modules already use, it only gives them something to
//  depend on that isn't a concrete HRM class.
//
//  Not included (deliberately, per the extraction plan's call-site survey):
//    - EvaluateLevel (public static, pure — a testable decision function, not
//      part of the runtime engine contract; stays internal to Engine)
//    - IsLiveApprovalRow (an EF expression tree bound to job_user_list; a
//      LINQ predicate isn't a meaningful interface member. Any caller that
//      needs it (WorkflowNotice.razor, WfHealthCheck.razor) either goes
//      through GetMyInboxAsync/GetVacantApprovalsAsync or stays HRM-internal.)
//    - GetApproverPlanAsync's underlying wf_custom_user/wf_custom_role reads
//      (exposed here as the one aggregate method callers actually use)
// ============================================================================

public interface IWorkflowEngine
{
    // ── ร่าง (draft) ──────────────────────────────────────────────────────
    Task<long> CreateDraftAsync(string workflowCode, string reftable, string refid,
        long requesterUserId, string? requesterEmpId, string? subject, decimal? amount, CancellationToken ct = default);

    Task DeleteDraftAsync(long jobMasterId, long actorUserId, CancellationToken ct = default);

    // ── เริ่ม / กระทำ / ยกเลิก ──────────────────────────────────────────────
    Task<long> StartJobAsync(long workflowId, string reftable, string refid,
        long requesterUserId, string? requesterEmpId, string? subject, decimal? amount, CancellationToken ct = default);

    Task ActAsync(long jobMasterId, long jobApproverId, long actorUserId, string actionKind,
        string? comment, long? reasonId = null, CancellationToken ct = default);

    Task CancelAsync(long jobMasterId, long actorUserId, bool isAdminOverride, string? reason, CancellationToken ct = default);

    // ── admin: มอบหมาย / เปลี่ยนผู้อนุมัติ ─────────────────────────────────
    Task AssignApproverAsync(long jobApproverId, long assigneeUserId, string? note, CancellationToken ct = default);

    Task ReassignApproverAsync(long jobApproverId, long newUserId, string reason, CancellationToken ct = default);

    // ── กล่องงาน / ประวัติ / ภาพรวม ─────────────────────────────────────────
    Task<List<job_user_list>> GetMyInboxAsync(long userId, CancellationToken ct = default);

    Task<List<job_user_list>> GetMyInvolvementAsync(long userId, CancellationToken ct = default);

    Task<List<job_user_list>> GetVacantApprovalsAsync(CancellationToken ct = default);

    Task<List<job_master>> GetMyRequestsAsync(long requesterUserId, CancellationToken ct = default);

    Task<List<job_master>> SearchJobsAsync(string? workflowCode = null, bool? isClosed = null,
        long? requesterUserId = null, DateTime? fromDate = null, DateTime? toDate = null, string? searchText = null,
        IEnumerable<long>? requesterUserIds = null, CancellationToken ct = default);

    Task<job_master?> GetJobDetailAsync(long jobMasterId, CancellationToken ct = default);

    // ── Pool workflow ──────────────────────────────────────────────────────
    Task<List<PoolInboxRow>> GetMyPoolInboxAsync(long userId, CancellationToken ct = default);

    Task ClaimPoolJobAsync(long jobMasterId, long actorUserId, CancellationToken ct = default);

    Task ReleasePoolClaimAsync(long jobMasterId, long actorUserId, CancellationToken ct = default);

    // ── อายุงาน / ผู้อนุมัติปัจจุบัน / แผนผู้อนุมัติทั้งเส้นทาง ────────────────
    Task<Dictionary<long, JobAgeInfo>> GetJobAgesAsync(IEnumerable<job_master> jobs, CancellationToken ct = default);

    Task<List<LevelApproverPlan>> GetApproverPlanAsync(long workflowId, CancellationToken ct = default);

    Task<Dictionary<long, string>> GetPendingApproverNamesAsync(IEnumerable<long> jobMasterIds, CancellationToken ct = default);

    Task<string> GetPendingApproverNamesAsync(long jobMasterId, CancellationToken ct = default);
}

// ── DTOs on the interface — copied from WorkflowEngineService's own nested records ──

public record JobAgeInfo(int DaysWaiting, int? ExpireDays, bool IsOverdue);

public record PoolInboxRow(job_user_list Row, bool IsClaimedByMe, string? ClaimedByName, DateTime? ClaimedDate);

public record LevelApproverPlan(int Level, string? Label, string ApproverText);
