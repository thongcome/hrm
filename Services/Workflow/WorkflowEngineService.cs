namespace HRM.Services.Workflow;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Blocks 2-6 + 9 of the Workflow Approval Engine: sequential level
// advancement, approve/reject, full approver resolution (Horizontal +
// Vertical + vacancy handling), LOA amount-based branching, AND-condition %
// partial approval, Mix Approval (vertical pre-check hops before a level's
// own approver), and admin-configured Moving Status text. Still
// deliberately excludes:
//   - Backward-level bounce on reject — reject terminates the job outright
//     (or fails an AND-condition level once threshold is unreachable), it
//     never routes the job back to an earlier level for revision
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

    // Block 6 (Mix Approval): job_user_list.reason rows for a vertical
    // pre-check hop always start with this marker, so hop-completion count
    // and "which round just resolved" detection can be done by string match
    // instead of a new column — jobseq was deliberately NOT reused for this
    // (see epms WorkflowController.cs: job.jobseq filters job_user_list by
    // job-wide resubmission round, a different concept entirely — reusing
    // it here would collide with that semantic if resubmission is ever
    // implemented later).
    private const string VerticalPrecheckMarker = "VERTICAL_PRECHECK";

    private enum LevelOutcome { StillPending, Complete, Failed }

    public WorkflowEngineService(IDbContextFactory<HRMContext> dbFactory)
    {
        _dbFactory = dbFactory;
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

        // Anchor org for Vertical resolution — resolved from wf_employee
        // (our clean pilot employee table), not Hremployee. Hremployee's
        // org linkage into com_organization is a KNOWN broken data-quality
        // gap from earlier work this session (Hremployee.DEPTGRP_CODE
        // doesn't match com_organization.code for real employees) — using
        // it here would make Vertical resolution silently fail for almost
        // everyone. Once that linkage is fixed, this should read from the
        // real HR source of truth instead.
        string? requesterOrgCode = null;
        if (!string.IsNullOrWhiteSpace(requesterEmpId))
        {
            requesterOrgCode = await context.wf_employees
                .Where(e => e.empid == requesterEmpId)
                .Select(e => e.orgcode)
                .FirstOrDefaultAsync(ct);
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
            isactive = true,
            isJobClosed = false,
        };
        context.job_masters.Add(job);
        await context.SaveChangesAsync(ct); // need job.jobmasterid before snapshotting levels

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
                moddate = DateTime.Now,
            });
        }
        await context.SaveChangesAsync(ct);

        await AssignLevelApproversAsync(context, job, levels[0], ct);
        await context.SaveChangesAsync(ct);

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
        if (approverRow.wlevel != job.lastLevel)
            throw new InvalidOperationException("งานนี้เลื่อนผ่านระดับนี้ไปแล้ว ไม่สามารถดำเนินการกับรายการเก่านี้ได้");

        approverRow.jobstatus = StatusApproved;
        approverRow.approvedate = DateTime.Now;
        approverRow.comment = comment;
        await context.SaveChangesAsync(ct);

        await TryAdvanceLevelAsync(context, job, approverRow.wlevel ?? 0, actorUserId, ct);
        await context.SaveChangesAsync(ct);
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
        if (approverRow.wlevel != job.lastLevel)
            throw new InvalidOperationException("งานนี้เลื่อนผ่านระดับนี้ไปแล้ว ไม่สามารถดำเนินการกับรายการเก่านี้ได้");

        approverRow.jobstatus = StatusRejected;
        approverRow.approvedate = DateTime.Now;
        approverRow.comment = comment;
        await context.SaveChangesAsync(ct);

        // Block 5 change from Block 2: a single reject no longer
        // unconditionally kills the job on the spot — TryAdvanceLevelAsync's
        // evaluator only fails the level (and the job) once the remaining
        // possible approval weight can no longer reach the AND% threshold.
        // Non-AND levels still fail immediately (any rejected row => Failed
        // in EvaluateLevel below), matching Block 2/3 behavior exactly.
        await TryAdvanceLevelAsync(context, job, approverRow.wlevel ?? 0, actorUserId, ct);
        await context.SaveChangesAsync(ct);
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
    private static LevelOutcome EvaluateLevel(job_subworkflow_master snapshot, List<job_user_list> rows)
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
    private async Task TryAdvanceLevelAsync(HRMContext context, job_master job, int completedLevel, long? actorUserId, CancellationToken ct)
    {
        var completedSnapshot = await context.job_subworkflow_masters
            .FirstOrDefaultAsync(s => s.jobmasterid == job.jobmasterid && s.wlevel == completedLevel, ct)
            ?? throw new InvalidOperationException($"ไม่พบ config ระดับ {completedLevel} ของงานนี้ — ข้อมูล snapshot ไม่ครบ");

        var allRows = await context.job_user_lists
            .Where(a => a.jobmasterid == job.jobmasterid && a.wlevel == completedLevel)
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
                return;

            if (precheckRows.Any(a => string.Equals(a.jobstatus, StatusRejected, StringComparison.OrdinalIgnoreCase)))
            {
                FailJob(job, completedSnapshot, precheckRows.LastOrDefault(a => string.Equals(a.jobstatus, StatusRejected, StringComparison.OrdinalIgnoreCase))?.comment);
                return;
            }

            // All hops resolved (or no hops configured at all) — let
            // AssignLevelApproversAsync decide whether to issue the next
            // hop or the level's own real round; it re-derives
            // hopsSatisfied itself from these same rows.
            var liveLevelForHop = await context.wf_sub_workflow_masters
                .FirstOrDefaultAsync(s => s.workflowid == job.workflowid && s.wlevel == completedLevel, ct)
                ?? throw new InvalidOperationException($"ไม่พบ config ต้นฉบับของระดับ {completedLevel} ใน wf_sub_workflow_master แล้ว (อาจถูกลบหลังงานเริ่ม)");
            await AssignLevelApproversAsync(context, job, liveLevelForHop, ct);
            return;
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
            return;

        if (outcome == LevelOutcome.Failed)
        {
            FailJob(job, completedSnapshot, realRoundRows.LastOrDefault(r => string.Equals(r.jobstatus, StatusRejected, StringComparison.OrdinalIgnoreCase))?.comment);
            return;
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
            return;
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

        await AssignLevelApproversAsync(context, job, nextLiveLevel, ct);
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

    // Block 6 (Mix Approval): walks up the org chart `hopNumber` steps
    // starting from the requester's own org (hop 1 = requester's org
    // itself, hop 2 = its parent, ...) via com_organization.parent_code,
    // then resolves that org's approver using the same "always use
    // approver_empid" rule as the ordinary Vertical resolution in
    // ResolveCandidatesAsync below (approver_empid is the workflow's real
    // approver, which may differ from boss_emp_id — see plan file).
    private static async Task<List<(long UserId, string? EmpId)>> ResolveVerticalHopApproverAsync(
        HRMContext context, string? anchorOrgCode, int hopNumber, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(anchorOrgCode))
            return new();

        var org = await context.com_organizations.FirstOrDefaultAsync(o => o.code == anchorOrgCode || o.orgCode == anchorOrgCode, ct);
        for (var i = 1; i < hopNumber && org is not null; i++)
        {
            if (string.IsNullOrWhiteSpace(org.parent_code))
            {
                org = null;
                break;
            }
            org = await context.com_organizations.FirstOrDefaultAsync(o => o.code == org.parent_code, ct);
        }
        if (org is null)
            return new();

        if (org.approver_userid is not null)
            return new() { (org.approver_userid.Value, null) };

        if (!string.IsNullOrWhiteSpace(org.approver_empid))
        {
            var approverUser = await context.sc_users.FirstOrDefaultAsync(u => u.empid == org.approver_empid, ct);
            if (approverUser is not null)
                return new() { (approverUser.userid, approverUser.empid) };
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
    private async Task AssignLevelApproversAsync(HRMContext context, job_master job, wf_sub_workflow_master level, CancellationToken ct)
    {
        // Block 9: display status reflects whichever level (or hop) is now
        // the open round — kept simple (no separate hop-specific status
        // text) since wf_sub_workflow_master only has one "pending" label.
        job.status = level.standstatus ?? StatusPending;

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
                candidates = await ResolveVerticalHopApproverAsync(context, job.reqOrg, hopNumber, ct);
                precheckReasonPrefix = $"{VerticalPrecheckMarker} {hopNumber}/{neededHops}: รอหัวหน้าอนุมัติก่อนเข้าสู่ระดับนี้ (Mix Approval)";
            }
            else
            {
                candidates = await ResolveCandidatesAsync(context, level, job.reqOrg, ct);
            }
        }
        else
        {
            candidates = await ResolveCandidatesAsync(context, level, job.reqOrg, ct);
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
                });
            }
            return;
        }

        if (level.isAutoApproveAllow)
        {
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
            });
            await context.SaveChangesAsync(ct);
            await TryAdvanceLevelAsync(context, job, level.wlevel, null, ct);
            return;
        }

        // No one resolved and this level doesn't allow auto-skip — including
        // the plan's explicit "last level vacant -> stay pending" case,
        // which falls out of this same branch naturally since there's no
        // special-casing of the last level here.
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
        });
    }

    // Horizontal (custom user / custom role) + Vertical (org-chart) approver
    // resolution against the LIVE wf_sub_workflow_master row. Returns an
    // empty list — never throws — when the level is configured but nobody
    // currently resolves (a runtime vacancy, handled by the caller); still
    // throws for a genuine setup mistake (level has none of
    // iscustomUser/iscustomRole/isupperrole/isupperuser ticked at all).
    private static async Task<List<(long UserId, string? EmpId)>> ResolveCandidatesAsync(
        HRMContext context, wf_sub_workflow_master level, string? requesterOrgCode, CancellationToken ct)
    {
        if (level.iscustomUser)
        {
            var users = await context.wf_custom_users
                .Where(u => u.subworkflowid == level.subworkflowid && u.isactive)
                .Select(u => new { u.userid, u.empid })
                .ToListAsync(ct);
            return users.Select(u => (u.userid, u.empid)).ToList();
        }

        if (level.iscustomRole)
        {
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
            return withEmp.Select(u => (u.userid, u.empid)).ToList();
        }

        if (level.isupperrole || level.isupperuser)
        {
            // Vertical: resolve via com_organization.approver_userid /
            // approver_empid for the REQUESTER's own org unit — per the
            // plan's explicit clarification, approver_empid (not
            // boss_emp_id) is always the real workflow approver, since it
            // may be an acting substitute rather than the literal boss.
            if (string.IsNullOrWhiteSpace(requesterOrgCode))
                return new(); // no anchor org known for the requester -> vacancy path

            var org = await context.com_organizations
                .FirstOrDefaultAsync(o => o.code == requesterOrgCode || o.orgCode == requesterOrgCode, ct);
            if (org is null)
                return new(); // requester's org code doesn't match any com_organization row -> vacancy path

            if (org.approver_userid is not null)
                return new() { (org.approver_userid.Value, null) };

            if (!string.IsNullOrWhiteSpace(org.approver_empid))
            {
                var approverUser = await context.sc_users.FirstOrDefaultAsync(u => u.empid == org.approver_empid, ct);
                if (approverUser is not null)
                    return new() { (approverUser.userid, approverUser.empid) };
            }

            return new(); // org found but approver_userid/approver_empid both empty -> vacancy path
        }

        throw new InvalidOperationException(
            $"ระดับ {level.wlevel} ของ workflow นี้ยังไม่ได้กำหนดประเภทผู้อนุมัติเลย (ไม่ได้ติ๊ก custom user / custom role / vertical ใดๆ เลย — ตั้งค่า config ไม่ครบ)");
    }
}
