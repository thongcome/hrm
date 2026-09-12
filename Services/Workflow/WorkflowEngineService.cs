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

    private readonly HRM.Services.Audit.IAuditLogger _auditLogger;

    // ตัวใหม่ถูกฉีดเข้ามาแบบขี้เกียจ (Lazy) เพราะทั้งสองตัวอ้างถึงกัน —
    // WorkflowService เรียก EvaluateLevel ของตัวนี้ ส่วนตัวนี้ส่งงานต่อให้ตัวนั้น
    private readonly IServiceProvider _sp;
    private WorkflowService NewEngine => _sp.GetRequiredService<WorkflowService>();


    public WorkflowEngineService(IDbContextFactory<HRMContext> dbFactory,
        HRM.Services.Audit.IAuditLogger auditLogger, IServiceProvider sp)
    {
        _dbFactory = dbFactory;
        _auditLogger = auditLogger;
        _sp = sp;
    }

    // 12 ก.ย. 2569 — engine เดิมปลดแล้ว: ทุก workflow เดินด้วย WorkflowService ทางเดียว
    // (wf_workflow.useNewEngine ถูกตั้ง 1 ทั้งหมดใน Migrations/Manual/2026-09-12_retire_old_workflow_engine.sql)
    // คลาสนี้เหลือเป็นทางเข้าที่โมดูล 21 ตัวเรียกอยู่ + เมธอดอ่านข้อมูล/กล่องงาน/pool
    private static WorkFlowViewModel Carry(long jobMasterId, long actorUserId, string? comment, long? reasonId)
        => new() { jobmasterid = jobMasterId, actorUserId = actorUserId, reason = comment, mas_reason_id = reasonId };

    // ── ทางเข้าเดียวของ "ปุ่ม" ──────────────────────────────────────────────
    // CEO, 10 ก.ย. 2569: ปุ่มทุกหน้าต้องเหมือนกัน — WfActionButtons สร้างปุ่มจาก
    // WorkflowButtonService แล้วส่งมาแค่ "ทำอะไร" (approve / sendback / decline)
    // ที่นี่ค่อยดูว่างานนี้เดินด้วยเครื่องไหน แล้วเรียก method ที่ตรงกัน
    // หน้าจึงไม่ต้องรู้เรื่อง engine เก่า-ใหม่ (เดิมกล่องงานเรียก ApproveAsync ของ
    // เครื่องเดิมใส่งานของเครื่องใหม่ตรง ๆ)
    public async Task ActAsync(long jobMasterId, long jobApproverId, long actorUserId, string actionKind,
        string? comment, long? reasonId = null, CancellationToken ct = default)
    {
        try { await NewEngine.ActAsync(Carry(jobMasterId, actorUserId, comment, reasonId), actionKind, ct); }
        catch (DbUpdateConcurrencyException ex) { throw WorkflowService.ConcurrentActionError(ex); }   // audit H5
    }

    // ── ร่าง: โมดูลบันทึกเอกสารของตัวเองก่อน แล้วเปิดงานที่ขั้น 0 ด้วย id ที่เพิ่งได้ ─────
    //
    //  CEO, 11 ก.ย. 2569: "ตอน save draft เราจะรู้ id ของที่เราสร้าง เราสร้าง status -> draft
    //  ตอน level 0 ... ตอน workflow วิ่ง id ที่เราสร้างคือ refid — workflow ให้สนใจ logic
    //  บนตารางหลัก สร้าง method ไว้ call ถ้าโปรแกรมทำงานถูกมันจะถูกเสมอ"
    //
    //  ลำดับที่โมดูลต้องทำ:
    //    1. บันทึกแถวในตารางของโมดูล (เช่น Exp_ClaimHeader) -> ได้ id
    //    2. CreateDraftAsync(workflowcode, reftable, id, ...) -> ได้ jobmasterid เก็บกลับลงแถวนั้น
    //    3. ผู้ยื่นกด "ส่งต่อ" ในหน้างาน (หรือโมดูลเรียก ActAsync approve ที่ขั้น 0) งานถึงเข้าเส้นทาง
    //    4. ก่อนวิ่ง ลบได้ด้วย DeleteDraftAsync แล้วโมดูลลบแถวของตัวเอง
    //  reftable/refid มาจากโค้ดของโมดูลเท่านั้น ไม่รับจาก URL (ปิดช่อง IDOR 11 ก.ย. 2569)
    public async Task<long> CreateDraftAsync(string workflowCode, string reftable, string refid,
        long requesterUserId, string? requesterEmpId, string? subject, decimal? amount, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reftable) || string.IsNullOrWhiteSpace(refid))
            throw new InvalidOperationException("ต้องระบุตารางและ id ของเอกสารที่บันทึกแล้ว (reftable/refid)");

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var workflow = await context.wf_workflows.FirstOrDefaultAsync(w => w.workflowcode == workflowCode, ct)
            ?? throw new InvalidOperationException($"ไม่พบ workflow '{workflowCode}'");
        if (await context.job_masters.AnyAsync(j => j.workflowid == workflow.workflowid
                && j.reftable == reftable && j.refid == refid && j.isJobClosed != true, ct))
            throw new InvalidOperationException($"เอกสาร {reftable} #{refid} มีงานที่ยังเปิดอยู่แล้ว");

        var created = await NewEngine.CreateAsync(workflow.workflowid, requesterUserId, requesterEmpId,
            subject, reftable, refid, amount, ct);
        return created.jobmasterid;
    }

    // ลบร่าง (ขั้น 0 ที่ยังไม่เคยวิ่ง) — โมดูลเรียกก่อนลบแถวเอกสารของตัวเอง
    public Task DeleteDraftAsync(long jobMasterId, long actorUserId, CancellationToken ct = default)
        => NewEngine.DeleteDraftAsync(jobMasterId, actorUserId, ct);

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
        var created = await NewEngine.CreateAsync(workflowId, requesterUserId, requesterEmpId,
            subject, reftable, refid, amount, ct);

        // isautoapprove เป็นค่าของ workflow ไม่ใช่ของ engine — ต้องมีผลทั้งสองทาง
        // (เดิมสาขานี้อยู่เหนือการเช็ค isautoapprove ด้านล่าง workflow ที่ย้ายมา
        //  engine ใหม่จึงเข้าสายอนุมัติปกติเงียบ ๆ ทั้งที่ตั้ง auto ไว้)
        if (workflow.isautoapprove == true)
        {
            await NewEngine.AutoApproveAsync(Carry(created.jobmasterid, requesterUserId, null, null), ct);
            return created.jobmasterid;
        }

        await NewEngine.SubmitAsync(
            Carry(created.jobmasterid, requesterUserId, null, null), ct);
        return created.jobmasterid;
    }
    // รอยเท้าล่าสุดของระดับนั้น (jobseq สูงสุด) — ใช้แทนการอ่าน "แถวของระดับ X"
    // เพราะตอนนี้หนึ่งระดับมีได้หลายแถว
    private static Task<job_subworkflow_master?> CurrentFootprintAsync(
        HRMContext context, long jobMasterId, int? wlevel, CancellationToken ct)
        => context.job_subworkflow_masters
            .Where(s => s.jobmasterid == jobMasterId && s.wlevel == wlevel)
            .OrderByDescending(s => s.jobseq).ThenByDescending(s => s.jobsubworkflowid)
            .FirstOrDefaultAsync(ct);

    public Task CancelAsync(long jobMasterId, long actorUserId, bool isAdminOverride, string? reason, CancellationToken ct = default)
        => NewEngine.CancelAsync(Carry(jobMasterId, actorUserId, reason, null), isAdminOverride, ct);

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
        row.username = $"{assignee.firstname} {assignee.lastname}".Trim();   // snapshot ตามคนที่ได้รับมอบหมาย (audit M3)
        row.orgcode = assignee.orgcode;
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
        // snapshot บนใบงานต้องตามคนใหม่ด้วย — หน้าจอและรายงานอ่านชื่อ/หน่วยงานจากใบงาน ไม่ได้ join สด (audit M3)
        row.username = $"{newAssignee.firstname} {newAssignee.lastname}".Trim();
        row.orgcode = newAssignee.orgcode;
        row.reason = string.IsNullOrWhiteSpace(row.reason) ? $"เปลี่ยนผู้อนุมัติโดย admin: {reason}" : $"{row.reason} | เปลี่ยนผู้อนุมัติโดย admin: {reason}";

        // ถ้าคนเดิมรับงาน pool ไว้ ต้องปล่อยคืน ไม่งั้นคนใหม่กดไม่ได้เพราะ "มีคนอื่นรับไปแล้ว"
        var pooled = await context.job_masters.FirstOrDefaultAsync(j => j.jobmasterid == row.jobmasterid, ct);
        if (pooled is not null && pooled.PoolClaimedByUserId == oldUserId && pooled.PoolClaimedWLevel == row.wlevel)
        {
            pooled.PoolClaimedByUserId = null;
            pooled.PoolClaimedWLevel = null;
            pooled.PoolClaimedJobSeq = null;
            pooled.PoolClaimedDate = null;
        }
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

        // ใบงานที่ "มีชีวิต" = PENDING + isLast + ขั้นตรงกับ lastLevel — ห้ามใช้ jobseq (engine ใหม่ขยับ
        // job.jobseq ทุก action คนที่สองในขั้น pool จึงรับงานไม่ได้ทั้งที่ยังถือใบงานอยู่) (audit M1)
        var myPendingRow = await context.job_user_lists.FirstOrDefaultAsync(a =>
            a.jobmasterid == jobMasterId && a.userid == actorUserId && a.jobstatus == StatusPending
            && a.wlevel == job.lastLevel && a.isLast == true, ct)
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
        // ประวัติทั้งหมดของคนหนึ่งโตไม่มีที่สิ้นสุด — หน้ากล่องงานเอาล่าสุด 300 รายการพอ (audit M11)
        return await context.job_user_lists
            .Include(a => a.jobmaster).ThenInclude(j => j.workflow)
            .Where(a => a.userid == userId)
            .OrderByDescending(a => a.jobmaster.createdate).ThenByDescending(a => a.jobapproverid)
            .Take(300)
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

    // หน้ารายการ (เช่น งานที่ฉันขอไป) ถามทีเดียวทั้งชุด ไม่วนถามทีละงาน (audit M11)
    public async Task<Dictionary<long, string>> GetPendingApproverNamesAsync(IEnumerable<long> jobMasterIds, CancellationToken ct = default)
    {
        var ids = jobMasterIds.Distinct().ToList();
        var result = new Dictionary<long, string>();
        if (ids.Count == 0) return result;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var pending = await context.job_user_lists
            .Where(IsLiveApprovalRow)
            .Where(a => ids.Contains(a.jobmasterid))
            .Select(a => new { a.jobmasterid, a.userid })
            .ToListAsync(ct);
        if (pending.Count == 0) return result;

        var userIds = pending.Where(p => p.userid.HasValue).Select(p => p.userid!.Value).Distinct().ToList();
        var users = await context.sc_users
            .Where(u => userIds.Contains(u.userid))
            .Select(u => new { u.userid, u.empid, u.loginname })
            .ToListAsync(ct);
        var empIds = users.Where(u => u.empid != null).Select(u => u.empid!).Distinct().ToList();
        var nameByEmpNo = (await context.Hremployee
            .Where(e => empIds.Contains(e.EmpNo))
            .Select(e => new { e.EmpNo, e.EmpName, e.EmpSurname })
            .ToListAsync(ct))
            .ToDictionary(e => e.EmpNo, e => $"{e.EmpName} {e.EmpSurname}".Trim());
        var nameByUser = users.ToDictionary(u => u.userid, u =>
            u.empid != null && nameByEmpNo.TryGetValue(u.empid, out var n) && !string.IsNullOrWhiteSpace(n)
                ? n
                : (!string.IsNullOrWhiteSpace(u.loginname) ? u.loginname! : $"#{u.userid}"));

        foreach (var g in pending.GroupBy(p => p.jobmasterid))
        {
            var names = g.Where(p => p.userid.HasValue).Select(p => nameByUser.GetValueOrDefault(p.userid!.Value, $"#{p.userid}")).Distinct();
            var joined = string.Join(" / ", names);
            if (!string.IsNullOrWhiteSpace(joined)) result[g.Key] = joined;
        }
        return result;
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
