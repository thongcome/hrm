namespace HRM.Services.Workflow;

using HRM.Models;
using HRM.Services;
using HRM.Services.Shared;
using Microsoft.EntityFrameworkCore;

// Blocks 2-6 + 9 of the Workflow Approval Engine: sequential level
// advancement, approve/reject, full approver resolution (Horizontal +
// Vertical + vacancy handling), LOA amount-based branching, AND-condition %
// partial approval, OR-condition (any-one-approval), Mix Approval (vertical
// pre-check hops before a level's own approver), reject bounce-back to an
// earlier level (backwardlevel, round-scoped via jobseq), admin-configured
// Moving Status text, and a best-effort email notification to the requester
// when a job closes. Phase 2 Block 2: Vertical resolution and the Mix
// Approval hop-walker now share one org-chain-walking helper
// (ResolveOrgChainApproverAsync) and both anchor on Hremployee.orgcode
// (real, synced data) instead of the wf_employee pilot table. Phase 2 Block
// 3: ResolveCandidatesAsync unions every non-LOA strategy ticked on a level
// (previously only the first matching flag in an if/else-if chain ever
// ran) — verified zero-behavior-change via a live-DB query showing no
// existing level combines more than one strategy flag. Phase 2 Block 4:
// three epms-inspired strategies added to that same union —
// isReturnSender (routes back to job.createuserid directly, not epms's
// wlevel-2 offset), isApproverSameCostCenter, isAdhocUser (per-job override
// via wf_adhoc_user, CRUD already built in Phase 1). Still deliberately
// excludes:
//   - Cross-workflow LOA jumps (wf_loa.nextWorkflowId != nowWorkflowid) —
//     see the comment on ResolveNextLevelViaLoaAsync
//
// Tables used are NOT new — job_master/job_user_list/job_subworkflow_master
// already existed fully scaffolded in this DB (0 rows, unused by any app
// code) before this work started; see the plan file's "แก้ไขสำคัญ" note
// under Block 1 for how that was discovered.
public class WorkflowEngineService
{
    private readonly IDbContextFactory<HRMContext> _dbFactory;

    public const string StatusPending = "PENDING";
    public const string StatusApproved = "APPROVED";
    public const string StatusRejected = "REJECTED";
    public const string StatusCompleted = "COMPLETED";
    public const string StatusCancelled = "CANCELLED";
    // "ส่งกลับแก้ไข" — a non-terminal return to an earlier level for rework.
    // Distinct from StatusRejected (a terminal Decline) so the history shows
    // which of the two negative actions happened (owner's two-action model).
    public const string StatusReturned = "RETURNED";

    // Block 6 (Mix Approval): job_user_list.reason rows for a vertical
    // pre-check hop always start with this marker, so hop-completion count
    // and "which round just resolved" detection can be done by string match
    // instead of a new column — jobseq was deliberately NOT reused for this
    // (see epms WorkflowController.cs: job.jobseq filters job_user_list by
    // job-wide resubmission round, a different concept entirely — reusing
    // it here would collide with that semantic if resubmission is ever
    // implemented later).
    private const string VerticalPrecheckMarker = "VERTICAL_PRECHECK";

    // public only so the pure AND/OR/unanimous level decision below can be
    // unit-tested directly (WorkflowEvaluateLevelTests) — same "expose the
    // pure decision for testing" convention as PayrollCalculationService
    // .FoldPriorEmployerIncome / PayrollWorkflowService.GetAllowedActions.
    public enum LevelOutcome { StillPending, Complete, Failed }

    // Propagated up through the recursive TryAdvanceLevelAsync <->
    // AssignLevelApproversAsync chain (which can be several auto-skip hops
    // deep) so the three outermost entry points (StartJobAsync/ApproveAsync/
    // RejectAsync) know exactly when a job truly closed and fire the
    // requester notification exactly once, regardless of how many
    // vacant+auto-approve levels chained through in between.
    private enum WorkflowOutcome { StillOpen, Approved, Rejected, BouncedBack }

    private readonly EmailSender _emailSender;
    private readonly HRM.Services.Audit.IAuditLogger _auditLogger;

    public WorkflowEngineService(IDbContextFactory<HRMContext> dbFactory, EmailSender emailSender, HRM.Services.Audit.IAuditLogger auditLogger)
    {
        _dbFactory = dbFactory;
        _emailSender = emailSender;
        _auditLogger = auditLogger;
    }

    // Starts a new approval instance for any document type — reftable/refid
    // is the generic routing pair (Block 7 uses this to build the link back
    // to the originating record).
    public async Task<long> StartJobAsync(long workflowId, string reftable, string refid,
        long requesterUserId, string? requesterEmpId, string? subject, decimal? amount, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var workflow = await context.wf_workflows.FirstOrDefaultAsync(w => w.workflowid == workflowId, ct)
            ?? throw new InvalidOperationException($"ไม่พบ workflow id {workflowId}");
        if (workflow.isactive != true)
            throw new InvalidOperationException($"workflow '{workflow.wname}' ปิดใช้งานอยู่ ไม่สามารถเริ่มงานใหม่ได้");

        var levels = await context.wf_sub_workflow_masters
            .Where(s => s.workflowid == workflowId)
            .OrderBy(s => s.wlevel)
            .ToListAsync(ct);
        if (levels.Count == 0)
            throw new InvalidOperationException($"workflow '{workflow.wname}' ยังไม่ได้กำหนดระดับการอนุมัติเลย (wf_sub_workflow_master ว่างเปล่า)");

        // Anchor org for Vertical resolution — Block 2: resolved from
        // Hremployee.orgcode (the real, synced anchor — see
        // Services/Shared/EmployeePositionSync.cs and Hremployee.cs's own
        // doc comment), not the wf_employee pilot table used before this
        // block. Hremployee.DEPTGRP_CODE (the OLD field that never matched
        // com_organization.code) is a separate, still-dead legacy column —
        // orgcode is the fixed replacement, kept in sync automatically.
        // costcenter is likewise snapshotted here for the first time (was
        // never set at all before this block) so Block 4's future
        // isApproverSameCostCenter strategy has something real to compare
        // against.
        string? requesterOrgCode = null;
        string? requesterCostCenter = null;
        if (!string.IsNullOrWhiteSpace(requesterEmpId))
        {
            var requesterEmp = await context.Hremployee
                .Where(e => e.EmpNo == requesterEmpId)
                .Select(e => new { e.orgcode, e.CostCenterCode })
                .FirstOrDefaultAsync(ct);
            requesterOrgCode = requesterEmp?.orgcode;
            requesterCostCenter = requesterEmp?.CostCenterCode;
        }

        var job = new job_master
        {
            workflowid = workflowId,
            workflowcode = workflow.workflowcode,
            wname = workflow.wname,
            subject = subject,
            maxlevel = levels.Count,
            lastLevel = levels[0].wlevel,
            // Block 9: display status is the level's configured "pending at
            // this level" text, not a hardcoded engine constant — kept as a
            // fallback only for levels that somehow have no standstatus set.
            status = levels[0].standstatus ?? StatusPending,
            reftable = reftable,
            refid = refid,
            createuserid = requesterUserId,
            empid = requesterEmpId,
            reqOrg = requesterOrgCode,
            createdate = DateTime.Now,
            reqdate = DateTime.Now,
            reqamont = amount,
            costcenter = requesterCostCenter,
            isactive = true,
            isJobClosed = false,
        };
        context.job_masters.Add(job);
        await context.SaveChangesAsync(ct); // need job.jobmasterid before snapshotting levels

        // Block 1: actor-attributed audit trail — HRMContext.Audit.cs's
        // auto-hook deliberately doesn't know who's calling (see its own
        // comment), so every mutation this engine makes gets an explicit
        // LogChangeAsync call instead. Additive only — never changes what
        // the engine decides.
        await _auditLogger.LogChangeAsync(AuditActionType.Create, "job_master", job.jobmasterid.ToString(),
            null, new { job.workflowcode, job.subject, job.reqOrg, job.reqamont, job.status }, isSensitive: false, ct);
        Serilog.Log.Information("Workflow job {JobMasterId} started: workflow {WorkflowCode}, requester {RequesterEmpId}, first level {Level}",
            job.jobmasterid, workflow.workflowcode, requesterEmpId, levels[0].wlevel);

        foreach (var level in levels)
        {
            context.job_subworkflow_masters.Add(new job_subworkflow_master
            {
                jobmasterid = job.jobmasterid,
                workflowid = level.workflowid,
                wlevel = level.wlevel,
                isupperrole = level.isupperrole,
                isupperuser = level.isupperuser,
                iscondition = level.iscondition,
                isorcondition = level.isorcondition,
                isandcondition = level.isandcondition,
                andpercent = level.andpercent,
                status = level.standstatus,
                forwardstatus = level.forwardstatus,
                backwardstatus = level.backwardstatus,
                istop = level.istop,
                iscustomUser = level.iscustomUser,
                iscustomRole = level.iscustomRole,
                empLevel = level.empLevel,
                isshow = level.isshow,
                isLOA = level.isLOA,
                isNeedsupervisorapprove = level.isNeedsupervisorapprove,
                backwardlevel = level.backwardlevel,
                moddate = DateTime.Now,
            });
        }
        await context.SaveChangesAsync(ct);

        // Auto-approve (opt-in per workflow): close the job as COMPLETED right
        // away with no approver row assigned. Nothing routes to a human inbox,
        // and the caller's lazy SyncStatusFromJobAsync sees a closed+completed
        // job on the next read and applies the outcome as if approved. Only
        // affects workflows explicitly flagged isautoapprove — every other
        // workflow still runs its normal level routing below.
        if (workflow.isautoapprove == true)
        {
            job.status = StatusCompleted;
            job.isJobClosed = true;
            job.reasonClosed = "อนุมัติอัตโนมัติ (auto-approve)";
            await context.SaveChangesAsync(ct);
            await _auditLogger.LogChangeAsync(AuditActionType.Update, "job_master", job.jobmasterid.ToString(),
                new { status = levels[0].standstatus ?? StatusPending }, new { status = StatusCompleted, reason = "auto-approve" }, isSensitive: false, ct);
            await NotifyRequesterAsync(context, job, WorkflowOutcome.Approved, ct);
            Serilog.Log.Information("Workflow job {JobMasterId} auto-approved (workflow {WorkflowCode} flagged isautoapprove).", job.jobmasterid, workflow.workflowcode);
            return job.jobmasterid;
        }

        var startOutcome = await AssignLevelApproversAsync(context, job, levels[0], ct);
        await context.SaveChangesAsync(ct);

        // Covers the vacancy-auto-skip-chain case: a workflow whose level 1
        // is itself vacant+auto-approve (and chains straight through to
        // istop) can close before this method even returns, with no human
        // ever having clicked anything — the requester still needs to know.
        if (startOutcome != WorkflowOutcome.StillOpen)
            await NotifyRequesterAsync(context, job, startOutcome, ct);

        return job.jobmasterid;
    }

