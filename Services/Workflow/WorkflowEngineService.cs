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

    // job_master.reasonClosed = "อะไรเป็นตัวปิดงานนี้" ไม่ใช่ข้อความเหตุผล
    // ตามต้นฉบับ epms (job.reasonClosed = JobStatusService.approve / "Decline")
    // ข้อความที่ผู้อนุมัติพิมพ์ไปอยู่ที่ job.remark ต่างหาก แยกสองอย่างนี้ออกจากกัน
    // job_master จึงตอบได้เองว่างานปิดเพราะอนุมัติ ไม่อนุมัติ หรือถูกยกเลิก
    public const string ClosedByApprove = "Approve";
    public const string ClosedByDecline = "Decline";
    public const string ClosedByCancel = "Cancel";
    public const string ClosedByAutoApprove = "AutoApprove";

    // ── "ใบงานนี้ยังรอทำอยู่จริงไหม" — กฎเดียวสำหรับทุกที่ที่อ่าน ─────────────
    //
    // CEO, 10 ก.ย. 2569: "ต้อง stamp ใน jobmaster ว่าปัจจุบัน job อยู่ level ไหน
    // (islast) แล้วก็ไปดูใน job_userlist นั้น (ตรงกันไหม)" และ "กรองแค่
    // jobstatus = PENDING ยังไม่ถูก ถ้ามีผู้อนุมัติหลายคน คุณจะอ่านผิดตลอด
    // แม้ว่า workflow เลื่อนไปแล้ว"
    //
    // ทำไมกรองแค่ PENDING ถึงผิด: ขั้นที่มีผู้อนุมัติหลายคน พอมีคนกดจนขั้นนั้นครบ
    // เงื่อนไขแล้วงานเดินต่อ ใบของคนอื่นในขั้นเดิมถูกปลด isLast=false ก็จริง แต่
    // jobstatus ยังเป็น PENDING ค้างไว้ตลอดไป (TryAdvanceLevelAsync / MoveAsync
    // ปลดแค่ isLast) ใครกรองแค่ PENDING จึงเห็นใบพวกนี้เป็น "งานค้าง" ตลอด
    // ทั้งที่งานเดินผ่านไปนานแล้ว
    //
    // ตัวชี้ขาดคือ job_master.lastLevel — บอกว่างานอยู่ขั้นไหน ณ ตอนนี้ ใบที่ยัง
    // ต้องทำจึงต้องเป็นขั้นเดียวกันนั้น และเป็นใบของรอบล่าสุด (isLast) เท่านั้น
    //
    // ตั้งใจไม่เอา jobseq มาร่วมเป็นเงื่อนไข แม้ guard ตอนเขียนของ engine เดิมจะ
    // เช็ค (approverRow.jobseq != job.jobseq) ด้วย เพราะสอง engine ให้ความหมาย
    // jobseq ไม่เหมือนกัน:
    //   - engine เดิม  jobseq เดินเมื่อเปิดรอบใหม่ (ถูกตีกลับแล้วเดินขึ้นมาใหม่)
    //   - engine ใหม่  jobseq เดินทุก action ตามที่ CEO สั่งไว้ 10 ก.ย. 2569
    //     ("ต้อง stamp ทุกครั้งที่มี workflow action" — jobseq เป็นของรอยเท้า)
    // ถ้าเอา jobseq มากรอง ขั้นที่มีผู้อนุมัติหลายคนจะพัง: พอคนแรกกดอนุมัติ
    // job.jobseq เดินไปแล้ว ใบของคนที่เหลือ (ออกตอน jobseq เก่า) จะหายจาก
    // กล่องงานทันที ทั้งที่ยังต้องอนุมัติอยู่ — isLast ทำหน้าที่แยกรอบอยู่แล้ว
    // (ทั้งสอง engine ปลด isLast ของรอบเก่าก่อนออกใบรอบใหม่เสมอ)
    public static readonly System.Linq.Expressions.Expression<Func<job_user_list, bool>> IsLiveApprovalRow =
        a => a.jobstatus == StatusPending
          && a.isLast == true
          && a.wlevel == a.jobmaster.lastLevel
          && a.jobmaster.isJobClosed != true;

    // Block 6 (Mix Approval): job_user_list.reason rows for a vertical
    // pre-check hop always start with this marker, so hop-completion count
    // and "which round just resolved" detection can be done by string match
    // instead of a new column — jobseq was deliberately NOT reused for this
    // (see epms WorkflowController.cs: job.jobseq filters job_user_list by
    // job-wide resubmission round, a different concept entirely — reusing
    // it here would collide with that semantic if resubmission is ever
    // implemented later).
    private const string VerticalPrecheckMarker = "VERTICAL_PRECHECK";
    // Self-terminating vertical chain (CEO, 2026-09-07) — see
    // AssignVerticalChainHopAsync/ResolveVerticalChainHopAsync. Distinct
    // marker from VerticalPrecheckMarker above: those hops gate a SEPARATE
    // final approval; these hops ARE the final approval, whichever one turns
    // out to be last.
    private const string VerticalChainMarker = "VERTICAL_CHAIN";
    // Fixed-count org-chart climb (CEO, 2026-09-07 follow-up, empLevel) —
    // see AssignEmpLevelClimbAsync. Distinct from VerticalChainMarker: that
    // mechanism self-terminates the WHOLE JOB once its climb finishes; this
    // one finishes the LEVEL and falls through to whatever comes next
    // (e.g. a role-based HR step), same as an ordinary level would.
    private const string EmpLevelClimbMarker = "EMP_LEVEL_CLIMB";

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

    // ตัวใหม่ถูกฉีดเข้ามาแบบขี้เกียจ (Lazy) เพราะทั้งสองตัวอ้างถึงกัน —
    // WorkflowService เรียก EvaluateLevel ของตัวนี้ ส่วนตัวนี้ส่งงานต่อให้ตัวนั้น
    private readonly IServiceProvider _sp;
    private WorkflowService NewEngine => _sp.GetRequiredService<WorkflowService>();

    public WorkflowEngineService(IDbContextFactory<HRMContext> dbFactory, EmailSender emailSender,
        HRM.Services.Audit.IAuditLogger auditLogger, IServiceProvider sp)
    {
        _dbFactory = dbFactory;
        _emailSender = emailSender;
        _auditLogger = auditLogger;
        _sp = sp;
    }

    // ── สลับเครื่องยนต์ด้วย config ───────────────────────────────────────
    //
    // CEO, 10 ก.ย. 2569: "เพราะเป็น configuration base น่าจะ config แล้วใช้ได้เลย"
    //
    // โมดูล 21 ตัวเรียกเมธอดของคลาสนี้ตรง ๆ ตั้งแต่ตอนเขียน การย้ายไปใช้ตัวใหม่
    // จึงเคยแปลว่าต้องแก้ call site ทุกตัวแล้ว deploy ใหม่ แทนที่จะทำแบบนั้น
    // คลาสนี้ถามที่ตัว workflow เองว่าจะให้ใครเดิน แล้วส่งต่อ — ย้ายโมดูลไหน
    // ก็แค่ติ๊ก wf_workflow.useNewEngine ถอยกลับก็ปลดติ๊ก ไม่ต้อง build
    private async Task<bool> UsesNewEngineAsync(HRMContext context, long workflowId, CancellationToken ct)
        => await context.wf_workflows.Where(w => w.workflowid == workflowId)
            .Select(w => w.useNewEngine).FirstOrDefaultAsync(ct) == true;

    private async Task<bool> JobUsesNewEngineAsync(HRMContext context, long jobMasterId, CancellationToken ct)
        => await context.job_masters.Where(j => j.jobmasterid == jobMasterId)
            .Select(j => j.workflow.useNewEngine).FirstOrDefaultAsync(ct) == true;

    private static WorkFlowViewModel Carry(long jobMasterId, long actorUserId, string? comment, long? reasonId)
        => new() { jobmasterid = jobMasterId, actorUserId = actorUserId, reason = comment, mas_reason_id = reasonId };

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

        // workflow นี้ย้ายไปเครื่องใหม่แล้ว -> ส่งต่อ
        // โมดูลเรียก StartJobAsync ตอนที่ "ผู้ขอกดส่ง" อยู่แล้ว งานจึงต้องเข้าสาย
        // อนุมัติทันที ไม่ใช่ค้างเป็น draft — สร้างแล้วเดินเข้าขั้นแรกให้เลย
        // พฤติกรรมที่โมดูลเห็นจึงเหมือนเดิมทุกประการ
        if (workflow.useNewEngine == true)
        {
            var created = await NewEngine.CreateAsync(workflowId, requesterUserId, requesterEmpId,
                subject, reftable, refid, amount, ct);
            await NewEngine.SubmitAsync(
                Carry(created.jobmasterid, requesterUserId, null, null), ct);
            return created.jobmasterid;
        }

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
        string? requesterName = null;
        if (!string.IsNullOrWhiteSpace(requesterEmpId))
        {
            var requesterEmp = await context.Hremployee
                .Where(e => e.EmpNo == requesterEmpId)
                .Select(e => new { e.orgcode, e.CostCenterCode, e.EmpName, e.EmpSurname })
                .FirstOrDefaultAsync(ct);
            requesterOrgCode = requesterEmp?.orgcode;
            requesterCostCenter = requesterEmp?.CostCenterCode;
            if (requesterEmp is not null)
                requesterName = $"{requesterEmp.EmpName} {requesterEmp.EmpSurname}".Trim();
        }
        // epms parity: snapshot the requester's real name onto the job so an
        // approver's inbox shows WHO asked, never an id/empno code. Prefer the
        // authoritative Hremployee name; fall back to the submitting sc_user.
        var submitter = await context.sc_users
            .Where(u => u.userid == requesterUserId)
            .Select(u => new { u.firstname, u.lastname, u.loginname })
            .FirstOrDefaultAsync(ct);
        if (string.IsNullOrWhiteSpace(requesterName) && submitter is not null)
            requesterName = $"{submitter.firstname} {submitter.lastname}".Trim();

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
            status = levels[0].sitinstatus ?? levels[0].standstatus ?? StatusPending,
            reftable = reftable,
            refid = refid,
            createuserid = requesterUserId,
            empid = requesterEmpId,
            reqName = requesterName,
            reqForName = requesterName,
            createusername = requesterName,
            createby = submitter?.loginname,
            reqOrg = requesterOrgCode,
            createdate = DateTime.Now,
            reqdate = DateTime.Now,
            reqamont = amount,
            costcenter = requesterCostCenter,
            isactive = true,
            isJobClosed = false,
            // jobseq starts at 1 (matches epms's real initial value) — every
            // later level transition, forward or backward, increments it by
            // 1 from here (see TryAdvanceLevelAsync/TryBounceBackAsync).
            jobseq = 1,
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

        // ประทับรอยเท้าขั้นแรกเท่านั้น — ขั้นถัดไปจะถูกประทับตอนงานเดินไปถึงจริง
        // (CEO, 9 ก.ย. 2569: "อ่านไปทีละขั้นๆ ไป stamp ใน job_master กับ job_subworkflow")
        context.job_subworkflow_masters.Add(BuildLevelSnapshot(job, levels[0], workflow.workflowcode, job.jobseq));
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
            job.reasonClosed = ClosedByAutoApprove;
            job.remark = "อนุมัติอัตโนมัติ (auto-approve)";
            job.approvedDate = DateTime.Now;
            job.enddate = DateTime.Now;
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

    // job_subworkflow_master คือ "รอยเท้าการเดิน" ของงาน ไม่ใช่สำเนา config
    // (CEO, 9 ก.ย. 2569). ระบบต้นฉบับประทับหนึ่งแถวทุกครั้งที่งานเข้าสู่ระดับหนึ่ง
    // พร้อม jobseq ของก้าวนั้น — TTMEPMS job 229 จึงมี 7 แถวจาก 4 ระดับ เพราะ
    // มันวน 0->1->0->1->0->1->2->3 เดิม HRM สร้างครบทุกระดับตั้งแต่เริ่มงานแล้ว
    // เขียนทับเมื่อวนกลับ ทำให้ประวัติการตีกลับหายทั้งหมด
    private static job_subworkflow_master BuildLevelSnapshot(
        job_master job, wf_sub_workflow_master level, string? workflowCode, int? jobseq = null)
    {
        return new job_subworkflow_master
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
            // epms: jobSubW.status = sub.sitinstatus — สถานะที่ตราไว้บนรอยเท้าคือ
            // "งานนั่งอยู่ที่ขั้นนี้" ไม่มีค่อยตกมาที่ standstatus
            status = level.sitinstatus ?? level.standstatus,
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
            verticalMaxLevel = level.verticalMaxLevel,
            isPool = level.isPool,
            wfcode = workflowCode,
            jobseq = jobseq,
            // Full-level snapshot (CEO, 2026-09-07 follow-up) — the rest
            // of wf_sub_workflow_master's columns, frozen alongside the
            // ones above rather than hand-picked. remark intentionally
            // NOT copied here — job_subworkflow_master.remark is written
            // by the engine itself at runtime (unrelated to the level
            // definition's own remark text).
            isAdhocUser = level.isAdhocUser,
            iscustomApprover = level.iscustomApprover,
            approvedstatus = level.approvedstatus,
            declinestatus = level.declinestatus,
            isReturnSender = level.isReturnSender,
            loacode = level.loacode,
            isAutoApproveAllow = level.isAutoApproveAllow,
            isNeedBudgetApproval = level.isNeedBudgetApproval,
            sitinstatus = level.sitinstatus,
            aa_id = level.aa_id,
            aa_level = level.aa_level,
            controller = level.controller,
            action = level.action,
            displayName = level.displayName,
            userid1 = level.userid1,
            userid2 = level.userid2,
            userid3 = level.userid3,
            subject = level.subject,
            subjectBiz = level.subjectBiz,
            describeBiz = level.describeBiz,
            describe = level.describe,
            actionEdit = level.actionEdit,
            subject_en = level.subject_en,
            subjectBiz_en = level.subjectBiz_en,
            describeBiz_en = level.describeBiz_en,
            describe_en = level.describe_en,
            isApproverSameOrg = level.isApproverSameOrg,
            isApproverSameCostCenter = level.isApproverSameCostCenter,
            isManualButton = level.isManualButton,
            ApproveController = level.ApproveController,
            ApproveAction = level.ApproveAction,
            moddate = DateTime.Now,
        };
    }

    // รอยเท้าล่าสุดของระดับนั้น (jobseq สูงสุด) — ใช้แทนการอ่าน "แถวของระดับ X"
    // เพราะตอนนี้หนึ่งระดับมีได้หลายแถว
    private static Task<job_subworkflow_master?> CurrentFootprintAsync(
        HRMContext context, long jobMasterId, int? wlevel, CancellationToken ct)
        => context.job_subworkflow_masters
            .Where(s => s.jobmasterid == jobMasterId && s.wlevel == wlevel)
            .OrderByDescending(s => s.jobseq).ThenByDescending(s => s.jobsubworkflowid)
            .FirstOrDefaultAsync(ct);

    public async Task ApproveAsync(long jobApproverId, long actorUserId, string? comment, long? reasonId = null, CancellationToken ct = default)
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
        await EnsureNotClaimedByAnotherAsync(context, job, approverRow.wlevel, actorUserId, ct);

        approverRow.jobstatus = StatusApproved;
        approverRow.approvedate = DateTime.Now;
        approverRow.comment = comment;
        approverRow.mas_reason_id = reasonId;
        // ทุก action ต้องตามไป update job_master ด้วย (CEO, 10 ก.ย. 2569) —
        // ต้นฉบับ epms Submit/RejectOneStep เขียน job.remark = model.reason ทุกครั้ง
        // job_master จึงบอกได้เสมอว่า "ล่าสุดเกิดอะไรขึ้นและด้วยเหตุผลอะไร"
        // โดยไม่ต้องไปไล่หาแถวล่าสุดใน job_user_list เอง
        job.remark = comment;
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

    public async Task RejectAsync(long jobApproverId, long actorUserId, string? comment, long? reasonId = null, CancellationToken ct = default)
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
        await EnsureNotClaimedByAnotherAsync(context, job, approverRow.wlevel, actorUserId, ct);

        approverRow.jobstatus = StatusRejected;
        approverRow.approvedate = DateTime.Now;
        approverRow.comment = comment;
        approverRow.mas_reason_id = reasonId;
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
        job.reasonClosed = ClosedByCancel;
        job.remark = reason;
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
    public async Task DeclineAsync(long jobApproverId, long actorUserId, string? comment, long? reasonId = null, CancellationToken ct = default)
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
        await EnsureNotClaimedByAnotherAsync(context, job, approverRow.wlevel, actorUserId, ct);

        var snapshot = await CurrentFootprintAsync(context, job.jobmasterid, approverRow.wlevel, ct)
            ?? throw new InvalidOperationException("ไม่พบ config ระดับนี้ของงานนี้");
        if (!snapshot.istop)
            throw new InvalidOperationException("\"ไม่อนุมัติ (Decline)\" ทำได้เฉพาะระดับสุดท้ายเท่านั้น — ระดับกลางที่ไม่เห็นด้วยให้ใช้ \"ส่งกลับแก้ไข\" แทน");

        approverRow.jobstatus = StatusRejected;
        approverRow.approvedate = DateTime.Now;
        approverRow.comment = comment;
        approverRow.mas_reason_id = reasonId;
        approverRow.isLast = false;

        job.status = StatusRejected; // terminal "ไม่อนุมัติ" — a clear declined state, not RETURNED (rework)
        job.isJobClosed = true;
        // reasonClosed = "การกระทำที่ปิดงาน" ไม่ใช่ข้อความเหตุผล — ตามต้นฉบับ epms
        // (job.reasonClosed = "Decline" / JobStatusService.approve ส่วนข้อความไป
        // job.remark) เดิมเราใส่ข้อความลงทั้งสองช่อง ทำให้อ่านจาก job_master ไม่ได้
        // ว่างานปิดเพราะอนุมัติหรือเพราะไม่อนุมัติ
        job.reasonClosed = ClosedByDecline;
        job.remark = comment;
        job.approvedDate = DateTime.Now;
        job.approvedUserID = actorUserId;
        job.approvedBy = (await context.sc_users
            .Where(u => u.userid == actorUserId).Select(u => u.loginname).FirstOrDefaultAsync(ct));
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
    public async Task SendBackAsync(long jobApproverId, long actorUserId, string? comment, long? reasonId = null, CancellationToken ct = default)
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
        await EnsureNotClaimedByAnotherAsync(context, job, approverRow.wlevel, actorUserId, ct);

        var snapshot = await CurrentFootprintAsync(context, job.jobmasterid, approverRow.wlevel, ct)
            ?? throw new InvalidOperationException("ไม่พบ config ระดับนี้ของงานนี้");
        // ปุ่ม "ส่งกลับ" = ถอยหลังหนึ่งขั้น (epms RejectOneStep: nextLevel = lastLevel - 1)
        // ถ้า config ตั้ง backwardlevel ไว้ว่าให้ย้อนไกลกว่านั้น ก็เดินตาม config
        var sendBackTo = snapshot.backwardlevel ?? snapshot.wlevel - 1;

        approverRow.jobstatus = StatusReturned;
        approverRow.approvedate = DateTime.Now;
        approverRow.comment = comment;
        approverRow.mas_reason_id = reasonId;
        job.remark = comment;   // ตามไป stamp job_master ทุก action (epms: job.remark = model.reason)

        // Route back exactly like an engine bounce (increments round, resets the
        // target level, issues a fresh approver round, flips isLast). Returns
        // false only for an invalid backwardlevel config — nothing persisted yet
        // in that case, so throwing leaves the context clean.
        var bounced = await TryBounceBackAsync(context, job, snapshot, ct, sendBackTo);
        if (!bounced)
            throw new InvalidOperationException("ส่งกลับไม่สำเร็จ — งานนี้ยังไม่มีประวัติผู้ส่งในขั้นก่อนหน้า (ขั้นนี้เป็นขั้นแรกของงาน)");
        await context.SaveChangesAsync(ct);

        await _auditLogger.LogChangeAsync(AuditActionType.Update, "job_user_list", jobApproverId.ToString(),
            new { jobstatus = StatusPending }, new { jobstatus = StatusReturned, action = "SendBack", toLevel = sendBackTo, comment }, isSensitive: false, ct);
        Serilog.Log.Information("Job {JobMasterId} SENT BACK from level {Level} to level {ToLevel} by user {ActorUserId}",
            job.jobmasterid, approverRow.wlevel, sendBackTo, actorUserId);

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

    // Admin-only (CEO, item 5, 2026-09-07): the deliberate counterpart to
    // AssignApproverAsync — that one refuses a row that already has an
    // approver (a safety rail against accidentally stealing someone's
    // pending item); this is the explicit "yes, move it anyway" action for
    // when an approver is on leave, transferred, or otherwise unreachable.
    // Requires a reason so there's always a paper trail for why a job moved
    // to a different person than the engine originally resolved.
    public async Task ReassignApproverAsync(long jobApproverId, long newUserId, string reason, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var row = await context.job_user_lists.FirstOrDefaultAsync(a => a.jobapproverid == jobApproverId, ct)
            ?? throw new InvalidOperationException($"ไม่พบรายการอนุมัติ id {jobApproverId}");
        if (row.userid is null)
            throw new InvalidOperationException("รายการนี้ยังไม่มีผู้อนุมัติ — ใช้หน้า \"ตำแหน่งว่าง\" แทน");
        if (!string.Equals(row.jobstatus, StatusPending, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("รายการนี้ถูกดำเนินการไปแล้ว ไม่สามารถเปลี่ยนผู้อนุมัติได้");
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("กรุณาระบุเหตุผลในการเปลี่ยนผู้อนุมัติ");

        var newAssignee = await context.sc_users.FirstOrDefaultAsync(u => u.userid == newUserId, ct)
            ?? throw new InvalidOperationException($"ไม่พบผู้ใช้ id {newUserId}");

        var oldUserId = row.userid;
        row.userid = newUserId;
        row.empid = newAssignee.empid;
        row.reason = string.IsNullOrWhiteSpace(row.reason) ? $"เปลี่ยนผู้อนุมัติโดย admin: {reason}" : $"{row.reason} | เปลี่ยนผู้อนุมัติโดย admin: {reason}";
        await context.SaveChangesAsync(ct);

        await _auditLogger.LogChangeAsync(AuditActionType.Update, "job_user_list", jobApproverId.ToString(),
            new { userid = oldUserId }, new { userid = newUserId, reason }, isSensitive: false, ct);
        Serilog.Log.Information("Job {JobMasterId} level {Level}: approver reassigned from user {OldUserId} to {NewUserId} ({Reason})",
            row.jobmasterid, row.wlevel, oldUserId, newUserId, reason);
    }

    // Passive expire (CEO, 2026-09-07 follow-up: "เอาแบบ Passive" — no
    // background job; computed lazily whenever an inbox/list page loads,
    // same apply-on-read pattern as SyncStatusFromJobAsync/every other
    // status sync in this codebase). DaysWaiting counts from the CURRENT
    // level's job_subworkflow_master.starttime (falls back to
    // job.createdate for a job whose level predates this feature, i.e.
    // starttime still null); IsOverdue compares that against the owning
    // workflow's wf_workflow.wexpireday — null/0 = no limit configured,
    // never flagged. Batched (one query for however many jobs the caller
    // passes) so a list page doesn't run N+1 queries per row.
    public record JobAgeInfo(int DaysWaiting, int? ExpireDays, bool IsOverdue);

    public async Task<Dictionary<long, JobAgeInfo>> GetJobAgesAsync(IEnumerable<job_master> jobs, CancellationToken ct = default)
    {
        var openJobs = jobs.Where(j => j.isJobClosed != true).ToList();
        var result = new Dictionary<long, JobAgeInfo>();
        if (openJobs.Count == 0) return result;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var jobIds = openJobs.Select(j => j.jobmasterid).ToList();
        var starts = await context.job_subworkflow_masters
            .Where(s => jobIds.Contains(s.jobmasterid))
            .Select(s => new { s.jobmasterid, s.wlevel, s.starttime })
            .ToListAsync(ct);
        // หนึ่งระดับมีได้หลายรอยเท้า (งานที่ตีกลับแล้ววนกลับมาระดับเดิม) —
        // นับอายุจากการมาถึง "ครั้งล่าสุด" ของระดับนั้น ไม่ใช่ครั้งแรก
        var startByJobLevel = starts
            .GroupBy(s => (s.jobmasterid, s.wlevel))
            .ToDictionary(g => g.Key, g => g.Max(s => s.starttime));

        foreach (var job in openJobs)
        {
            var levelStart = startByJobLevel.GetValueOrDefault((job.jobmasterid, job.lastLevel ?? 0)) ?? job.createdate;
            if (levelStart is null) continue;

            var days = (int)(DateTime.Now - levelStart.Value).TotalDays;
            var expireDays = job.workflow?.wexpireday;
            result[job.jobmasterid] = new JobAgeInfo(days, expireDays, expireDays is int ed && ed > 0 && days > ed);
        }
        return result;
    }

    public async Task<List<job_user_list>> GetMyInboxAsync(long userId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        // ใบที่ยังรอทำจริง = IsLiveApprovalRow (ดูคำอธิบายกฎที่ประกาศไว้ด้านบนไฟล์)
        return await context.job_user_lists
            .Include(a => a.jobmaster).ThenInclude(j => j.workflow)
            .Where(IsLiveApprovalRow)
            .Where(a => a.userid == userId)
            .OrderBy(a => a.jobmaster.createdate)
            .ToListAsync(ct);
    }

    // Pool Workflow (CEO, 2026-09-07): "งานที่เป็นของแผนกที่ต้องช่วยกันเอาไป
    // อนุมัติ" — a level marked isPool=true on the job's own snapshot still
    // resolves candidates and completes exactly like any other level
    // (normally isorcondition, so whoever acts first wins — isPool changes
    // no completion logic at all); this only adds a SECOND, shared view of
    // the same pending rows so a team can see everything currently up for
    // grabs in one place instead of each person only noticing it in their
    // own personal inbox. Same PENDING filter as GetMyInboxAsync, narrowed
    // to isPool levels via the job's own frozen snapshot.
    //
    // Claim-lock follow-up (CEO, 2026-09-07): each row also reports whether
    // ITS job is currently claimed, and by whom — job_master.PoolClaimedByUserId
    // is per-JOB (not per-candidate-row), since exactly one claim can be live
    // for a job's current level regardless of how many pool candidates exist.
    public record PoolInboxRow(job_user_list Row, bool IsClaimedByMe, string? ClaimedByName, DateTime? ClaimedDate);

    public async Task<List<PoolInboxRow>> GetMyPoolInboxAsync(long userId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        // pool คือขั้นที่มีผู้อนุมัติหลายคนโดยนิยาม จุดนี้จึงเป็นจุดที่การกรองแค่
        // PENDING เพี้ยนหนักที่สุด — พอเพื่อนร่วมทีมคนหนึ่งรับงานไปทำจนงานเดินต่อ
        // ใบของคนที่เหลือยังค้าง PENDING งานที่ทำไปแล้วก็จะโชว์ใน pool ตลอดไป
        var pending = await context.job_user_lists
            .Include(a => a.jobmaster).ThenInclude(j => j.workflow)
            .Where(IsLiveApprovalRow)
            .Where(a => a.userid == userId)
            .ToListAsync(ct);
        if (pending.Count == 0) return new();

        var jobMasterIds = pending.Select(a => a.jobmasterid).Distinct().ToList();
        var poolSnapshots = await context.job_subworkflow_masters
            .Where(s => jobMasterIds.Contains(s.jobmasterid) && s.isPool)
            .Select(s => new { s.jobmasterid, s.wlevel })
            .ToListAsync(ct);
        var poolKeys = poolSnapshots.Select(s => (s.jobmasterid, s.wlevel)).ToHashSet();

        var poolRows = pending.Where(a => poolKeys.Contains((a.jobmasterid, a.wlevel ?? 0)))
            .OrderBy(a => a.jobmaster.createdate)
            .ToList();
        if (poolRows.Count == 0) return new();

        var claimantIds = poolRows
            .Where(a => IsPoolClaimLive(a.jobmaster))
            .Select(a => a.jobmaster.PoolClaimedByUserId!.Value)
            .Distinct().ToList();
        var claimantNames = await context.sc_users
            .Where(u => claimantIds.Contains(u.userid))
            .ToDictionaryAsync(u => u.userid, u => $"{u.firstname} {u.lastname}", ct);

        return poolRows.Select(a =>
        {
            var live = IsPoolClaimLive(a.jobmaster);
            var claimedBy = live ? a.jobmaster.PoolClaimedByUserId : null;
            return new PoolInboxRow(
                a,
                IsClaimedByMe: claimedBy == userId,
                ClaimedByName: claimedBy is long id ? claimantNames.GetValueOrDefault(id, $"#{id}") : null,
                ClaimedDate: live ? a.jobmaster.PoolClaimedDate : null);
        }).ToList();
    }

    // A claim only counts as "live" while it still points at the job's
    // CURRENT level/round — job_master doesn't get a fresh row per level or
    // bounce-back round, so without this check a claim from an earlier,
    // already-completed round would incorrectly keep blocking a later one.
    private static bool IsPoolClaimLive(job_master job) =>
        job.PoolClaimedByUserId is not null
        && job.PoolClaimedWLevel == job.lastLevel
        && (job.PoolClaimedJobSeq ?? 0) == (job.jobseq ?? 0);

    // Enforcement side of the claim-lock (ApproveAsync/RejectAsync/DeclineAsync/
    // SendBackAsync all call this) — only blocks when the level being acted on
    // is isPool==true AND someone OTHER than the actor holds a live claim.
    // A non-pool level, an unclaimed pool level, or the actor's own claim are
    // all no-ops here — zero behavior change for any workflow that never
    // uses isPool.
    private async Task EnsureNotClaimedByAnotherAsync(HRMContext context, job_master job, int? wlevel, long actorUserId, CancellationToken ct)
    {
        if (!IsPoolClaimLive(job) || job.PoolClaimedByUserId == actorUserId) return;

        var levelSnapshot = await CurrentFootprintAsync(context, job.jobmasterid, wlevel, ct);
        if (levelSnapshot?.isPool != true) return;

        throw new InvalidOperationException("งานนี้มีเพื่อนร่วมทีมรับไปดำเนินการแล้ว กรุณาเลือกงานอื่นในพูล หรือรอให้เขาปล่อยคืน");
    }

    // Claims a pool job for the caller so teammates see it's already being
    // worked (WfPoolInbox.razor's "รับงาน" button) — idempotent if the caller
    // already holds the live claim, throws if someone else does or if the
    // caller isn't actually a pending candidate on this job's current level.
    public async Task ClaimPoolJobAsync(long jobMasterId, long actorUserId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var job = await context.job_masters.FirstOrDefaultAsync(j => j.jobmasterid == jobMasterId, ct)
            ?? throw new InvalidOperationException("ไม่พบงานนี้แล้ว");
        if (job.isJobClosed == true)
            throw new InvalidOperationException("งานนี้ปิดแล้ว ไม่สามารถรับงานได้");

        if (IsPoolClaimLive(job))
        {
            if (job.PoolClaimedByUserId == actorUserId) return; // already claimed by me — no-op
            throw new InvalidOperationException("มีเพื่อนร่วมทีมรับงานนี้ไปแล้ว");
        }

        var myPendingRow = await context.job_user_lists.FirstOrDefaultAsync(a =>
            a.jobmasterid == jobMasterId && a.userid == actorUserId && a.jobstatus == StatusPending
            && a.wlevel == job.lastLevel && (a.jobseq ?? 0) == (job.jobseq ?? 0), ct)
            ?? throw new InvalidOperationException("คุณไม่ใช่ผู้ได้รับมอบหมายในระดับปัจจุบันของงานนี้");

        var levelSnapshot = await CurrentFootprintAsync(context, jobMasterId, myPendingRow.wlevel, ct);
        if (levelSnapshot?.isPool != true)
            throw new InvalidOperationException("ระดับนี้ไม่ใช่ pool workflow — ไม่ต้องรับงาน สามารถอนุมัติได้ทันที");

        job.PoolClaimedByUserId = actorUserId;
        job.PoolClaimedWLevel = job.lastLevel;
        job.PoolClaimedJobSeq = job.jobseq;
        job.PoolClaimedDate = DateTime.Now;
        await context.SaveChangesAsync(ct);

        await _auditLogger.LogChangeAsync(AuditActionType.Update, "job_master", jobMasterId.ToString(),
            new { PoolClaimedByUserId = (long?)null }, new { PoolClaimedByUserId = actorUserId }, isSensitive: false, ct);
    }

    // Lets the claimant give a pool job back (e.g. they realize they can't
    // handle it) — only the current claimant may release; a no-op if nobody
    // (or someone else, via a stale/foreign call) holds it.
    public async Task ReleasePoolClaimAsync(long jobMasterId, long actorUserId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var job = await context.job_masters.FirstOrDefaultAsync(j => j.jobmasterid == jobMasterId, ct)
            ?? throw new InvalidOperationException("ไม่พบงานนี้แล้ว");
        if (!IsPoolClaimLive(job)) return;
        if (job.PoolClaimedByUserId != actorUserId)
            throw new InvalidOperationException("คุณไม่ใช่ผู้ที่รับงานนี้ไว้ ไม่สามารถปล่อยคืนได้");

        job.PoolClaimedByUserId = null;
        job.PoolClaimedWLevel = null;
        job.PoolClaimedJobSeq = null;
        job.PoolClaimedDate = null;
        await context.SaveChangesAsync(ct);

        await _auditLogger.LogChangeAsync(AuditActionType.Update, "job_master", jobMasterId.ToString(),
            new { PoolClaimedByUserId = actorUserId }, new { PoolClaimedByUserId = (long?)null }, isSensitive: false, ct);
    }

    // "คนที่เกี่ยวข้องในการอนุมัติต้องเห็นงานด้วยว่าเขาทำไปแล้วถึงไหนแล้ว"
    // (CEO, 2026-09-07) — every job_user_list row this user has ever been on,
    // any status, newest first: what's still pending (their turn or not) plus
    // what they already approved/rejected/sent back. GetMyInboxAsync only
    // ever returns the PENDING slice; this is the full involvement history.
    public async Task<List<job_user_list>> GetMyInvolvementAsync(long userId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.job_user_lists
            .Include(a => a.jobmaster).ThenInclude(j => j.workflow)
            .Where(a => a.userid == userId)
            .OrderByDescending(a => a.jobmaster.createdate)
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
            .Where(IsLiveApprovalRow)
            .Where(a => a.userid == null)
            .OrderBy(a => a.jobmaster.createdate)
            .ToListAsync(ct);
    }

    // Requester-side "งานของฉัน" (CEO, 2026-09-07, REQ: approval inbox gap #1)
    // — every job the caller started, across every module, newest first.
    // Deliberately reads job_master directly (no per-user_list join): the
    // requester relationship is createuserid, not a job_user_list row, so
    // this returns jobs even before any approver has been assigned.
    public async Task<List<job_master>> GetMyRequestsAsync(long requesterUserId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.job_masters
            .Include(j => j.workflow)
            .Where(j => j.createuserid == requesterUserId)
            .OrderByDescending(j => j.createdate)
            .ToListAsync(ct);
    }

    // Admin "ภาพรวมงานอนุมัติ" (CEO, 2026-09-07, REQ: approval inbox gap #2)
    // — every job across every workflow/module, with the handful of filters
    // job_master already carries columns for. All filters are optional and
    // AND together; a blank filter set returns everything, newest first.
    public async Task<List<job_master>> SearchJobsAsync(string? workflowCode = null, bool? isClosed = null,
        long? requesterUserId = null, DateTime? fromDate = null, DateTime? toDate = null, string? searchText = null,
        IEnumerable<long>? requesterUserIds = null, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var query = context.job_masters.Include(j => j.workflow).AsQueryable();

        if (!string.IsNullOrWhiteSpace(workflowCode)) query = query.Where(j => j.workflowcode == workflowCode);
        if (isClosed is bool closed) query = query.Where(j => j.isJobClosed == closed);
        if (requesterUserId is long uid) query = query.Where(j => j.createuserid == uid);
        // Supervisor-scoped overview (CEO, 2026-09-07): "หัวหน้าต้องดูงาน
        // แผนกที่ตัวเองดูแลได้ทั้งหมด" — every job created by anyone in the
        // caller's own team/subtree (self included), computed by the caller
        // (see /wf/team-jobs) since org-subtree resolution lives in
        // Services/Shared, not this engine.
        if (requesterUserIds is not null)
        {
            var idSet = requesterUserIds as ICollection<long> ?? requesterUserIds.ToList();
            query = query.Where(j => j.createuserid != null && idSet.Contains(j.createuserid.Value));
        }
        if (fromDate is DateTime from) query = query.Where(j => j.createdate >= from);
        if (toDate is DateTime to) query = query.Where(j => j.createdate < to.AddDays(1));
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var term = searchText.Trim();
            query = query.Where(j => (j.subject != null && j.subject.Contains(term))
                || (j.createusername != null && j.createusername.Contains(term))
                || (j.reqName != null && j.reqName.Contains(term))
                || (j.wname != null && j.wname.Contains(term))
                || (j.status != null && j.status.Contains(term)));
        }

        return await query.OrderByDescending(j => j.createdate).Take(500).ToListAsync(ct);
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
    // ทิศทางการเดินของงาน — ตรงกับพารามิเตอร์ waction ของ
    // WorkFlowChain.updateWorkFlowChain() ในระบบต้นฉบับ
    // การขยับของงาน มีสามแบบเท่านั้น (CEO, 9 ก.ย. 2569):
    //   Forward  = เดินหน้า  -> wlevel + 1
    //   Backward = ถอยหลัง   -> wlevel - 1
    //   Stand    = อยู่ที่เดิม -> wlevel เท่าเดิม ไม่ขยับ (เช่นไต่ผังองค์กรอีกขั้น
    //              ที่ระดับเดียวกัน หรือขั้นที่ต้องวนขอความเห็นเพิ่ม)
    // ตรงกับพารามิเตอร์ waction ของ WorkFlowChain.updateWorkFlowChain() ต้นฉบับ
    private enum StepDirection { Forward, Backward, Stand }

    // ==================== หนึ่งก้าวของงาน ====================
    //
    // CEO, 9 ก.ย. 2569: "workflow คือชื่อ workflow, subworkflow คือเส้นทางเดิน
    // แต่ละ level config ว่าอะไรก็ไปหามาเตรียมไว้ ... แค่อ่านแล้ว stamp
    // คุณไม่ต้องคิด คุณแค่ทำ method ไว้แล้วส่งค่าผ่านไปกลับแต่ละ level"
    //
    // นี่คือเมธอดนั้น — จุดเดียวที่งานเคลื่อนที่ ทั้งเดินหน้าและถอยหลัง
    // เทียบบรรทัดต่อบรรทัดกับ WorkFlowChain.updateWorkFlowChain() ของต้นฉบับ:
    //
    //   1. อ่านสคริปต์ของ level ปลายทางจาก wf_sub_workflow_master (อ่านสด
    //      ไม่ใช่จากรอยเท้าเก่า — คนใน role/ตำแหน่งเปลี่ยนได้ระหว่างงานยังเดินอยู่)
    //   2. ถอยหลัง -> ผู้รับคือคนที่เป็นคนส่งใน level นั้น (ไม่ต้องตีความอะไร)
    //   3. นับก้าว (jobseq) แล้วย้ายงานมาที่ level นี้ — stamp ลง job_master
    //   4. ประทับรอยเท้าลง job_subworkflow_master หนึ่งแถวต่อก้าว
    //   5. ถอยหลัง -> ออกใบงานให้คนนั้น + stamp backwardstatus ของ level
    //   6. เดินหน้า -> resolve ผู้เกี่ยวข้องตามที่ config ไว้ใน level นั้น
    //                 (AssignLevelApproversAsync stamp standstatus ให้เอง)
    //
    // เมธอดนี้ไม่ตัดสินใจอะไรเอง และไม่ตีความความหมายของ config ด้วย — config
    // มีค่าอะไรก็ทำงานของมันไป เครื่องแค่สร้างกระบวนการให้มันเดิน
    // คืนค่า null = ก้าวนี้เดินไม่ได้ (ไม่มี level ปลายทาง หรือส่งกลับไปยัง level
    // ที่งานไม่เคยผ่าน) ให้ผู้เรียกตัดสินใจต่อเอง
    private async Task<WorkflowOutcome?> StepAsync(
        HRMContext context, job_master job, int wlevel,
        StepDirection direction, CancellationToken ct)
    {
        // 1. อ่านสคริปต์ของ level นี้
        var level = await context.wf_sub_workflow_masters
            .FirstOrDefaultAsync(s => s.workflowid == job.workflowid && s.wlevel == wlevel, ct);
        if (level is null)
            return null;

        // 2. ถอยหลัง — ผู้รับคือ "คนที่เป็นคนส่งใน level นั้น" ตรง ๆ
        //    (CEO, 9 ก.ย. 2569) ไม่ตีความ flag ใด ๆ ไม่ resolve ผู้อนุมัติใหม่
        //    ตรงกับต้นฉบับทั้งสองรุ่น: JSP WorkFlowChain ที่ waction="backward"
        //    ใช้ lastuser ตรง ๆ และ epms RejectOneStep ที่วนถอยลงทีละขั้นจนกว่า
        //    จะเจอคนที่เคยส่ง — สุดทางคือ level 0 = ผู้ขอ จึงส่งกลับถึงผู้ขอได้เอง
        //    โดยไม่ต้องมี flag พิเศษอะไรเลย
        //
        //    หาให้ได้ก่อนจะแตะ job/รอยเท้า เพื่อให้ผู้เรียกล้มกลับไปทาง FailJob ได้สะอาด
        long? recipientUserId = null;
        string? recipientEmpId = null;
        if (direction == StepDirection.Backward)
        {
            var searchLevel = wlevel;
            job_user_list? sender = null;
            while (searchLevel >= 0)
            {
                sender = await context.job_user_lists
                    .Where(a => a.jobmasterid == job.jobmasterid && a.wlevel == searchLevel && a.userid != null)
                    .OrderByDescending(a => a.jobseq).ThenByDescending(a => a.jobapproverid)
                    .FirstOrDefaultAsync(ct);
                if (sender is not null) break;
                searchLevel--;   // ไม่มีใครเคยอยู่ขั้นนี้ -> ถอยลงอีกขั้นไปหาคนที่ส่ง
            }
            if (sender is null)
                return null; // งานไม่เคยผ่านขั้นไหนเลย -> ส่งกลับไม่ได้

            recipientUserId = sender.userid;
            recipientEmpId = sender.empid;

            // ถ้าวนถอยไปเจอที่ขั้นอื่น ปลายทางจริงคือขั้นนั้น ต้องอ่าน config ใหม่
            if (searchLevel != wlevel)
            {
                wlevel = searchLevel;
                level = await context.wf_sub_workflow_masters
                    .FirstOrDefaultAsync(s => s.workflowid == job.workflowid && s.wlevel == wlevel, ct);
                if (level is null) return null;
            }
        }

        // 3. นับก้าว แล้วย้ายงานมาที่ level นี้
        //    jobseq (CEO, 2026-09-07, ยืนยันกับข้อมูลจริงของ epms): นับ "ทุก" การ
        //    ขยับ ทั้งเดินหน้า ถอยหลัง และอยู่ที่เดิม ไม่ใช่เฉพาะตอนตีกลับ
        job.jobseq = (job.jobseq ?? 0) + 1;
        job.lastLevel = wlevel;

        // 4. ประทับรอยเท้าของก้าวนี้ — หนึ่งแถวต่อการมาถึงหนึ่งครั้ง งานที่วน
        //    1 -> 0 -> 1 จึงเก็บครบทุกครั้ง ไม่ใช่เขียนทับแถวเดียวของ level นั้น
        //    (TTMEPMS job 229 มี 7 แถวจาก 4 ระดับด้วยเหตุนี้)
        context.job_subworkflow_masters.Add(BuildLevelSnapshot(job, level, job.workflowcode, job.jobseq));

        // 5. stamp สถานะว่า "งานมานั่งอยู่ที่ขั้นนี้แล้ว" — sitinstatus ก่อน
        //    ถ้าไม่ได้ตั้งไว้ก็ standstatus (CEO: "ถ้า sit in ก็งานมาอยู่ที่ level นี้
        //    หรือ stand ก็ได้") ขากลับใช้ backwardstatus เพราะเป็นผลของการถอย
        job.status = direction == StepDirection.Backward
            ? level.backwardstatus ?? level.sitinstatus ?? level.standstatus ?? StatusRejected
            : level.sitinstatus ?? level.standstatus ?? StatusPending;

        // 6. ถอยหลัง — ออกใบงานให้คนที่ส่งมา ไม่ resolve ผู้อนุมัติใหม่
        if (direction == StepDirection.Backward)
        {
            await IssueApproverRowAsync(context, job, level, recipientUserId!.Value, recipientEmpId, ct);
            return WorkflowOutcome.StillOpen;
        }

        // 7. เดินหน้า/อยู่ที่เดิม — resolve ผู้เกี่ยวข้องตาม config ของขั้นนี้
        return await AssignLevelApproversAsync(context, job, level, ct);
    }

    // สามประตูของกระบวนการ ตามที่ CEO อธิบายไว้ (9 ก.ย. 2569):
    //   กด "ส่งต่อ"    -> Forward()   ไป wlevel + 1  ใครเกี่ยวข้อง ดูที่ config ของขั้นนั้น
    //   กด "ส่งกลับ"   -> SendBack()  ไป wlevel - 1  ส่งให้คนที่เป็นคนส่งในขั้นนั้น
    //   ยังไม่ขยับ      -> Stand()     อยู่ wlevel เดิม  วนขอความเห็นเพิ่มที่ขั้นเดียวกัน
    //
    // ไม่ส่ง toLevel มาก็คำนวณจาก job.lastLevel ให้เอง (+1 / -1 / เท่าเดิม) — ส่งมา
    // เมื่อเส้นทางไม่ใช่ทีละขั้น เช่น isLOA กระโดดตามวงเงิน หรือ backwardlevel
    // ที่ config ให้ย้อนหลายขั้น เมธอดพวกนี้ไม่ตัดสินใจเองว่าไปขั้นไหน
    private Task<WorkflowOutcome?> ForwardAsync(HRMContext context, job_master job, CancellationToken ct, int? toLevel = null)
        => StepAsync(context, job, toLevel ?? (job.lastLevel ?? 0) + 1, StepDirection.Forward, ct);

    private Task<WorkflowOutcome?> SendBackAsync(HRMContext context, job_master job, CancellationToken ct, int? toLevel = null)
        => StepAsync(context, job, toLevel ?? (job.lastLevel ?? 0) - 1, StepDirection.Backward, ct);

    private Task<WorkflowOutcome?> StandAsync(HRMContext context, job_master job, CancellationToken ct)
        => StepAsync(context, job, job.lastLevel ?? 0, StepDirection.Stand, ct);

    // ออกใบงานให้ผู้รับหนึ่งคน (ใช้ตอนถอยหลัง ซึ่งผู้รับถูกกำหนดมาแล้ว
    // ไม่ต้อง resolve) — รูปแบบแถวเดียวกับที่ AssignLevelApproversAsync ออกให้
    private async Task IssueApproverRowAsync(
        HRMContext context, job_master job, wf_sub_workflow_master level,
        long userId, string? empId, CancellationToken ct)
    {
        var prior = await context.job_user_lists
            .Where(a => a.jobmasterid == job.jobmasterid && a.isLast == true)
            .ToListAsync(ct);
        foreach (var r in prior) r.isLast = false;

        var name = await context.sc_users.Where(u => u.userid == userId)
            .Select(u => (u.firstname + " " + u.lastname).Trim())
            .FirstOrDefaultAsync(ct);

        context.job_user_lists.Add(new job_user_list
        {
            jobmasterid = job.jobmasterid,
            workflowid = job.workflowid,
            wlevel = level.wlevel,
            userid = userId,
            empid = empId,
            username = string.IsNullOrWhiteSpace(name) ? null : name,
            subworkflowmasterid = level.subworkflowid,
            jobstatus = StatusPending,
            sendDate = DateTime.Now,
            jobseq = job.jobseq,
            isLast = true,
        });
        await NotifyApproverAsync(context, job, userId, empId, ct);
    }

    private async Task<WorkflowOutcome> TryAdvanceLevelAsync(HRMContext context, job_master job, int completedLevel, long? actorUserId, CancellationToken ct)
    {
        var completedSnapshot = await CurrentFootprintAsync(context, job.jobmasterid, completedLevel, ct)
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
            // This level's involvement ends here either way (bounced away or
            // the job dies here) — stamp endtime before either branch.
            completedSnapshot.endtime = DateTime.Now;
            var rejectComment = realRoundRows.LastOrDefault(r => string.Equals(r.jobstatus, StatusRejected, StringComparison.OrdinalIgnoreCase))?.comment;
            if (await TryBounceBackAsync(context, job, completedSnapshot, ct))
                return WorkflowOutcome.BouncedBack;
            FailJob(job, completedSnapshot, rejectComment);
            return WorkflowOutcome.Rejected;
        }

        // Self-terminating vertical chain (CEO, 2026-09-07): a completed hop
        // that ISN'T this job's terminal one stays on the SAME wlevel for
        // the next hop — never advances to wlevel+1 like an ordinary level,
        // since the "next level" here is just another climb on this same
        // config. AssignVerticalChainHopAsync re-derives the hop number from
        // job_user_list rows itself, so calling it again is exactly correct.
        if (completedSnapshot.verticalMaxLevel is int maxVerticalLevel && !completedSnapshot.istop)
        {
            var liveLevelForChain = await context.wf_sub_workflow_masters
                .FirstOrDefaultAsync(s => s.workflowid == job.workflowid && s.wlevel == completedLevel, ct)
                ?? throw new InvalidOperationException($"ไม่พบ config ต้นฉบับของระดับ {completedLevel} ใน wf_sub_workflow_master แล้ว (อาจถูกลบหลังงานเริ่ม)");
            return await AssignVerticalChainHopAsync(context, job, liveLevelForChain, maxVerticalLevel, ct);
        }

        // Fixed-count org-chart climb (empLevel) — mirrors the vertical-chain
        // re-entry above, but unlike that mechanism this one does NOT force
        // istop: it re-enters for one more hop only while the just-approved
        // hop's own reason text (stamped at issue time by
        // AssignEmpLevelClimbAsync) says it wasn't the terminal one. Once the
        // terminal hop resolves, execution simply falls through to the
        // ordinary istop/next-level logic below — exactly as if this had
        // been any other level's own approver resolving.
        if (completedSnapshot.empLevel is int climbHops && climbHops > 0 && completedSnapshot.verticalMaxLevel is null)
        {
            var lastClimbRow = realRoundRows
                .Where(r => r.reason != null && r.reason.StartsWith(EmpLevelClimbMarker, StringComparison.Ordinal))
                .OrderByDescending(r => r.jobapproverid)
                .FirstOrDefault();
            var wasTerminalHop = lastClimbRow?.reason?.Contains("ระดับสุดท้าย") ?? true; // no climb row at all -> treat as done, don't loop forever
            if (!wasTerminalHop)
            {
                var liveLevelForClimb = await context.wf_sub_workflow_masters
                    .FirstOrDefaultAsync(s => s.workflowid == job.workflowid && s.wlevel == completedLevel, ct)
                    ?? throw new InvalidOperationException($"ไม่พบ config ต้นฉบับของระดับ {completedLevel} ใน wf_sub_workflow_master แล้ว (อาจถูกลบหลังงานเริ่ม)");
                return await AssignEmpLevelClimbAsync(context, job, liveLevelForClimb, climbHops, ct);
            }
            // Terminal hop just resolved — fall through below exactly like
            // an ordinary level whose own approver just acted.
        }

        // outcome == Complete. Decided by istop (the level explicitly
        // marked as terminal), not by comparing to job.maxlevel — that
        // count-based check was a latent bug from Block 2/3 (see Block 4
        // notes in the plan file): LOA branching can jump straight past
        // several levels, so a count-based check would be flatly wrong.
        if (completedSnapshot.istop)
        {
            completedSnapshot.endtime = DateTime.Now;
            // Block 9: display status becomes the level's configured
            // "approved" text instead of a hardcoded engine constant.
            job.status = completedSnapshot.forwardstatus ?? StatusCompleted;
            job.isJobClosed = true;
            // ครบชุดตามต้นฉบับ epms Approve: status / isJobClosed / reasonClosed /
            // approvedDate / approvedUserID / approvedBy — เดิมขาด reasonClosed,
            // approvedBy และ enddate ทำให้ job ที่ปิดแล้วไม่มีวันจบและไม่รู้ว่าใครปิด
            job.reasonClosed = ClosedByApprove;
            job.approvedDate = DateTime.Now;
            job.approvedUserID = actorUserId;
            job.approvedBy = await context.sc_users
                .Where(u => u.userid == actorUserId).Select(u => u.loginname).FirstOrDefaultAsync(ct);
            job.enddate = DateTime.Now;
            Serilog.Log.Information("Job {JobMasterId} closed: Approved at level {Level}", job.jobmasterid, completedLevel);
            return WorkflowOutcome.Approved;
        }

        var nextLevelNo = completedSnapshot.isLOA
            ? await ResolveNextLevelViaLoaAsync(context, job, completedLevel, ct)
            : completedLevel + 1;

        completedSnapshot.endtime = DateTime.Now; // this level is done, advancing onward

        // ส่งต่อไป level ถัดไป — ทุกอย่างของก้าวนั้น (นับ jobseq, ย้าย level,
        // ประทับรอยเท้า, อ่าน config หาผู้เกี่ยวข้อง) อยู่ใน StepAsync ที่เดียว
        return await ForwardAsync(context, job, ct, nextLevelNo)
            ?? throw new InvalidOperationException(
                $"ไม่พบ config ของระดับ {nextLevelNo} ใน wf_sub_workflow_master — ไม่สามารถเดินงานต่อได้");
    }

    // Terminates the job as failed/returned — used both for ordinary
    // single-reject failures and for an AND-condition level whose remaining
    // approval weight can no longer reach threshold. Block 9: display
    // status becomes the level's configured "returned/rejected" text.
    private static void FailJob(job_master job, job_subworkflow_master completedSnapshot, string? comment)
    {
        job.status = completedSnapshot.backwardstatus ?? StatusRejected;
        job.isJobClosed = true;
        job.reasonClosed = ClosedByDecline;
        job.remark = comment;
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
    // explicitBackwardLevel: ปุ่ม "ส่งกลับ" ที่คนกดเอง ส่งขั้นปลายทางมาตรง ๆ
    // (ปกติ wlevel - 1 ตาม epms RejectOneStep) ส่วนการตีกลับอัตโนมัติตอนถูก
    // ปฏิเสธยังต้องอาศัย backwardlevel ที่ config ไว้เท่านั้น ไม่งั้นทุกการปฏิเสธ
    // จะกลายเป็นตีกลับหมด
    private async Task<bool> TryBounceBackAsync(HRMContext context, job_master job, job_subworkflow_master completedSnapshot, CancellationToken ct, int? explicitBackwardLevel = null)
    {
        var backwardLevel = explicitBackwardLevel ?? completedSnapshot.backwardlevel;
        if (backwardLevel is null)
            return false; // not configured -> zero extra queries, fall through to FailJob

        // Don't trust config blindly: must be a real earlier level, never
        // forward/self (which would be a no-op-forever or same-level loop).
        // ระดับ 0 คือขั้นของผู้ขอในระบบต้นฉบับ (TTMEPMS wf3 = Requistioner,
        // wf8 = Supplier Registration) การตีกลับถึงผู้ขอเป็นเส้นทางปกติ —
        // job 229 ทำแบบนั้น 2 รอบ เดิม HRM บล็อกไว้ที่ < 1 จึงตีกลับถึงผู้ขอไม่ได้เลย
        if (backwardLevel < 0 || backwardLevel >= completedSnapshot.wlevel)
            return false;

        // Defensive cap: if two levels are misconfigured to bounce back and
        // forth at each other, a human still has to keep rejecting/approving
        // every round for it to continue (nothing here loops automatically),
        // but cap it anyway so a bad config can't be ridden forever. jobseq
        // now counts forward advances too (CEO, 2026-09-07 follow-up, epms
        // parity), so a flat 20 would wrongly cap a long-but-legitimate
        // workflow that simply has many real levels — scale the cap against
        // maxlevel instead, floored at 20 so a short workflow still gets a
        // real limit.
        if ((job.jobseq ?? 0) >= Math.Max(20, (job.maxlevel ?? 0) * 4))
            return false;

        // ส่งกลับ — เมธอดเดียวกับขาส่งต่อ ต่างกันแค่ทิศทางที่ส่งเข้าไป
        var stepped = await SendBackAsync(context, job, ct, backwardLevel.Value);
        if (stepped is null)
            return false; // ไม่มี level ปลายทาง หรืองานไม่เคยผ่าน level นั้น

        job.remark = (string.IsNullOrEmpty(job.remark) ? "" : job.remark + "\n")
            + $"{DateTime.Now:yyyy-MM-dd HH:mm}: ตีกลับจากระดับ {completedSnapshot.wlevel} ไปยังระดับ {backwardLevel.Value} (รอบที่ {job.jobseq})";

        Serilog.Log.Information("Job {JobMasterId} bounced back from level {FromLevel} to level {ToLevel} (round {Jobseq})",
            job.jobmasterid, completedSnapshot.wlevel, backwardLevel.Value, job.jobseq);

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
    //
    // excludeUserId (CEO, 2026-09-07, after the JSP-study mandate): a
    // requester can genuinely resolve as their own org's approver — the
    // legacy epms production data proves this actually happened in real use
    // (job 228 and 7 other real historical jobs self-approved at wlevel
    // 2/4). The JSP system's own equivalent walker (WorkflowOrgVertical.
    // getBoss) explicitly guards this: if the resolved boss is the requester
    // themselves, climb one more org level instead of self-approving. Same
    // fix here — a self-match is treated exactly like a vacancy for the
    // purposes of this loop (climb if allowSkipVacant, otherwise fail
    // through to vacant like any other unresolved hop).
    private static async Task<List<(long UserId, string? EmpId)>> ResolveOrgChainApproverAsync(
        HRMContext context, string? anchorOrgCode, bool allowSkipVacant, int maxHops, long? excludeUserId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(anchorOrgCode))
            return new();

        var org = await context.com_organizations.FirstOrDefaultAsync(o => o.code == anchorOrgCode || o.orgCode == anchorOrgCode, ct);
        for (var hop = 1; org is not null && hop <= maxHops; hop++)
        {
            if (allowSkipVacant || hop == maxHops)
            {
                if (org.approver_userid is long approverUserId && approverUserId != excludeUserId)
                    return new() { (approverUserId, null) };

                if (!string.IsNullOrWhiteSpace(org.approver_empid))
                {
                    var approverUser = await context.sc_users.FirstOrDefaultAsync(u => u.empid == org.approver_empid, ct);
                    if (approverUser is not null && approverUser.userid != excludeUserId)
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
        // สถานะ "งานนั่งอยู่ที่ขั้นนี้" — sitinstatus ก่อน ไม่มีค่อยใช้ standstatus
        // (CEO, 9 ก.ย. 2569) เดิม HRM อ่านแค่ standstatus คอลัมน์ sitinstatus
        // มีอยู่แต่ไม่เคยถูกอ่านเลย — epms ใช้ตัวนี้ทั้ง job.status และรอยเท้า
        job.status = level.sitinstatus ?? level.standstatus ?? StatusPending;

        // Per-level duration tracking (CEO, 2026-09-07 follow-up): stamp
        // starttime the FIRST time this level's round is issued only — a
        // Mix Approval pre-check hop re-entering this same method for the
        // same wlevel must not reset it, so ??= rather than an
        // unconditional assignment.
        var levelSnapshot = await CurrentFootprintAsync(context, job.jobmasterid, level.wlevel, ct);
        if (levelSnapshot is not null) levelSnapshot.starttime ??= DateTime.Now;

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

        // Self-terminating vertical chain (CEO, 2026-09-07) — checked before
        // the ordinary Mix Approval hop gate below; the two are mutually
        // exclusive configurations on the same level (this one IS the whole
        // approval, not a gate before a separate one).
        if (level.verticalMaxLevel is int maxVerticalLevel && maxVerticalLevel > 0)
            return await AssignVerticalChainHopAsync(context, job, level, maxVerticalLevel, ct);

        // Fixed-count org-chart climb (CEO, 2026-09-07 follow-up, empLevel):
        // "พนักงานเริ่ม job อยู่ระดับ 17, subworkflow บอกวิ่ง 3 ระดับ ->
        // 17->16->15->14 แล้วจบ ไปวิ่ง subworkflow ถัดไป" — climbs EXACTLY
        // empLevel hops (adaptively landing early only if the org chart
        // itself runs out first, same landing logic as verticalMaxLevel's
        // ResolveVerticalChainHopAsync), but — unlike verticalMaxLevel —
        // never forces istop: once the climb finishes, this level completes
        // NORMALLY and falls through to whatever level/config comes next
        // (e.g. a role-based HR step), exactly like an ordinary level would
        // after its own approver resolves. Mutually exclusive with
        // verticalMaxLevel (checked above) on the same level.
        if (level.empLevel is int climbHops && climbHops > 0 && level.verticalMaxLevel is null)
            return await AssignEmpLevelClimbAsync(context, job, level, climbHops, ct);

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
                candidates = await ResolveOrgChainApproverAsync(context, job.reqOrg, allowSkipVacant: false, maxHops: hopNumber, job.createuserid, ct);
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

            // epms parity: snapshot each resolved approver's real name onto the
            // inbox row so the approval screens show a name, not an id/empno.
            // Prefer the authoritative Hremployee name; fall back to sc_user.
            var candEmpIds = candidates.Where(c => c.EmpId != null).Select(c => c.EmpId!).Distinct().ToList();
            var candUserIds = candidates.Select(c => c.UserId).Distinct().ToList();
            var empNames = (await context.Hremployee.Where(e => candEmpIds.Contains(e.EmpNo))
                    .Select(e => new { e.EmpNo, e.EmpName, e.EmpSurname }).ToListAsync(ct))
                .ToDictionary(e => e.EmpNo, e => $"{e.EmpName} {e.EmpSurname}".Trim());
            var userNames = (await context.sc_users.Where(u => candUserIds.Contains(u.userid))
                    .Select(u => new { u.userid, u.firstname, u.lastname }).ToListAsync(ct))
                .ToDictionary(u => u.userid, u => $"{u.firstname} {u.lastname}".Trim());

            foreach (var (userId, empId) in candidates)
            {
                var approverName = empId != null && empNames.TryGetValue(empId, out var en) && !string.IsNullOrWhiteSpace(en)
                    ? en
                    : userNames.TryGetValue(userId, out var un) && !string.IsNullOrWhiteSpace(un) ? un : null;
                context.job_user_lists.Add(new job_user_list
                {
                    jobmasterid = job.jobmasterid,
                    workflowid = job.workflowid,
                    wlevel = level.wlevel,
                    userid = userId,
                    empid = empId,
                    username = approverName,
                    subworkflowmasterid = level.subworkflowid,
                    jobstatus = StatusPending,
                    sendDate = DateTime.Now,
                    reason = precheckReasonPrefix,
                    andPercent = andWeight,
                    jobseq = job.jobseq,
                    isLast = true,
                });

                // Vertical pre-check hops are also real people who need to
                // act — notify them too, not just the level's own real round.
                await NotifyApproverAsync(context, job, userId, empId, ct);
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

    // Self-terminating vertical climb (CEO, 2026-09-07): "climb up from the
    // requester's own org, at most N levels — whichever hop actually
    // resolves (hop N, or an earlier hop if the org chart runs out first,
    // e.g. a senior requester whose chain reaches the CEO in only 2 hops)
    // closes the job, not always a fixed level N." Each hop is its own
    // approval round on the SAME wlevel (same shape as the Mix Approval
    // precheck hops), but this level IS the whole approval — there's no
    // separate final round after the last hop. Re-derives the current hop
    // number from job_user_list every call, so TryAdvanceLevelAsync can call
    // this again for "one more hop" simply by re-entering here.
    private async Task<WorkflowOutcome> AssignVerticalChainHopAsync(HRMContext context, job_master job, wf_sub_workflow_master level, int maxLevel, CancellationToken ct)
    {
        var hopsApproved = await context.job_user_lists.CountAsync(a =>
            a.jobmasterid == job.jobmasterid && a.wlevel == level.wlevel && (a.jobseq ?? 0) == (job.jobseq ?? 0)
            && a.reason != null && a.reason.StartsWith(VerticalChainMarker) && a.jobstatus == StatusApproved, ct);
        var hopNumber = hopsApproved + 1;

        var (candidates, isTerminalHop) = await ResolveVerticalChainHopAsync(context, job.reqOrg, hopNumber, maxLevel, job.createuserid, ct);

        // Freeze this hop's terminal-ness onto the job's own snapshot row —
        // TryAdvanceLevelAsync reads istop fresh from the DB (possibly in a
        // later, separate top-level Approve/Reject call), so this is the
        // only way it can know, once this hop resolves, whether to close the
        // job or ask for one more hop.
        var snapshotRow = await CurrentFootprintAsync(context, job.jobmasterid, level.wlevel, ct)
            ?? throw new InvalidOperationException($"ไม่พบ config ระดับ {level.wlevel} ของงานนี้ — ข้อมูล snapshot ไม่ครบ");
        snapshotRow.istop = isTerminalHop;

        var reasonText = isTerminalHop
            ? $"{VerticalChainMarker} {hopNumber}/{maxLevel} (ระดับสุดท้าย — วิ่งจนสุดผังองค์กรหรือครบจำนวนที่ตั้งไว้)"
            : $"{VerticalChainMarker} {hopNumber}/{maxLevel}";

        if (candidates.Count > 0)
        {
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
                    reason = reasonText,
                    jobseq = job.jobseq,
                    isLast = true,
                });
                await NotifyApproverAsync(context, job, userId, empId, ct);
            }
            return WorkflowOutcome.StillOpen;
        }

        // Nobody resolved at this hop (vacant org, or the only candidate was
        // the requester themselves) — same vacant-for-admin handling as the
        // ordinary path, so /wf/vacant-approvals still covers a stalled chain.
        Serilog.Log.Warning("Job {JobMasterId} level {Level} vertical-chain hop {Hop}/{Max}: no candidate resolved, left vacant for admin assignment",
            job.jobmasterid, level.wlevel, hopNumber, maxLevel);
        context.job_user_lists.Add(new job_user_list
        {
            jobmasterid = job.jobmasterid,
            workflowid = job.workflowid,
            wlevel = level.wlevel,
            userid = null,
            subworkflowmasterid = level.subworkflowid,
            jobstatus = StatusPending,
            reason = $"{reasonText} | ตำแหน่งว่าง — รอ admin มอบหมายผู้อนุมัติ (ดู /wf/vacant-approvals)",
            jobseq = job.jobseq,
            isLast = true,
        });
        return WorkflowOutcome.StillOpen;
    }

    // Fixed-count org-chart climb (CEO, 2026-09-07 follow-up, empLevel):
    // "พนักงานเริ่ม job อยู่ระดับ 17, subworkflow บอกวิ่ง 3 ระดับ ->
    // 17->16->15->14 แล้วจบ ไปวิ่ง subworkflow ถัดไป". Reuses the exact same
    // hop-landing/terminal-detection primitive as AssignVerticalChainHopAsync
    // (ResolveVerticalChainHopAsync — adaptive: lands early only if the org
    // chart itself runs out before `climbHops` hops), but never touches
    // istop — TryAdvanceLevelAsync's re-entry check for this mechanism reads
    // the terminal-ness back from the just-approved hop's own reason text
    // (stamped below) instead, then falls through to ordinary istop/next-
    // level handling once the climb is done, exactly like any other level.
    private async Task<WorkflowOutcome> AssignEmpLevelClimbAsync(HRMContext context, job_master job, wf_sub_workflow_master level, int climbHops, CancellationToken ct)
    {
        var hopsApproved = await context.job_user_lists.CountAsync(a =>
            a.jobmasterid == job.jobmasterid && a.wlevel == level.wlevel && (a.jobseq ?? 0) == (job.jobseq ?? 0)
            && a.reason != null && a.reason.StartsWith(EmpLevelClimbMarker) && a.jobstatus == StatusApproved, ct);
        var hopNumber = hopsApproved + 1;

        var (candidates, isTerminalHop) = await ResolveVerticalChainHopAsync(context, job.reqOrg, hopNumber, climbHops, job.createuserid, ct);

        var reasonText = isTerminalHop
            ? $"{EmpLevelClimbMarker} {hopNumber}/{climbHops} (ระดับสุดท้าย — ครบตามที่ตั้งไว้หรือสุดผังองค์กร — ต่อไปยังขั้นถัดไปตามปกติ)"
            : $"{EmpLevelClimbMarker} {hopNumber}/{climbHops}";

        if (candidates.Count > 0)
        {
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
                    reason = reasonText,
                    jobseq = job.jobseq,
                    isLast = true,
                });
                await NotifyApproverAsync(context, job, userId, empId, ct);
            }
            return WorkflowOutcome.StillOpen;
        }

        Serilog.Log.Warning("Job {JobMasterId} level {Level} empLevel climb hop {Hop}/{Max}: no candidate resolved, left vacant for admin assignment",
            job.jobmasterid, level.wlevel, hopNumber, climbHops);
        context.job_user_lists.Add(new job_user_list
        {
            jobmasterid = job.jobmasterid,
            workflowid = job.workflowid,
            wlevel = level.wlevel,
            userid = null,
            subworkflowmasterid = level.subworkflowid,
            jobstatus = StatusPending,
            reason = $"{reasonText} | ตำแหน่งว่าง — รอ admin มอบหมายผู้อนุมัติ (ดู /wf/vacant-approvals)",
            jobseq = job.jobseq,
            isLast = true,
        });
        return WorkflowOutcome.StillOpen;
    }

    // Walks EXACTLY to hop `hopNumber` from anchorOrgCode (hop 1 = anchor's
    // own org, hop 2 = its parent, ... — same numbering as
    // ResolveOrgChainApproverAsync). Also reports whether this hop is the
    // chain's terminal one: either hopNumber reached maxLevel, or the org
    // chart ran out (no parent_code) at or before this hop. Each hop is a
    // fixed landing spot, not a skip-and-climb search, so a self-match here
    // (the resolved approver is the requester) just means "vacant at this
    // hop" — it does not climb further; the chain still lands where the
    // schedule says it should, and vacancy is handled the same way it is
    // everywhere else in this engine (admin assignment via
    // /wf/vacant-approvals).
    private static async Task<(List<(long UserId, string? EmpId)> Candidates, bool IsTerminalHop)> ResolveVerticalChainHopAsync(
        HRMContext context, string? anchorOrgCode, int hopNumber, int maxLevel, long? excludeUserId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(anchorOrgCode))
            return (new(), true);

        var org = await context.com_organizations.FirstOrDefaultAsync(o => o.code == anchorOrgCode || o.orgCode == anchorOrgCode, ct);
        for (var hop = 1; org is not null; hop++)
        {
            var chartExhausted = string.IsNullOrWhiteSpace(org.parent_code);
            if (hop == hopNumber || chartExhausted)
            {
                var candidates = new List<(long, string?)>();
                if (org.approver_userid is long uid && uid != excludeUserId)
                    candidates.Add((uid, null));
                else if (!string.IsNullOrWhiteSpace(org.approver_empid))
                {
                    var approverUser = await context.sc_users.FirstOrDefaultAsync(u => u.empid == org.approver_empid, ct);
                    if (approverUser is not null && approverUser.userid != excludeUserId)
                        candidates.Add((approverUser.userid, approverUser.empid));
                }
                return (candidates, hopNumber >= maxLevel || chartExhausted);
            }

            org = await context.com_organizations.FirstOrDefaultAsync(o => o.code == org.parent_code, ct);
        }

        return (new(), true); // org lookup failed entirely — treat as chart-exhausted, not an infinite/unresolved wait
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
            var vertical = await ResolveOrgChainApproverAsync(context, requesterOrgCode, level.isAutoApproveAllow, maxHops, job.createuserid, ct);
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

        // isApproverSameOrg (epms parity — the strategy HRM's model had but
        // never resolved): any other active employee in the requester's own
        // organization unit (job.reqOrg, snapshotted at StartJobAsync from
        // Hremployee.orgcode), same company, excluding the requester. Same
        // sc_user resolution as isApproverSameCostCenter above.
        if (level.isApproverSameOrg)
        {
            anyStrategyConfigured = true;
            if (!string.IsNullOrWhiteSpace(requesterOrgCode) && !string.IsNullOrWhiteSpace(job.empid))
            {
                var requesterCompanyId = await context.Hremployee
                    .Where(e => e.EmpNo == job.empid)
                    .Select(e => e.companyid)
                    .FirstOrDefaultAsync(ct);
                var peerEmpNos = await context.Hremployee
                    .Where(e => e.orgcode == requesterOrgCode && e.companyid == requesterCompanyId && e.EmpNo != job.empid)
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

    // "ผู้อนุมัติไม่ได้รับแจ้งเตือนเมื่อมีงานใหม่เข้าคิว" (CEO, 2026-09-07,
    // JSP-study gap #2) — epms emails whoever is now responsible on every
    // level transition; HRM only ever emailed the REQUESTER, only at
    // closure. This is the approver-side counterpart, fired once per newly
    // created PENDING row in AssignLevelApproversAsync's candidate loop —
    // covers the very first level (StartJobAsync) and every subsequent
    // level/hop transition identically, since that's the one place real
    // approver rows are ever created. empId may be null (org-chain
    // resolution via approver_userid alone doesn't always carry one) — fall
    // back to the approver's own sc_user.empid in that case.
    private async Task NotifyApproverAsync(HRMContext context, job_master job, long approverUserId, string? approverEmpId, CancellationToken ct)
    {
        try
        {
            var empId = approverEmpId;
            if (string.IsNullOrWhiteSpace(empId))
            {
                empId = await context.sc_users.Where(u => u.userid == approverUserId)
                    .Select(u => u.empid).FirstOrDefaultAsync(ct);
            }
            if (string.IsNullOrWhiteSpace(empId))
                return;

            var emp = await context.Hremployee.FirstOrDefaultAsync(e => e.EmpNo == empId, ct);
            if (emp is null)
                return;

            var email = await EmployeeEmailResolver.ResolveAsync(context, emp.id, ct);
            if (string.IsNullOrWhiteSpace(email))
                return;

            var subject = $"มีคำขอรออนุมัติใหม่: {job.subject ?? job.wname}";
            var body = $"<p>คุณมีคำขออนุมัติใหม่รอดำเนินการ เรื่อง \"{job.subject}\" (Workflow: {job.wname})</p>"
                + "<p>กรุณาเข้าสู่ระบบและตรวจสอบที่หน้า \"งานรออนุมัติของฉัน\"</p>";
            await _emailSender.SendEmailAsync(email, subject, body);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Failed to send new-pending-approval notification for job {JobMasterId} to user {UserId}", job.jobmasterid, approverUserId);
        }
    }

    // Display names of the approver(s) a freshly-submitted (or in-flight) job is
    // currently waiting on — resolved from the lowest level that still has
    // PENDING rows. Lets a requester see exactly who their request just went to.
    // Returns "" when nothing is pending (e.g. the job already closed/auto-approved).
    // The workflow's full approval PLAN, straight from config — every level
    // of wf_sub_workflow_master with who is set to approve it (CEO,
    // 2026-09-08: "ทายไม่ได้ครับต้องแม่น ... เอา subworkflow มากางก่อน แล้วไป
    // ดึงจาก job_sub มาแนบ"). Nothing here is resolved at runtime or guessed:
    // named users come from wf_custom_user, roles from wf_custom_role, and
    // every other strategy is described by the flag that's actually ticked.
    // Callers lay this out as the skeleton, then overlay the job's real
    // job_subworkflow_master / job_user_list rows on top.
    public record LevelApproverPlan(int Level, string? Label, string ApproverText);

    public async Task<List<LevelApproverPlan>> GetApproverPlanAsync(long workflowId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var levels = await context.wf_sub_workflow_masters
            .Where(s => s.workflowid == workflowId)
            .OrderBy(s => s.wlevel)
            .ToListAsync(ct);
        if (levels.Count == 0) return new();

        // Named users configured per level.
        var customUsers = await context.wf_custom_users
            .Where(u => u.workflowid == workflowId && u.isactive)
            .Select(u => new { u.wlevel, u.userid })
            .ToListAsync(ct);
        var userIds = customUsers.Select(u => u.userid).Distinct().ToList();
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
        var nameByUserId = users.ToDictionary(
            u => u.userid,
            u => u.empid != null && nameByEmpNo.TryGetValue(u.empid, out var n) && !string.IsNullOrWhiteSpace(n)
                ? n
                : (!string.IsNullOrWhiteSpace(u.loginname) ? u.loginname! : $"#{u.userid}"));

        // Roles configured per level.
        var customRoles = await context.wf_custom_roles
            .Where(r => r.workflowid == workflowId && r.isactive == true)
            .Select(r => new { r.wlevel, r.roleid })
            .ToListAsync(ct);
        var roleIds = customRoles.Select(r => r.roleid).Distinct().ToList();
        var roleNames = await context.sc_roles
            .Where(r => roleIds.Contains(r.roleid))
            .ToDictionaryAsync(r => r.roleid, r => r.name, ct);

        var plan = new List<LevelApproverPlan>();
        foreach (var level in levels)
        {
            var parts = new List<string>();

            if (level.iscustomUser)
            {
                var names = customUsers.Where(u => u.wlevel == level.wlevel)
                    .Select(u => nameByUserId.GetValueOrDefault(u.userid, $"#{u.userid}"))
                    .Distinct().ToList();
                if (names.Count > 0) parts.Add(string.Join(" / ", names));
            }
            if (level.iscustomRole)
            {
                var names = customRoles.Where(r => r.wlevel == level.wlevel)
                    .Select(r => roleNames.GetValueOrDefault(r.roleid) ?? $"role #{r.roleid}")
                    .Distinct().ToList();
                if (names.Count > 0) parts.Add($"บทบาท: {string.Join(" / ", names)}");
            }
            if (level.isupperrole || level.isupperuser)
            {
                var hops = level.verticalMaxLevel ?? level.empLevel;
                parts.Add(hops is int h && h > 0 ? $"หัวหน้าตามผังองค์กร (ไต่ {h} ระดับ)" : "หัวหน้าตามผังองค์กร");
            }
            if (level.isNeedsupervisorapprove is int precheck && precheck > 0)
                parts.Add($"ผ่านหัวหน้า {precheck} ระดับก่อน");
            if (level.isLOA) parts.Add("ผู้อนุมัติตามวงเงิน (LOA)");
            if (level.isReturnSender) parts.Add("ส่งกลับผู้ยื่นคำขอ");
            if (level.isApproverSameOrg) parts.Add("ผู้อนุมัติในหน่วยงานเดียวกับผู้ขอ");
            if (level.isApproverSameCostCenter) parts.Add("ผู้อนุมัติใน Cost Center เดียวกัน");
            if (level.isAdhocUser) parts.Add("ผู้อนุมัติเฉพาะกิจของงานนั้น");

            plan.Add(new LevelApproverPlan(level.wlevel, level.subject, string.Join(" · ", parts)));
        }

        return plan;
    }

    public async Task<string> GetPendingApproverNamesAsync(long jobMasterId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        // "ตอนนี้ใครค้างอนุมัติอยู่" — ต้องอ่านจากขั้นที่ job_master บอกว่างานอยู่
        // เดิมกรองแค่ PENDING แล้วเอา min(wlevel) ซึ่งผิดเสมอเมื่อขั้นก่อนหน้ามี
        // ผู้อนุมัติหลายคน: ใบของคนที่เหลือในขั้นเก่ายังค้าง PENDING และ wlevel
        // น้อยกว่าขั้นปัจจุบัน min() จึงไปหยิบขั้นที่งานผ่านไปแล้วมาตอบ
        var pending = await context.job_user_lists
            .Where(IsLiveApprovalRow)
            .Where(a => a.jobmasterid == jobMasterId)
            .Select(a => new { a.wlevel, a.userid })
            .ToListAsync(ct);
        if (pending.Count == 0) return "";

        var userIds = pending.Where(p => p.userid.HasValue)
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