    public async Task ApproveAsync(long jobApproverId, long actorUserId, string? comment, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var approverRow = await context.job_user_lists.FirstOrDefaultAsync(a => a.jobapproverid == jobApproverId, ct)
            ?? throw new InvalidOperationException($"ไม่พบรายการอนุมัติ id {jobApproverId}");
        if (!string.Equals(approverRow.jobstatus, StatusPending, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("รายการนี้ถูกดำเนินการไปแล้ว ไม่สามารถอนุมัติซ้ำได้");
        if (approverRow.userid != actorUserId)
            throw new InvalidOperationException("คุณไม่ใช่ผู้ได้รับมอบหมายให้อนุมัติรายการนี้");

        var job = await context.job_masters.FirstOrDefaultAsync(j => j.jobmasterid == approverRow.jobmasterid, ct)
            ?? throw new InvalidOperationException("ไม่พบ job_master ของรายการนี้");
        if (job.isJobClosed == true)
            throw new InvalidOperationException("งานนี้ปิดแล้ว ไม่สามารถดำเนินการต่อได้");
        // Guards against acting on a stale row after the job has already
        // moved past this level (e.g. an AND-condition level that resolved
        // via threshold while other rows were still Pending, or a level
        // whose earlier round already advanced — Block 5/6 both make this
        // reachable in ways Block 2-4 never could).
        // Block: reject bounce-back can revisit the same wlevel in a later
        // round (jobseq), so wlevel alone no longer uniquely identifies "is
        // this row still the open round" — must also match the current
        // jobseq. Null-coalesced to 0 on both sides so jobs that never
        // bounce (jobseq always null on both job and every row) are
        // unaffected: null==null coalesces to 0==0, always true, same as
        // before this guard existed.
        if (approverRow.wlevel != job.lastLevel || (approverRow.jobseq ?? 0) != (job.jobseq ?? 0))
            throw new InvalidOperationException("งานนี้เลื่อนผ่านระดับนี้ไปแล้ว ไม่สามารถดำเนินการกับรายการเก่านี้ได้");

        approverRow.jobstatus = StatusApproved;
        approverRow.approvedate = DateTime.Now;
        approverRow.comment = comment;
        await context.SaveChangesAsync(ct);

        await _auditLogger.LogChangeAsync(AuditActionType.Update, "job_user_list", jobApproverId.ToString(),
            new { jobstatus = StatusPending }, new { jobstatus = StatusApproved, comment }, isSensitive: false, ct);
        Serilog.Log.Information("Job {JobMasterId} level {Level}: approver {ActorUserId} approved (jobapproverid {JobApproverId})",
            job.jobmasterid, approverRow.wlevel, actorUserId, jobApproverId);

        var outcome = await TryAdvanceLevelAsync(context, job, approverRow.wlevel ?? 0, actorUserId, ct);
        await context.SaveChangesAsync(ct);

        if (outcome != WorkflowOutcome.StillOpen)
            await NotifyRequesterAsync(context, job, outcome, ct);
    }

    public async Task RejectAsync(long jobApproverId, long actorUserId, string? comment, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var approverRow = await context.job_user_lists.FirstOrDefaultAsync(a => a.jobapproverid == jobApproverId, ct)
            ?? throw new InvalidOperationException($"ไม่พบรายการอนุมัติ id {jobApproverId}");
        if (!string.Equals(approverRow.jobstatus, StatusPending, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("รายการนี้ถูกดำเนินการไปแล้ว ไม่สามารถปฏิเสธซ้ำได้");
        if (approverRow.userid != actorUserId)
            throw new InvalidOperationException("คุณไม่ใช่ผู้ได้รับมอบหมายให้อนุมัติรายการนี้");

        var job = await context.job_masters.FirstOrDefaultAsync(j => j.jobmasterid == approverRow.jobmasterid, ct)
            ?? throw new InvalidOperationException("ไม่พบ job_master ของรายการนี้");
        if (job.isJobClosed == true)
            throw new InvalidOperationException("งานนี้ปิดแล้ว ไม่สามารถดำเนินการต่อได้");
        // Block: reject bounce-back can revisit the same wlevel in a later
        // round (jobseq), so wlevel alone no longer uniquely identifies "is
        // this row still the open round" — must also match the current
        // jobseq. Null-coalesced to 0 on both sides so jobs that never
        // bounce (jobseq always null on both job and every row) are
        // unaffected: null==null coalesces to 0==0, always true, same as
        // before this guard existed.
        if (approverRow.wlevel != job.lastLevel || (approverRow.jobseq ?? 0) != (job.jobseq ?? 0))
            throw new InvalidOperationException("งานนี้เลื่อนผ่านระดับนี้ไปแล้ว ไม่สามารถดำเนินการกับรายการเก่านี้ได้");

        approverRow.jobstatus = StatusRejected;
        approverRow.approvedate = DateTime.Now;
        approverRow.comment = comment;
        await context.SaveChangesAsync(ct);

        await _auditLogger.LogChangeAsync(AuditActionType.Update, "job_user_list", jobApproverId.ToString(),
            new { jobstatus = StatusPending }, new { jobstatus = StatusRejected, comment }, isSensitive: false, ct);
        Serilog.Log.Information("Job {JobMasterId} level {Level}: approver {ActorUserId} rejected (jobapproverid {JobApproverId})",
            job.jobmasterid, approverRow.wlevel, actorUserId, jobApproverId);

        // Block 5 change from Block 2: a single reject no longer
        // unconditionally kills the job on the spot — TryAdvanceLevelAsync's
        // evaluator only fails the level (and the job) once the remaining
        // possible approval weight can no longer reach the AND% threshold.
        // Non-AND levels still fail immediately (any rejected row => Failed
        // in EvaluateLevel below), matching Block 2/3 behavior exactly.
        var outcome = await TryAdvanceLevelAsync(context, job, approverRow.wlevel ?? 0, actorUserId, ct);
        await context.SaveChangesAsync(ct);

        if (outcome != WorkflowOutcome.StillOpen)
            await NotifyRequesterAsync(context, job, outcome, ct);
    }

    // Requester-initiated withdrawal of their own still-open job — the one
    // capability this engine never had until now (see the Leave "cancel
    // request" feature this was built for). Deliberately does NOT touch
    // TryAdvanceLevelAsync/NotifyRequesterAsync — there is no approval
    // outcome to propagate, the requester already knows they cancelled it.
    // Bulk-flipping every still-PENDING job_user_list row (not just the
    // current level's) is required, not optional: GetMyInboxAsync filters
    // strictly on jobstatus == StatusPending, so any row left at that value
    // would keep showing up in an approver's inbox forever for a job that
    // no longer exists in any meaningful sense.
    public async Task CancelAsync(long jobMasterId, long actorUserId, bool isAdminOverride, string? reason, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var job = await context.job_masters.FirstOrDefaultAsync(j => j.jobmasterid == jobMasterId, ct)
            ?? throw new InvalidOperationException($"ไม่พบงาน id {jobMasterId}");
        if (job.isJobClosed == true)
            throw new InvalidOperationException("งานนี้ปิดแล้ว ไม่สามารถยกเลิกได้");
        if (!isAdminOverride && job.createuserid != actorUserId)
            throw new InvalidOperationException("ยกเลิกได้เฉพาะผู้ยื่นคำขอเองเท่านั้น");

        var pendingRows = await context.job_user_lists
            .Where(a => a.jobmasterid == jobMasterId && a.jobstatus == StatusPending)
            .ToListAsync(ct);
        foreach (var row in pendingRows)
        {
            row.jobstatus = StatusCancelled;
            row.approvedate = DateTime.Now;
            row.comment = reason;
        }

        job.status = StatusCancelled;
        job.isJobClosed = true;
        job.reasonClosed = reason;
        await context.SaveChangesAsync(ct);

        await _auditLogger.LogChangeAsync(AuditActionType.Update, "job_master", jobMasterId.ToString(),
            new { status = StatusPending }, new { status = StatusCancelled, reason }, isSensitive: false, ct);
        Serilog.Log.Information("Job {JobMasterId} cancelled by user {ActorUserId} (admin override: {IsAdminOverride})",
            jobMasterId, actorUserId, isAdminOverride);
    }

    // "ไม่อนุมัติ (Decline)" — the terminal NO, allowed ONLY at the istop
    // (final) level per the owner's two-action model: a mid-flow approver who
    // disagrees must "ส่งกลับแก้ไข" (SendBackAsync) instead, never kill the job
    // outright. Requires a reason. Closes the job; nothing advances.
    public async Task DeclineAsync(long jobApproverId, long actorUserId, string? comment, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var approverRow = await context.job_user_lists.FirstOrDefaultAsync(a => a.jobapproverid == jobApproverId, ct)
            ?? throw new InvalidOperationException($"ไม่พบรายการอนุมัติ id {jobApproverId}");
        if (!string.Equals(approverRow.jobstatus, StatusPending, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("รายการนี้ถูกดำเนินการไปแล้ว");
        if (approverRow.userid != actorUserId)
            throw new InvalidOperationException("คุณไม่ใช่ผู้ได้รับมอบหมายให้ดำเนินการรายการนี้");
        if (string.IsNullOrWhiteSpace(comment))
            throw new InvalidOperationException("กรุณาระบุเหตุผลในการไม่อนุมัติ");

        var job = await context.job_masters.FirstOrDefaultAsync(j => j.jobmasterid == approverRow.jobmasterid, ct)
            ?? throw new InvalidOperationException("ไม่พบ job_master ของรายการนี้");
        if (job.isJobClosed == true)
            throw new InvalidOperationException("งานนี้ปิดแล้ว ไม่สามารถดำเนินการต่อได้");
        if (approverRow.wlevel != job.lastLevel || (approverRow.jobseq ?? 0) != (job.jobseq ?? 0))
            throw new InvalidOperationException("งานนี้เลื่อนผ่านระดับนี้ไปแล้ว ไม่สามารถดำเนินการกับรายการเก่านี้ได้");

        var snapshot = await context.job_subworkflow_masters
            .FirstOrDefaultAsync(s => s.jobmasterid == job.jobmasterid && s.wlevel == approverRow.wlevel, ct)
            ?? throw new InvalidOperationException("ไม่พบ config ระดับนี้ของงานนี้");
        if (!snapshot.istop)
            throw new InvalidOperationException("\"ไม่อนุมัติ (Decline)\" ทำได้เฉพาะระดับสุดท้ายเท่านั้น — ระดับกลางที่ไม่เห็นด้วยให้ใช้ \"ส่งกลับแก้ไข\" แทน");

        approverRow.jobstatus = StatusRejected;
        approverRow.approvedate = DateTime.Now;
        approverRow.comment = comment;
        approverRow.isLast = false;

        job.status = StatusRejected; // terminal "ไม่อนุมัติ" — a clear declined state, not RETURNED (rework)
        job.isJobClosed = true;
        job.reasonClosed = comment;
        job.approvedDate = DateTime.Now;
        job.approvedUserID = actorUserId;
        await context.SaveChangesAsync(ct);

        await _auditLogger.LogChangeAsync(AuditActionType.Update, "job_user_list", jobApproverId.ToString(),
            new { jobstatus = StatusPending }, new { jobstatus = StatusRejected, action = "Decline", comment }, isSensitive: false, ct);
        Serilog.Log.Information("Job {JobMasterId} DECLINED (terminal) at istop level {Level} by user {ActorUserId}",
            job.jobmasterid, approverRow.wlevel, actorUserId);

        await NotifyRequesterAsync(context, job, WorkflowOutcome.Rejected, ct);
    }

    // "ส่งกลับแก้ไข (Send Back)" — the holder returns the document to the
    // earlier level configured on this level's backwardlevel, for rework; the
    // job stays OPEN. This is the ONLY way to reverse a decision once it has
    // left an approver's hands (there is deliberately no self-recall — see the
    // owner's "paper on the desk" model). Requires a reason. Routes through the
    // existing bounce logic so isLast/jobseq/new-round rows are handled exactly
    // like an engine-initiated bounce.
    public async Task SendBackAsync(long jobApproverId, long actorUserId, string? comment, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var approverRow = await context.job_user_lists.FirstOrDefaultAsync(a => a.jobapproverid == jobApproverId, ct)
            ?? throw new InvalidOperationException($"ไม่พบรายการอนุมัติ id {jobApproverId}");
        if (!string.Equals(approverRow.jobstatus, StatusPending, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("รายการนี้ถูกดำเนินการไปแล้ว");
        if (approverRow.userid != actorUserId)
            throw new InvalidOperationException("คุณไม่ใช่ผู้ได้รับมอบหมายให้ดำเนินการรายการนี้");
        if (string.IsNullOrWhiteSpace(comment))
            throw new InvalidOperationException("กรุณาระบุเหตุผลในการส่งกลับแก้ไข");

        var job = await context.job_masters.FirstOrDefaultAsync(j => j.jobmasterid == approverRow.jobmasterid, ct)
            ?? throw new InvalidOperationException("ไม่พบ job_master ของรายการนี้");
        if (job.isJobClosed == true)
            throw new InvalidOperationException("งานนี้ปิดแล้ว ไม่สามารถดำเนินการต่อได้");
        if (approverRow.wlevel != job.lastLevel || (approverRow.jobseq ?? 0) != (job.jobseq ?? 0))
            throw new InvalidOperationException("งานนี้เลื่อนผ่านระดับนี้ไปแล้ว ไม่สามารถดำเนินการกับรายการเก่านี้ได้");

        var snapshot = await context.job_subworkflow_masters
            .FirstOrDefaultAsync(s => s.jobmasterid == job.jobmasterid && s.wlevel == approverRow.wlevel, ct)
            ?? throw new InvalidOperationException("ไม่พบ config ระดับนี้ของงานนี้");
        if (snapshot.backwardlevel is null)
            throw new InvalidOperationException("workflow นี้ไม่ได้เปิดให้ \"ส่งกลับแก้ไข\" ที่ระดับนี้ (ไม่ได้ตั้ง backwardlevel) — โปรดตั้งค่าที่ระดับการอนุมัติก่อน");

        approverRow.jobstatus = StatusReturned;
        approverRow.approvedate = DateTime.Now;
        approverRow.comment = comment;

        // Route back exactly like an engine bounce (increments round, resets the
        // target level, issues a fresh approver round, flips isLast). Returns
        // false only for an invalid backwardlevel config — nothing persisted yet
        // in that case, so throwing leaves the context clean.
        var bounced = await TryBounceBackAsync(context, job, snapshot, ct);
        if (!bounced)
            throw new InvalidOperationException("ส่งกลับไม่สำเร็จ — ตรวจสอบการตั้งค่า backwardlevel ของ workflow (ต้องเป็นระดับก่อนหน้าที่มีอยู่จริง)");
        await context.SaveChangesAsync(ct);

        await _auditLogger.LogChangeAsync(AuditActionType.Update, "job_user_list", jobApproverId.ToString(),
            new { jobstatus = StatusPending }, new { jobstatus = StatusReturned, action = "SendBack", toLevel = snapshot.backwardlevel, comment }, isSensitive: false, ct);
        Serilog.Log.Information("Job {JobMasterId} SENT BACK from level {Level} to level {ToLevel} by user {ActorUserId}",
            job.jobmasterid, approverRow.wlevel, snapshot.backwardlevel, actorUserId);

        await NotifyRequesterAsync(context, job, WorkflowOutcome.BouncedBack, ct);
    }

    // Admin-only: fills a vacant approver slot (userid == null, created by
    // AssignLevelApproversAsync when resolution found nobody and
    // isAutoApproveAllow was false on that level) with a real person. The
    // assignee still has to actually click Approve/Reject themselves — this
    // does not auto-approve on their behalf.
    public async Task AssignApproverAsync(long jobApproverId, long assigneeUserId, string? note, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var row = await context.job_user_lists.FirstOrDefaultAsync(a => a.jobapproverid == jobApproverId, ct)
            ?? throw new InvalidOperationException($"ไม่พบรายการอนุมัติ id {jobApproverId}");
        if (row.userid is not null)
            throw new InvalidOperationException("รายการนี้มีผู้อนุมัติอยู่แล้ว ไม่ใช่ตำแหน่งว่าง");
        if (!string.Equals(row.jobstatus, StatusPending, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("รายการนี้ถูกดำเนินการไปแล้ว");

        var assignee = await context.sc_users.FirstOrDefaultAsync(u => u.userid == assigneeUserId, ct)
            ?? throw new InvalidOperationException($"ไม่พบผู้ใช้ id {assigneeUserId}");

        row.userid = assigneeUserId;
        row.empid = assignee.empid;
        row.reason = string.IsNullOrWhiteSpace(note) ? row.reason : $"{row.reason} | มอบหมายโดย admin: {note}";
        await context.SaveChangesAsync(ct);

        await _auditLogger.LogChangeAsync(AuditActionType.Update, "job_user_list", jobApproverId.ToString(),
            new { userid = (long?)null }, new { userid = assigneeUserId, note }, isSensitive: false, ct);
        Serilog.Log.Information("Vacant approver slot {JobApproverId} on job {JobMasterId} assigned to user {AssigneeUserId} by admin",
            jobApproverId, row.jobmasterid, assigneeUserId);
    }

    public async Task<List<job_user_list>> GetMyInboxAsync(long userId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.job_user_lists
            .Include(a => a.jobmaster).ThenInclude(j => j.workflow)
            .Where(a => a.userid == userId && a.jobstatus == StatusPending)
            .OrderBy(a => a.jobmaster.createdate)
            .ToListAsync(ct);
    }

    // Vacant slots (nobody resolved, isAutoApproveAllow was false on that
    // level) waiting for an admin to call AssignApproverAsync — the "ค้างไว้
    // จน admin หาคนอนุมัติได้" case from the plan.
    public async Task<List<job_user_list>> GetVacantApprovalsAsync(CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.job_user_lists
            .Include(a => a.jobmaster)
            .Where(a => a.userid == null && a.jobstatus == StatusPending)
            .OrderBy(a => a.jobmaster.createdate)
            .ToListAsync(ct);
    }

    public async Task<job_master?> GetJobDetailAsync(long jobMasterId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.job_masters
            .Include(j => j.job_user_lists.OrderBy(a => a.wlevel).ThenBy(a => a.jobapproverid))
            .Include(j => j.workflow)
            .FirstOrDefaultAsync(j => j.jobmasterid == jobMasterId, ct);
    }

    // Block 5: decides whether a fully-resolved round of job_user_list rows
    // at a level means the level is Complete, has definitively Failed, or is
    // still StillPending. AND-condition levels sum andPercent weight against
    // the configured threshold; ordinary levels keep the Block 2/3 rule
    // (any reject fails, otherwise all-must-approve).
    // Pure level-approval decision: given a level's config snapshot and its
    // approver rows, is the level Complete / Failed / StillPending? The heart
    // of "a job silently stuck" and "a job wrongly approved" bugs, so it is
    // public-static and unit-tested (WorkflowEvaluateLevelTests) rather than
    // only exercised through the EF-bound engine.
    public static LevelOutcome EvaluateLevel(job_subworkflow_master snapshot, List<job_user_list> rows)
    {
        if (rows.Count == 0)
            return LevelOutcome.StillPending;

        bool IsPending(job_user_list r) => string.Equals(r.jobstatus, StatusPending, StringComparison.OrdinalIgnoreCase);
        bool IsRejected(job_user_list r) => string.Equals(r.jobstatus, StatusRejected, StringComparison.OrdinalIgnoreCase);
        bool IsApprovedLike(job_user_list r) => string.Equals(r.jobstatus, StatusApproved, StringComparison.OrdinalIgnoreCase);

        if (snapshot.isandcondition)
        {
            var threshold = snapshot.andpercent ?? 100m;
            var approvedWeight = rows.Where(IsApprovedLike).Sum(r => r.andPercent ?? 0);
            if (approvedWeight >= threshold)
                return LevelOutcome.Complete;

            var pendingWeight = rows.Where(IsPending).Sum(r => r.andPercent ?? 0);
            if (approvedWeight + pendingWeight < threshold)
                return LevelOutcome.Failed; // remaining votes can no longer reach threshold

            return LevelOutcome.StillPending;
        }

        // OR-condition: any single approval completes the level immediately
        // (remaining rows become moot, same "moot leftover" pattern as an
        // AND-threshold completing early above). Checked AFTER isandcondition
        // deliberately — if a level is somehow misconfigured with both flags
        // true, AND wins, a defined precedence rather than accidental
        // fallthrough. Every workflow seeded before this feature has
        // isorcondition=false, so this branch is provably unreachable for
        // any config that existed before it.
        if (snapshot.isorcondition)
        {
            if (rows.Any(IsApprovedLike))
                return LevelOutcome.Complete;
            if (rows.All(IsRejected))
                return LevelOutcome.Failed; // nobody left who could still approve
            return LevelOutcome.StillPending;
        }

        if (rows.Any(IsRejected))
            return LevelOutcome.Failed;
        if (rows.Any(IsPending))
            return LevelOutcome.StillPending;
        return LevelOutcome.Complete;
    }

    // Shared by ApproveAsync/RejectAsync (a real person just acted) and
    // AssignLevelApproversAsync's auto-skip path (a vacant+auto-approve
    // level just skipped itself) — checks whether `completedLevel`'s
    // currently-open round (a Block 6 vertical precheck hop, or the level's
    // own real round) is now fully resolved, and if so either advances the
    // hop/level machinery accordingly. actorUserId is null for
    // system-driven auto-skip advances (no human actually clicked anything).
    private async Task<WorkflowOutcome> TryAdvanceLevelAsync(HRMContext context, job_master job, int completedLevel, long? actorUserId, CancellationToken ct)
    {
        var completedSnapshot = await context.job_subworkflow_masters
            .FirstOrDefaultAsync(s => s.jobmasterid == job.jobmasterid && s.wlevel == completedLevel, ct)
            ?? throw new InvalidOperationException($"ไม่พบ config ระดับ {completedLevel} ของงานนี้ — ข้อมูล snapshot ไม่ครบ");

        // Round-scoped: after a bounce-back (Block: reject bounce-back),
        // this level may have leftover rows from an earlier, superseded
        // round. Coalescing both sides to 0 means jobs that never bounce
        // (job.jobseq and every row's jobseq always null) see an identical
        // row set to before this scoping existed — provably zero behavior
        // change for any job that doesn't use backwardlevel.
        var currentJobseq = job.jobseq ?? 0;
        var allRows = await context.job_user_lists
            .Where(a => a.jobmasterid == job.jobmasterid && a.wlevel == completedLevel && (a.jobseq ?? 0) == currentJobseq)
            .ToListAsync(ct);

        var precheckRows = allRows.Where(r => r.reason is not null && r.reason.StartsWith(VerticalPrecheckMarker, StringComparison.Ordinal)).ToList();
        var realRoundRows = allRows.Where(r => r.reason is null || !r.reason.StartsWith(VerticalPrecheckMarker, StringComparison.Ordinal)).ToList();

        if (realRoundRows.Count == 0)
        {
            // The level's own real round hasn't been issued yet — only
            // precheck hop(s) exist so far. Hops are always single-approver
            // rounds (no AND% weighting applies to them, see
            // AssignLevelApproversAsync), so they still need to fully
            // resolve before deciding the next step, unlike the AND%
            // evaluation below which must react immediately.
            if (precheckRows.Any(a => string.Equals(a.jobstatus, StatusPending, StringComparison.OrdinalIgnoreCase)))
                return WorkflowOutcome.StillOpen;

            if (precheckRows.Any(a => string.Equals(a.jobstatus, StatusRejected, StringComparison.OrdinalIgnoreCase)))
            {
                var precheckRejectComment = precheckRows.LastOrDefault(a => string.Equals(a.jobstatus, StatusRejected, StringComparison.OrdinalIgnoreCase))?.comment;
                if (await TryBounceBackAsync(context, job, completedSnapshot, ct))
                    return WorkflowOutcome.BouncedBack;
                FailJob(job, completedSnapshot, precheckRejectComment);
                return WorkflowOutcome.Rejected;
            }

            // All hops resolved (or no hops configured at all) — let
            // AssignLevelApproversAsync decide whether to issue the next
            // hop or the level's own real round; it re-derives
            // hopsSatisfied itself from these same rows.
            var liveLevelForHop = await context.wf_sub_workflow_masters
                .FirstOrDefaultAsync(s => s.workflowid == job.workflowid && s.wlevel == completedLevel, ct)
                ?? throw new InvalidOperationException($"ไม่พบ config ต้นฉบับของระดับ {completedLevel} ใน wf_sub_workflow_master แล้ว (อาจถูกลบหลังงานเริ่ม)");
            return await AssignLevelApproversAsync(context, job, liveLevelForHop, ct);
        }

        // The real round has been issued — evaluate it every time a row
        // changes, not just once everything resolves. This matters for
        // AND-condition levels: the threshold can be reached (or become
        // unreachable) while other rows are still Pending, and those
        // leftover rows are then simply moot (the level has already moved
        // on). Non-AND levels get identical behavior to Block 2/3 here
        // since EvaluateLevel's non-AND branch still requires everything
        // resolved before returning Complete/Failed.
        var outcome = EvaluateLevel(completedSnapshot, realRoundRows);
        if (outcome == LevelOutcome.StillPending)
            return WorkflowOutcome.StillOpen;

        if (outcome == LevelOutcome.Failed)
        {
            var rejectComment = realRoundRows.LastOrDefault(r => string.Equals(r.jobstatus, StatusRejected, StringComparison.OrdinalIgnoreCase))?.comment;
            if (await TryBounceBackAsync(context, job, completedSnapshot, ct))
                return WorkflowOutcome.BouncedBack;
            FailJob(job, completedSnapshot, rejectComment);
            return WorkflowOutcome.Rejected;
        }

        // outcome == Complete. Decided by istop (the level explicitly
        // marked as terminal), not by comparing to job.maxlevel — that
        // count-based check was a latent bug from Block 2/3 (see Block 4
        // notes in the plan file): LOA branching can jump straight past
        // several levels, so a count-based check would be flatly wrong.
        if (completedSnapshot.istop)
        {
            // Block 9: display status becomes the level's configured
            // "approved" text instead of a hardcoded engine constant.
            job.status = completedSnapshot.forwardstatus ?? StatusCompleted;
            job.isJobClosed = true;
            job.approvedDate = DateTime.Now;
            job.approvedUserID = actorUserId;
            Serilog.Log.Information("Job {JobMasterId} closed: Approved at level {Level}", job.jobmasterid, completedLevel);
            return WorkflowOutcome.Approved;
        }

        var nextLevelNo = completedSnapshot.isLOA
            ? await ResolveNextLevelViaLoaAsync(context, job, completedLevel, ct)
            : completedLevel + 1;

        var nextSnapshotLevel = await context.job_subworkflow_masters
            .FirstOrDefaultAsync(s => s.jobmasterid == job.jobmasterid && s.wlevel == nextLevelNo, ct)
            ?? throw new InvalidOperationException($"ไม่พบ config ระดับถัดไป (level {nextLevelNo}) ของงานนี้ — ข้อมูล snapshot ไม่ครบ");

        job.lastLevel = nextSnapshotLevel.wlevel;

        // Resolve against the LIVE wf_sub_workflow_master row (not the
        // snapshot) — the snapshot freezes this level's rules, but the pool
        // of eligible people (role membership, custom-user list, org-chart
        // approver) is read live, since who's in a role/position can (and
        // should) change over the life of a long-running approval.
        var nextLiveLevel = await context.wf_sub_workflow_masters
            .FirstOrDefaultAsync(s => s.workflowid == job.workflowid && s.wlevel == nextSnapshotLevel.wlevel, ct)
            ?? throw new InvalidOperationException(
                $"ไม่พบ config ต้นฉบับของระดับ {nextSnapshotLevel.wlevel} ใน wf_sub_workflow_master แล้ว (อาจถูกลบหลังงานเริ่ม) — ไม่สามารถหาผู้อนุมัติระดับถัดไปได้");

        return await AssignLevelApproversAsync(context, job, nextLiveLevel, ct);
    }

    // Terminates the job as failed/returned — used both for ordinary
    // single-reject failures and for an AND-condition level whose remaining
    // approval weight can no longer reach threshold. Block 9: display
    // status becomes the level's configured "returned/rejected" text.
    private static void FailJob(job_master job, job_subworkflow_master completedSnapshot, string? comment)
    {
        job.status = completedSnapshot.backwardstatus ?? StatusRejected;
        job.isJobClosed = true;
        job.reasonClosed = comment;
        Serilog.Log.Information("Job {JobMasterId} closed: Rejected/Returned at level {Level}", job.jobmasterid, completedSnapshot.wlevel);
    }

    // Reject bounce-back: when a level fails (single reject on a non-AND/OR
    // level, or an AND-threshold that became unreachable) and its config has
    // backwardlevel set to a real EARLIER level in this same job, route the
    // job back there for a fresh round instead of terminating it outright.
    // Returns false (do nothing) whenever bounce-back doesn't apply, so the
    // caller falls through to the existing FailJob behavior — this is the
    // single most important regression-safety property here: every workflow
    // that doesn't set backwardlevel (>99% of what exists today) must behave
    // byte-for-byte as before this feature existed.
    private async Task<bool> TryBounceBackAsync(HRMContext context, job_master job, job_subworkflow_master completedSnapshot, CancellationToken ct)
    {
        var backwardLevel = completedSnapshot.backwardlevel;
        if (backwardLevel is null)
            return false; // not configured -> zero extra queries, fall through to FailJob

        // Don't trust config blindly: must be a real earlier level, never
        // forward/self (which would be a no-op-forever or same-level loop).
        if (backwardLevel < 1 || backwardLevel >= completedSnapshot.wlevel)
            return false;

        // Defensive cap: if two levels are misconfigured to bounce back and
        // forth at each other, a human still has to keep rejecting/approving
        // every round for it to continue (nothing here loops automatically),
        // but cap it anyway so a bad config can't be ridden forever.
        if ((job.jobseq ?? 0) >= 20)
            return false;

        var targetSnapshot = await context.job_subworkflow_masters
            .FirstOrDefaultAsync(s => s.jobmasterid == job.jobmasterid && s.wlevel == backwardLevel.Value, ct);
        if (targetSnapshot is null)
            return false; // defensive: every level is snapshotted at StartJobAsync, should always exist

        var targetLiveLevel = await context.wf_sub_workflow_masters
            .FirstOrDefaultAsync(s => s.workflowid == job.workflowid && s.wlevel == backwardLevel.Value, ct);
        if (targetLiveLevel is null)
            return false; // defensive: live level config deleted since job started

        job.jobseq = (job.jobseq ?? 0) + 1; // new round starts
        job.lastLevel = backwardLevel.Value;
        // Block 9 Moving Status field, reused for its intended purpose here.
        job.status = targetSnapshot.backwardstatus ?? StatusRejected;
        job.remark = (string.IsNullOrEmpty(job.remark) ? "" : job.remark + "\n")
            + $"{DateTime.Now:yyyy-MM-dd HH:mm}: ตีกลับจากระดับ {completedSnapshot.wlevel} ไปยังระดับ {backwardLevel.Value} (รอบที่ {job.jobseq})";

        Serilog.Log.Information("Job {JobMasterId} bounced back from level {FromLevel} to level {ToLevel} (round {Jobseq})",
            job.jobmasterid, completedSnapshot.wlevel, backwardLevel.Value, job.jobseq);

        await AssignLevelApproversAsync(context, job, targetLiveLevel, ct); // fresh round of job_user_list rows
        return true;
    }

    // LOA (Level of Authority) amount-based branching, Block 4. When the
    // level just cleared has isLOA=true, the next level isn't simply +1 —
    // it's looked up from wf_loa by matching job.reqamont into a [min,max]
    // band for (nowWorkflowid=job.workflowid, nowLevel=completedLevel).
    // Records a job_loa snapshot row for audit history (which band/amount
    // decided the branch) same as job_user_list records who approved.
    //
    // wf_loa also carries orgcode/levelcode columns whose exact filtering
    // intent isn't spelled out in the plan doc — treated here as an
    // optional extra filter: a row with orgcode set only matches when it
    // equals the requester's own org (job.reqOrg), a row with orgcode null
    // matches any org (wildcard). levelcode is not used for filtering
    // (unclear what it's meant to key against) — flagging here rather than
    // guessing further; revisit if a real LOA config actually needs it.
    //
    // wf_loa.nextWorkflowId can differ from nowWorkflowid, which would mean
    // jumping to an ENTIRELY different wf_workflow mid-job (re-snapshotting
    // job_subworkflow_master for the new workflow, etc.) — that's a
    // materially bigger feature than "which level within this workflow" and
    // isn't implemented; such a row throws a clear NotSupportedException
    // instead of silently only moving the level number.
    private static async Task<int> ResolveNextLevelViaLoaAsync(HRMContext context, job_master job, int completedLevel, CancellationToken ct)
    {
        var matched = await FindLoaBandAsync(context, job, completedLevel, ct);

        if (matched.nextWorkflowId != matched.nowWorkflowid)
            throw new NotSupportedException(
                $"wf_loa id {matched.id} ระบุให้ข้ามไป workflow อื่น (nextWorkflowId={matched.nextWorkflowId} != nowWorkflowid={matched.nowWorkflowid}) — ยังไม่รองรับการข้าม workflow กลางทาง ต้องออกแบบเพิ่ม (re-snapshot job_subworkflow_master ของ workflow ปลายทาง ฯลฯ) ไม่ใช่แค่เปลี่ยนเลข level");

        context.job_loas.Add(new job_loa
        {
            jobmasterid = job.jobmasterid,
            wlevel = completedLevel,
            value = job.reqamont!.Value,
            workflowid = job.workflowid,
            loaid = matched.loaid,
            isActive = true,
            moddate = DateTime.Now,
        });

        Serilog.Log.Information("Job {JobMasterId} level {Level}: LOA band matched (wf_loa.id={LoaBandId}, amount={Amount:N2}) -> next level {NextLevel}",
            job.jobmasterid, completedLevel, matched.id, job.reqamont, matched.nextLevel);

        return matched.nextLevel;
    }

    // Shared band lookup used both to pick the next level after an isLOA
    // level completes (ResolveNextLevelViaLoaAsync) and to resolve WHO
    // approves an isLOA level in the first place (ResolveLoaApproversAsync)
    // — both are keyed off the same (workflow, level, amount) match, just
    // reading different columns off the matched row afterward.
    private static async Task<wf_loa> FindLoaBandAsync(HRMContext context, job_master job, int level, CancellationToken ct)
    {
        if (job.reqamont is null)
            throw new InvalidOperationException(
                $"ระดับ {level} ตั้งค่าเป็น LOA (isLOA) แต่งานนี้ไม่มีจำนวนเงิน (reqamont) ระบุไว้ตอนเริ่มงาน — ไม่สามารถหาวงเงินที่ตรงกันได้");

        var candidates = await context.wf_loas
            .Where(l => l.wfid == job.workflowid && l.nowWorkflowid == job.workflowid
                && l.nowLevel == level && l.isactive != false)
            .ToListAsync(ct);

        var amount = job.reqamont.Value;
        return candidates
            .Where(l => amount >= (l.min ?? decimal.MinValue) && amount <= (l.max ?? decimal.MaxValue))
            .Where(l => string.IsNullOrEmpty(l.orgcode) || l.orgcode == job.reqOrg)
            .OrderByDescending(l => !string.IsNullOrEmpty(l.orgcode)) // org-specific band wins over a wildcard one
            .FirstOrDefault()
            ?? throw new InvalidOperationException(
                $"ไม่พบช่วงวงเงินใน wf_loa ที่ตรงกับจำนวนเงิน {amount:N2} ที่ระดับ {level} ของ workflow นี้ — ตรวจสอบการตั้งค่า LOA (min/max อาจไม่ครอบคลุมช่วงนี้)");
    }

    // Block 4 (approver side): when a level has isLOA=true, its approver
    // isn't resolved via the usual custom-user/custom-role/vertical flags —
    // it's whoever is listed in wf_loa_user for the wf_loa band that
    // matches this level + job.reqamont. wf_loa_user.loaid is a foreign key
    // into wf_loa.id (the specific band row's primary key, NOT the
    // wf_loa.loaid grouping column used for the job_loa audit snapshot) —
    // confirmed by the user. After resolving, the engine keeps running the
    // SAME level (this only answers "who", not "which level next" — that's
    // still ResolveNextLevelViaLoaAsync, triggered separately once this
    // level's approval completes).
    private static async Task<List<(long UserId, string? EmpId)>> ResolveLoaApproversAsync(
        HRMContext context, job_master job, wf_sub_workflow_master level, CancellationToken ct)
    {
        var matched = await FindLoaBandAsync(context, job, level.wlevel, ct);

        var today = DateOnly.FromDateTime(DateTime.Now);
        var loaUsers = await context.wf_loa_users
            .Where(u => u.loaid == matched.id && u.isactive
                && (u.startdate == null || u.startdate <= today)
                && (u.enddate == null || u.enddate >= today))
            .Select(u => new { u.userid, u.empid })
            .ToListAsync(ct);

        return loaUsers.Select(u => (u.userid, u.empid)).ToList();
    }

    // Safety cap on how many org-chart levels ResolveOrgChainApproverAsync
    // will climb when skip-searching (allowSkipVacant=true) — same pattern
    // as TryBounceBackAsync's jobseq>=20 cap: a misconfigured or cyclical
    // parent_code chain can't be ridden forever.
    private const int MaxVerticalHops = 20;

    // Block 2: shared org-chart walker used by BOTH the ordinary Vertical
    // strategy (isupperrole/isupperuser in ResolveCandidatesAsync) and the
    // Mix Approval pre-check hop-walker (AssignLevelApproversAsync) — a
    // single mechanism with two distinct calling conventions selected by
    // allowSkipVacant, so neither caller's existing, already-verified
    // behavior changes:
    //
    //   allowSkipVacant=false (Mix Approval's fixed-hop landing): walks
    //   BLIND up to hop `maxHops` (no approver check on the way — hop 1 =
    //   anchor itself, hop 2 = its parent, ...), then checks ONLY that
    //   landed org's approver. This reproduces the old
    //   ResolveVerticalHopApproverAsync byte-for-byte: each pre-check hop
    //   must resolve a DIFFERENT, higher org's approver, never falling back
    //   to an earlier hop's org if that landed org happens to be vacant.
    //
    //   allowSkipVacant=true (ordinary Vertical's new richer behavior):
    //   checks EVERY hop starting from the anchor, walking up through
    //   vacant orgs (no approver_userid/approver_empid set) until it finds
    //   a real one, hits org root (empty parent_code), or hits maxHops.
    //   Tied to the level's own isAutoApproveAllow flag at the call site —
    //   reusing that existing flag instead of adding a new column, per the
    //   plan (mirrors epms/JSP's separate isBlankBossContinue idea using
    //   what HRM already has). When isAutoApproveAllow is false, the caller
    //   passes maxHops=1 instead, which — combined with allowSkipVacant
    //   short-circuiting resolution the moment hop==maxHops — collapses
    //   this loop back to "check only the requester's own org", identical
    //   to Vertical resolution's behavior before this block existed.
    //
    // approver_empid (not boss_emp_id) is always the real workflow approver
    // at every org checked here, per the plan's explicit clarification — it
    // may be an acting substitute rather than the literal boss.
    private static async Task<List<(long UserId, string? EmpId)>> ResolveOrgChainApproverAsync(
        HRMContext context, string? anchorOrgCode, bool allowSkipVacant, int maxHops, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(anchorOrgCode))
            return new();

        var org = await context.com_organizations.FirstOrDefaultAsync(o => o.code == anchorOrgCode || o.orgCode == anchorOrgCode, ct);
        for (var hop = 1; org is not null && hop <= maxHops; hop++)
        {
            if (allowSkipVacant || hop == maxHops)
            {
                if (org.approver_userid is not null)
                    return new() { (org.approver_userid.Value, null) };

                if (!string.IsNullOrWhiteSpace(org.approver_empid))
                {
                    var approverUser = await context.sc_users.FirstOrDefaultAsync(u => u.empid == org.approver_empid, ct);
                    if (approverUser is not null)
                        return new() { (approverUser.userid, approverUser.empid) };
                }
            }

            if (string.IsNullOrWhiteSpace(org.parent_code))
                break; // hit org root

            org = await context.com_organizations.FirstOrDefaultAsync(o => o.code == org.parent_code, ct);
        }

        return new();
    }

    // Resolves candidates for `level` and creates job_user_list row(s)
    // accordingly. Block 6 first checks whether this level still needs N
    // vertical pre-check hops (isNeedsupervisorapprove) before its own
    // approver is even resolved — hopsSatisfied is re-derived from existing
    // rows each call, so this is safe to call repeatedly as hops clear.
    // Outcomes for whichever round (hop or real) is being issued:
    //   1. Real candidate(s) resolved -> one Pending row each, job waits.
    //   2. Nobody resolved but isAutoApproveAllow=true -> one auto-approved
    //      marker row (matches epms WorkflowController.cs's
    //      isAutoApproveAllow behavior) then immediately tries to advance
    //      again — so a chain of consecutive vacant+auto-approve
    //      levels/hops skips through in one call.
    //   3. Nobody resolved and isAutoApproveAllow=false -> one Pending row
    //      with userid=null ("ค้างไว้จน admin หาคนอนุมัติได้" per the plan).
    private async Task<WorkflowOutcome> AssignLevelApproversAsync(HRMContext context, job_master job, wf_sub_workflow_master level, CancellationToken ct)
    {
        // Block 9: display status reflects whichever level (or hop) is now
        // the open round — kept simple (no separate hop-specific status
        // text) since wf_sub_workflow_master only has one "pending" label.
        job.status = level.standstatus ?? StatusPending;

        // isLast marks the currently-active approver round (epms parity): the
        // moment a new round (a vertical hop, the level's real round, the next
        // level, or a bounce-back round) is issued below, every previously
        // active row stops being "last". The rows this call adds are the new
        // active set (isLast = true on each). Lets any reader find "who is this
        // sitting with right now" without re-deriving it from wlevel + jobseq.
        var priorLastRows = await context.job_user_lists
            .Where(a => a.jobmasterid == job.jobmasterid && a.isLast == true)
            .ToListAsync(ct);
        foreach (var r in priorLastRows) r.isLast = false;

        var neededHops = level.isNeedsupervisorapprove ?? 0;
        string? precheckReasonPrefix = null;
        List<(long UserId, string? EmpId)> candidates;

        if (neededHops > 0)
        {
            var hopsSatisfied = await context.job_user_lists.CountAsync(a =>
                a.jobmasterid == job.jobmasterid && a.wlevel == level.wlevel
                && a.reason != null && a.reason.StartsWith(VerticalPrecheckMarker)
                && a.jobstatus == StatusApproved, ct);

            if (hopsSatisfied < neededHops)
            {
                var hopNumber = hopsSatisfied + 1;
                candidates = await ResolveOrgChainApproverAsync(context, job.reqOrg, allowSkipVacant: false, maxHops: hopNumber, ct);
                precheckReasonPrefix = $"{VerticalPrecheckMarker} {hopNumber}/{neededHops}: รอหัวหน้าอนุมัติก่อนเข้าสู่ระดับนี้ (Mix Approval)";
            }
            else
            {
                candidates = await ResolveCandidatesAsync(context, job, level, ct);
            }
        }
        else
        {
            candidates = await ResolveCandidatesAsync(context, job, level, ct);
        }

        // Block 5: equal-split AND% weight only applies to the level's own
        // real round — a vertical precheck hop is always a single approver
        // (100% of that hop by construction), so AND% is meaningless there.
        // No per-person weight field exists anywhere in the schema, so an
        // equal split among resolved candidates is the only defensible
        // default without inventing one.
        decimal? andWeight = (precheckReasonPrefix is null && level.isandcondition && candidates.Count > 0)
            ? Math.Round(100m / candidates.Count, 2)
            : null;

        if (candidates.Count > 0)
        {
            Serilog.Log.Information("Job {JobMasterId} level {Level}{HopInfo}: resolved {Count} candidate(s)",
                job.jobmasterid, level.wlevel, precheckReasonPrefix is null ? "" : " (vertical pre-check hop)", candidates.Count);
            foreach (var (userId, empId) in candidates)
            {
                context.job_user_lists.Add(new job_user_list
                {
                    jobmasterid = job.jobmasterid,
                    workflowid = job.workflowid,
                    wlevel = level.wlevel,
                    userid = userId,
                    empid = empId,
                    subworkflowmasterid = level.subworkflowid,
                    jobstatus = StatusPending,
                    sendDate = DateTime.Now,
                    reason = precheckReasonPrefix,
                    andPercent = andWeight,
                    jobseq = job.jobseq,
                    isLast = true,
                });
            }
            return WorkflowOutcome.StillOpen;
        }

        if (level.isAutoApproveAllow)
        {
            Serilog.Log.Information("Job {JobMasterId} level {Level}: no candidates resolved, auto-skipping (isAutoApproveAllow)",
                job.jobmasterid, level.wlevel);
            context.job_user_lists.Add(new job_user_list
            {
                jobmasterid = job.jobmasterid,
                workflowid = job.workflowid,
                wlevel = level.wlevel,
                userid = null,
                subworkflowmasterid = level.subworkflowid,
                jobstatus = StatusApproved,
                isAutoApprove = true,
                approvedate = DateTime.Now,
                reason = precheckReasonPrefix is null
                    ? "ตำแหน่งผู้อนุมัติว่าง — ข้ามอัตโนมัติ (isAutoApproveAllow)"
                    : $"{precheckReasonPrefix} | ตำแหน่งว่าง — ข้ามอัตโนมัติ (isAutoApproveAllow)",
                jobseq = job.jobseq,
                isLast = true,
            });
            await context.SaveChangesAsync(ct);
            return await TryAdvanceLevelAsync(context, job, level.wlevel, null, ct);
        }

        // No one resolved and this level doesn't allow auto-skip — including
        // the plan's explicit "last level vacant -> stay pending" case,
        // which falls out of this same branch naturally since there's no
        // special-casing of the last level here.
        Serilog.Log.Warning("Job {JobMasterId} level {Level}: no candidates resolved, left vacant for admin assignment (/wf/vacant-approvals)",
            job.jobmasterid, level.wlevel);
        context.job_user_lists.Add(new job_user_list
        {
            jobmasterid = job.jobmasterid,
            workflowid = job.workflowid,
            wlevel = level.wlevel,
            userid = null,
            subworkflowmasterid = level.subworkflowid,
            jobstatus = StatusPending,
            reason = precheckReasonPrefix is null
                ? "ตำแหน่งผู้อนุมัติว่าง — รอ admin มอบหมายผู้อนุมัติ (ดู /wf/vacant-approvals)"
                : $"{precheckReasonPrefix} | ตำแหน่งว่าง — รอ admin มอบหมายผู้อนุมัติ (ดู /wf/vacant-approvals)",
            jobseq = job.jobseq,
            isLast = true,
        });
        return WorkflowOutcome.StillOpen;
    }

    // Horizontal (custom user / custom role) + Vertical (org-chart) approver
    // resolution against the LIVE wf_sub_workflow_master row. Returns an
    // empty list — never throws — when the level is configured but nobody
    // currently resolves (a runtime vacancy, handled by the caller); still
    // throws for a genuine setup mistake (level has none of
    // isLOA/iscustomUser/iscustomRole/isupperrole/isupperuser ticked at all).
    //
    // Block 3: composable union resolution. isLOA remains fully exclusive
    // (per the user's explicit financial-control decision — see the block
    // below) but every OTHER strategy ticked on the level now runs
    // independently and its candidates are merged (deduped by userid),
    // replacing the old if/else-if chain that only ever ran the FIRST
    // matching strategy. Verified via a live-DB query immediately before
    // writing this change (`SELECT subworkflowid FROM wf_sub_workflow_master
    // WHERE (CAST(isupperrole AS INT)+CAST(isupperuser AS INT)+CAST(iscustomRole
    // AS INT)+CAST(iscustomUser AS INT)) > 1`) that returned zero rows — so
    // this union is provably zero-behavior-change for every level config
    // that existed before Block 3; it only changes behavior for a NEW level
    // deliberately configured with more than one strategy flag.
    private static async Task<List<(long UserId, string? EmpId)>> ResolveCandidatesAsync(
        HRMContext context, job_master job, wf_sub_workflow_master level, CancellationToken ct)
    {
        var requesterOrgCode = job.reqOrg;

        // Block 4 (approver side, per user confirmation): isLOA takes over
        // approver resolution entirely for this level, exclusive of every
        // other strategy — who approves is whoever wf_loa_user lists for the
        // amount band that matches this level, not the custom-user/custom-
        // role/vertical flags below. See ResolveLoaApproversAsync's own
        // comment for the exact FK chain.
        if (level.isLOA)
            return await ResolveLoaApproversAsync(context, job, level, ct);

        var candidates = new List<(long UserId, string? EmpId)>();
        var anyStrategyConfigured = false;

        if (level.iscustomUser)
        {
            anyStrategyConfigured = true;
            var users = await context.wf_custom_users
                .Where(u => u.subworkflowid == level.subworkflowid && u.isactive)
                .Select(u => new { u.userid, u.empid })
                .ToListAsync(ct);
            candidates.AddRange(users.Select(u => (u.userid, u.empid)));
        }

        if (level.iscustomRole)
        {
            anyStrategyConfigured = true;
            var roleIds = await context.wf_custom_roles
                .Where(r => r.subworkflowid == level.subworkflowid && r.isactive == true)
                .Select(r => r.roleid)
                .ToListAsync(ct);
            var userIds = await context.sc_user_roles
                .Where(ur => roleIds.Contains(ur.roleid))
                .Select(ur => ur.userid)
                .Distinct()
                .ToListAsync(ct);
            var withEmp = await context.sc_users
                .Where(u => userIds.Contains(u.userid))
                .Select(u => new { u.userid, u.empid })
                .ToListAsync(ct);
            candidates.AddRange(withEmp.Select(u => (u.userid, u.empid)));
        }

        if (level.isupperrole || level.isupperuser)
        {
            anyStrategyConfigured = true;
            // Block 2: Vertical resolution shares ResolveOrgChainApproverAsync
            // with the Mix Approval hop-walker (see that method's own comment
            // for the full allowSkipVacant/maxHops contract). isAutoApproveAllow
            // doubles here as "may this level's Vertical resolution climb past
            // a vacant org to find a real approver higher up the chart" —
            // reusing the existing flag instead of adding a new column, per
            // the plan. When false, only the requester's own org is checked
            // (maxHops=1), identical to Vertical resolution's behavior before
            // this block existed.
            var maxHops = level.isAutoApproveAllow ? MaxVerticalHops : 1;
            var vertical = await ResolveOrgChainApproverAsync(context, requesterOrgCode, level.isAutoApproveAllow, maxHops, ct);
            candidates.AddRange(vertical);
        }

        // Block 4 (epms-inspired strategies, adapted per the user's explicit
        // correction): isReturnSender routes back to whoever CREATED the job,
        // read directly off job_master — deliberately NOT epms's wlevel-2
        // hardcoded offset (breaks after any bounce/multi-round job; reading
        // the actual creator works at any level, any number of rounds).
        if (level.isReturnSender)
        {
            anyStrategyConfigured = true;
            if (job.createuserid is long senderUserId)
                candidates.Add((senderUserId, job.empid));
        }

        // isApproverSameCostCenter: any other active employee in the same
        // company sharing job.costcenter (snapshotted at StartJobAsync from
        // Hremployee.CostCenterCode — Block 2), excluding the requester
        // themselves. Resolves through sc_user the same way custom-role does.
        if (level.isApproverSameCostCenter)
        {
            anyStrategyConfigured = true;
            if (!string.IsNullOrWhiteSpace(job.costcenter) && !string.IsNullOrWhiteSpace(job.empid))
            {
                var requesterCompanyId = await context.Hremployee
                    .Where(e => e.EmpNo == job.empid)
                    .Select(e => e.companyid)
                    .FirstOrDefaultAsync(ct);
                var peerEmpNos = await context.Hremployee
                    .Where(e => e.CostCenterCode == job.costcenter && e.companyid == requesterCompanyId && e.EmpNo != job.empid)
                    .Select(e => e.EmpNo)
                    .ToListAsync(ct);
                var peerUsers = await context.sc_users
                    .Where(u => u.empid != null && peerEmpNos.Contains(u.empid))
                    .Select(u => new { u.userid, u.empid })
                    .ToListAsync(ct);
                candidates.AddRange(peerUsers.Select(u => (u.userid, u.empid)));
            }
        }

        // isAdhocUser: per-job override via wf_adhoc_user (Phase 1's CRUD at
        // /wf/adhoc-users already lets admins set jobmasterid — null there
        // means a level-wide template row, a real value targets one specific
        // job). Job-specific rows win outright when any exist; template rows
        // are the fallback so a level can also have a standing default.
        if (level.isAdhocUser)
        {
            anyStrategyConfigured = true;
            var jobSpecific = await context.wf_adhoc_users
                .Where(a => a.workflowid == level.workflowid && a.wlevel == level.wlevel && a.isactive && a.jobmasterid == job.jobmasterid)
                .Select(a => new { a.userid, a.empid })
                .ToListAsync(ct);
            var adhoc = jobSpecific.Count > 0
                ? jobSpecific
                : await context.wf_adhoc_users
                    .Where(a => a.workflowid == level.workflowid && a.wlevel == level.wlevel && a.isactive && a.jobmasterid == null)
                    .Select(a => new { a.userid, a.empid })
                    .ToListAsync(ct);
            candidates.AddRange(adhoc.Select(a => (a.userid, a.empid)));
        }

        if (!anyStrategyConfigured)
            throw new InvalidOperationException(
                $"ระดับ {level.wlevel} ของ workflow นี้ยังไม่ได้กำหนดประเภทผู้อนุมัติเลย (ไม่ได้ติ๊ก custom user / custom role / vertical ใดๆ เลย — ตั้งค่า config ไม่ครบ)");

        // Dedup by userid: a no-op for every level that only has one
        // strategy ticked (the common case, and the only case that existed
        // before Block 3) — only actually collapses anything when a level
        // deliberately unions multiple strategies AND the same person shows
        // up via more than one of them.
        return candidates.GroupBy(c => c.UserId).Select(g => g.First()).ToList();
    }

    // Best-effort email notification to the original requester when a job
    // closes (approved / rejected / bounced back) — including when closure
    // happens via an auto-skip chain with no human ever clicking anything
    // (see the 3 call sites in StartJobAsync/ApproveAsync/RejectAsync).
    // Must NEVER throw out of the caller's already-committed
    // approve/reject/start transaction — mirrors PayrollEmployeeAdmin.razor's
    // TrySendCredentialEmailAsync pattern exactly (catch, log, swallow).
    private async Task NotifyRequesterAsync(HRMContext context, job_master job, WorkflowOutcome outcome, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(job.empid))
                return;

            // job.empid stores Hremployee.EmpNo (a business code string),
            // not the numeric Hremployee.Id — confirmed by every existing
            // read-back site (PayrollCompanyResolver.cs, RecApplicationService.cs).
            var emp = await context.Hremployee.FirstOrDefaultAsync(e => e.EmpNo == job.empid, ct);
            if (emp is null)
                return;

            var email = await EmployeeEmailResolver.ResolveAsync(context, emp.id, ct);
            if (string.IsNullOrWhiteSpace(email))
                return;

            var outcomeLabel = outcome switch
            {
                WorkflowOutcome.Approved => "ได้รับการอนุมัติเรียบร้อยแล้ว",
                WorkflowOutcome.Rejected => "ถูกปฏิเสธ",
                WorkflowOutcome.BouncedBack => "ถูกตีกลับเพื่อแก้ไข กรุณาตรวจสอบและดำเนินการใหม่",
                _ => "มีการเปลี่ยนแปลงสถานะ",
            };
            var subject = $"ผลการอนุมัติ: {job.subject ?? job.wname}";
            var body = $"<p>คำขอของคุณเรื่อง \"{job.subject}\" {outcomeLabel}</p><p>สถานะปัจจุบัน: {job.status}</p>";
            await _emailSender.SendEmailAsync(email, subject, body);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Failed to send workflow closure notification for job {JobMasterId}", job.jobmasterid);
        }
    }

    // Display names of the approver(s) a freshly-submitted (or in-flight) job is
    // currently waiting on — resolved from the lowest level that still has
    // PENDING rows. Lets a requester see exactly who their request just went to.
    // Returns "" when nothing is pending (e.g. the job already closed/auto-approved).
    public async Task<string> GetPendingApproverNamesAsync(long jobMasterId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var pending = await context.job_user_lists
            .Where(a => a.jobmasterid == jobMasterId && a.jobstatus == StatusPending)
            .Select(a => new { a.wlevel, a.userid })
            .ToListAsync(ct);
        if (pending.Count == 0) return "";

        var minLevel = pending.Min(p => p.wlevel ?? 0);
        var userIds = pending.Where(p => (p.wlevel ?? 0) == minLevel && p.userid.HasValue)
            .Select(p => p.userid!.Value).Distinct().ToList();

        var users = await context.sc_users
            .Where(u => userIds.Contains(u.userid))
            .Select(u => new { u.userid, u.empid, u.loginname })
            .ToListAsync(ct);
        var empIds = users.Where(u => u.empid != null).Select(u => u.empid!).Distinct().ToList();
        var emps = await context.Hremployee
            .Where(e => empIds.Contains(e.EmpNo))
            .Select(e => new { e.EmpNo, e.EmpName, e.EmpSurname })
            .ToListAsync(ct);
        var nameByEmpNo = emps.ToDictionary(e => e.EmpNo, e => $"{e.EmpName} {e.EmpSurname}".Trim());

        var names = users.Select(u =>
            u.empid != null && nameByEmpNo.TryGetValue(u.empid, out var n) && !string.IsNullOrWhiteSpace(n)
                ? n
                : (!string.IsNullOrWhiteSpace(u.loginname) ? u.loginname! : $"#{u.userid}"));
        return string.Join(" / ", names.Distinct());
    }
}
