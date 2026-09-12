using Advance.Workflow.Contracts;
using Advance.Workflow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Advance.Workflow.Engine;

// ============================================================================
//  WorkflowEngineService — ported from HRM's Services/Workflow/
//  WorkflowEngineService.cs (~820 lines, already the thin post-retirement
//  facade as of 12 ก.ย. 2569 — the old pre-WorkflowService engine is gone in
//  HRM itself, this port never had it to begin with). Implements
//  IWorkflowEngine (Advance.Workflow.Contracts) — same method names, same
//  signatures, same status/closed-by constants and the IsLiveApprovalRow
//  predicate every list/inbox query in HRM depends on (see the giant comment
//  block on it below, copied verbatim — the "PENDING alone is not enough"
//  rule is the single most load-bearing piece of logic in the whole engine
//  and must never regress on a port).
//
//  Seam substitutions vs. the original: db.sc_users -> IWorkflowUserDirectory,
//  no more IAuditLogger/Serilog-audit split (see WorkflowService.cs's own
//  header for the audit TODO(seam)). WorkflowService is now a normal
//  constructor dependency, not a lazy IServiceProvider.GetRequiredService
//  call — the original needed laziness only because the file sat in the same
//  project as the (now-retired) OLD engine and the two referenced each
//  other; here there is only ever one engine.
// ============================================================================

public class WorkflowEngineService : IWorkflowEngine
{
    private readonly IDbContextFactory<WorkflowDbContext> _dbFactory;
    private readonly WorkflowService _engine;
    private readonly IWorkflowUserDirectory _users;

    public const string StatusPending = "PENDING";
    public const string StatusApproved = "APPROVED";
    public const string StatusRejected = "REJECTED";
    public const string StatusCompleted = "COMPLETED";
    public const string StatusCancelled = "CANCELLED";
    public const string StatusReturned = "RETURNED";

    public const string ClosedByApprove = "Approve";
    public const string ClosedByDecline = "Decline";
    public const string ClosedByCancel = "Cancel";
    public const string ClosedByAutoApprove = "AutoApprove";

    // ── "ใบงานนี้ยังรอทำอยู่จริงไหม" — กฎเดียวสำหรับทุกที่ที่อ่าน ─────────────
    // See the original in HRM's Services/Workflow/WorkflowEngineService.cs for
    // the full history of why filtering on jobstatus == PENDING alone is
    // wrong (multi-approver levels leave stale PENDING rows behind once the
    // level completes) — copied verbatim, unchanged, because getting this
    // wrong silently breaks every inbox/pool/notice page that reads it.
    public static readonly System.Linq.Expressions.Expression<Func<job_user_list, bool>> IsLiveApprovalRow =
        a => a.jobstatus == StatusPending
          && a.isLast == true
          && a.wlevel == a.jobmaster.lastLevel
          && a.jobmaster.isJobClosed != true;

    public enum LevelOutcome { StillPending, Complete, Failed }

    public WorkflowEngineService(IDbContextFactory<WorkflowDbContext> dbFactory, WorkflowService engine, IWorkflowUserDirectory users)
    {
        _dbFactory = dbFactory;
        _engine = engine;
        _users = users;
    }

    private static WorkFlowViewModel Carry(long jobMasterId, long actorUserId, string? comment, long? reasonId)
        => new() { jobmasterid = jobMasterId, actorUserId = actorUserId, reason = comment, mas_reason_id = reasonId };

    public async Task ActAsync(long jobMasterId, long jobApproverId, long actorUserId, string actionKind,
        string? comment, long? reasonId = null, CancellationToken ct = default)
    {
        try { await _engine.ActAsync(Carry(jobMasterId, actorUserId, comment, reasonId), actionKind, ct); }
        catch (DbUpdateConcurrencyException ex) { throw WorkflowService.ConcurrentActionError(ex); }
    }

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

        var created = await _engine.CreateAsync(workflow.workflowid, requesterUserId, requesterEmpId,
            subject, reftable, refid, amount, ct);
        return created.jobmasterid;
    }

    public Task DeleteDraftAsync(long jobMasterId, long actorUserId, CancellationToken ct = default)
        => _engine.DeleteDraftAsync(jobMasterId, actorUserId, ct);

    public async Task<long> StartJobAsync(long workflowId, string reftable, string refid,
        long requesterUserId, string? requesterEmpId, string? subject, decimal? amount, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var workflow = await context.wf_workflows.FirstOrDefaultAsync(w => w.workflowid == workflowId, ct)
            ?? throw new InvalidOperationException($"ไม่พบ workflow id {workflowId}");
        if (workflow.isactive != true)
            throw new InvalidOperationException($"workflow '{workflow.wname}' ปิดใช้งานอยู่ ไม่สามารถเริ่มงานใหม่ได้");

        var created = await _engine.CreateAsync(workflowId, requesterUserId, requesterEmpId,
            subject, reftable, refid, amount, ct);

        if (workflow.isautoapprove == true)
        {
            await _engine.AutoApproveAsync(Carry(created.jobmasterid, requesterUserId, null, null), ct);
            return created.jobmasterid;
        }

        await _engine.SubmitAsync(Carry(created.jobmasterid, requesterUserId, null, null), ct);
        return created.jobmasterid;
    }

    private static Task<job_subworkflow_master?> CurrentFootprintAsync(
        WorkflowDbContext context, long jobMasterId, int? wlevel, CancellationToken ct)
        => context.job_subworkflow_masters
            .Where(s => s.jobmasterid == jobMasterId && s.wlevel == wlevel)
            .OrderByDescending(s => s.jobseq).ThenByDescending(s => s.jobsubworkflowid)
            .FirstOrDefaultAsync(ct);

    public Task CancelAsync(long jobMasterId, long actorUserId, bool isAdminOverride, string? reason, CancellationToken ct = default)
        => _engine.CancelAsync(Carry(jobMasterId, actorUserId, reason, null), isAdminOverride, ct);

    public async Task AssignApproverAsync(long jobApproverId, long assigneeUserId, string? note, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var row = await context.job_user_lists.FirstOrDefaultAsync(a => a.jobapproverid == jobApproverId, ct)
            ?? throw new InvalidOperationException($"ไม่พบรายการอนุมัติ id {jobApproverId}");
        if (row.userid is not null)
            throw new InvalidOperationException("รายการนี้มีผู้อนุมัติอยู่แล้ว ไม่ใช่ตำแหน่งว่าง");
        if (!string.Equals(row.jobstatus, StatusPending, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("รายการนี้ถูกดำเนินการไปแล้ว");

        var assignee = await _users.GetUserAsync(assigneeUserId, ct)
            ?? throw new InvalidOperationException($"ไม่พบผู้ใช้ id {assigneeUserId}");

        row.userid = assigneeUserId;
        row.empid = assignee.EmpId;
        row.username = assignee.FullName;
        row.orgcode = assignee.OrgCode;
        row.reason = string.IsNullOrWhiteSpace(note) ? row.reason : $"{row.reason} | มอบหมายโดย admin: {note}";
        await context.SaveChangesAsync(ct);

        Serilog.Log.Information("Vacant approver slot {JobApproverId} on job {JobMasterId} assigned to user {AssigneeUserId} by admin",
            jobApproverId, row.jobmasterid, assigneeUserId);
    }

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

        var newAssignee = await _users.GetUserAsync(newUserId, ct)
            ?? throw new InvalidOperationException($"ไม่พบผู้ใช้ id {newUserId}");

        var oldUserId = row.userid;
        row.userid = newUserId;
        row.empid = newAssignee.EmpId;
        row.username = newAssignee.FullName;
        row.orgcode = newAssignee.OrgCode;
        row.reason = string.IsNullOrWhiteSpace(row.reason) ? $"เปลี่ยนผู้อนุมัติโดย admin: {reason}" : $"{row.reason} | เปลี่ยนผู้อนุมัติโดย admin: {reason}";

        var pooled = await context.job_masters.FirstOrDefaultAsync(j => j.jobmasterid == row.jobmasterid, ct);
        if (pooled is not null && pooled.PoolClaimedByUserId == oldUserId && pooled.PoolClaimedWLevel == row.wlevel)
        {
            pooled.PoolClaimedByUserId = null;
            pooled.PoolClaimedWLevel = null;
            pooled.PoolClaimedJobSeq = null;
            pooled.PoolClaimedDate = null;
        }
        await context.SaveChangesAsync(ct);

        Serilog.Log.Information("Job {JobMasterId} level {Level}: approver reassigned from user {OldUserId} to {NewUserId} ({Reason})",
            row.jobmasterid, row.wlevel, oldUserId, newUserId, reason);
    }

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
        return await context.job_user_lists
            .Include(a => a.jobmaster).ThenInclude(j => j.workflow)
            .Where(IsLiveApprovalRow)
            .Where(a => a.userid == userId)
            .OrderBy(a => a.jobmaster.createdate)
            .ToListAsync(ct);
    }

    public async Task<List<PoolInboxRow>> GetMyPoolInboxAsync(long userId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
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
        var claimants = (await _users.GetUsersAsync(claimantIds, ct)).ToDictionary(u => u.UserId, u => u.FullName);

        return poolRows.Select(a =>
        {
            var live = IsPoolClaimLive(a.jobmaster);
            var claimedBy = live ? a.jobmaster.PoolClaimedByUserId : null;
            return new PoolInboxRow(
                a,
                IsClaimedByMe: claimedBy == userId,
                ClaimedByName: claimedBy is long id ? claimants.GetValueOrDefault(id, $"#{id}") : null,
                ClaimedDate: live ? a.jobmaster.PoolClaimedDate : null);
        }).ToList();
    }

    private static bool IsPoolClaimLive(job_master job) =>
        job.PoolClaimedByUserId is not null
        && job.PoolClaimedWLevel == job.lastLevel
        && (job.PoolClaimedJobSeq ?? 0) == (job.jobseq ?? 0);

    public async Task ClaimPoolJobAsync(long jobMasterId, long actorUserId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var job = await context.job_masters.FirstOrDefaultAsync(j => j.jobmasterid == jobMasterId, ct)
            ?? throw new InvalidOperationException("ไม่พบงานนี้แล้ว");
        if (job.isJobClosed == true)
            throw new InvalidOperationException("งานนี้ปิดแล้ว ไม่สามารถรับงานได้");

        if (IsPoolClaimLive(job))
        {
            if (job.PoolClaimedByUserId == actorUserId) return;
            throw new InvalidOperationException("มีเพื่อนร่วมทีมรับงานนี้ไปแล้ว");
        }

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
    }

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
    }

    public async Task<List<job_user_list>> GetMyInvolvementAsync(long userId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.job_user_lists
            .Include(a => a.jobmaster).ThenInclude(j => j.workflow)
            .Where(a => a.userid == userId)
            .OrderByDescending(a => a.jobmaster.createdate).ThenByDescending(a => a.jobapproverid)
            .Take(300)
            .ToListAsync(ct);
    }

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

    public async Task<List<job_master>> GetMyRequestsAsync(long requesterUserId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.job_masters
            .Include(j => j.workflow)
            .Where(j => j.createuserid == requesterUserId)
            .OrderByDescending(j => j.createdate)
            .ToListAsync(ct);
    }

    public async Task<List<job_master>> SearchJobsAsync(string? workflowCode = null, bool? isClosed = null,
        long? requesterUserId = null, DateTime? fromDate = null, DateTime? toDate = null, string? searchText = null,
        IEnumerable<long>? requesterUserIds = null, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var query = context.job_masters.Include(j => j.workflow).AsQueryable();

        if (!string.IsNullOrWhiteSpace(workflowCode)) query = query.Where(j => j.workflowcode == workflowCode);
        if (isClosed is bool closed) query = query.Where(j => j.isJobClosed == closed);
        if (requesterUserId is long uid) query = query.Where(j => j.createuserid == uid);
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

    // Pure level-approval decision — logic copied verbatim from the original
    // (public static so it stays independently unit-testable, same rationale
    // as the HRM original's own comment on this method).
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
                return LevelOutcome.Failed;

            return LevelOutcome.StillPending;
        }

        if (snapshot.isorcondition)
        {
            if (rows.Any(IsApprovedLike))
                return LevelOutcome.Complete;
            if (rows.All(IsRejected))
                return LevelOutcome.Failed;
            return LevelOutcome.StillPending;
        }

        if (rows.Any(IsRejected))
            return LevelOutcome.Failed;
        if (rows.Any(IsPending))
            return LevelOutcome.StillPending;
        return LevelOutcome.Complete;
    }

    public async Task<List<LevelApproverPlan>> GetApproverPlanAsync(long workflowId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var levels = await context.wf_sub_workflow_masters
            .Where(s => s.workflowid == workflowId)
            .OrderBy(s => s.wlevel)
            .ToListAsync(ct);
        if (levels.Count == 0) return new();

        var customUsers = await context.wf_custom_users
            .Where(u => u.workflowid == workflowId && u.isactive)
            .Select(u => new { u.wlevel, u.userid })
            .ToListAsync(ct);
        var userIds = customUsers.Select(u => u.userid).Distinct().ToList();
        var users = await _users.GetUsersAsync(userIds, ct);
        var nameByUserId = users.ToDictionary(u => u.UserId, u => u.FullName);

        var customRoles = await context.wf_custom_roles
            .Where(r => r.workflowid == workflowId && r.isactive == true)
            .Select(r => new { r.wlevel, r.roleid })
            .ToListAsync(ct);
        // TODO(seam): the original resolves role NAMES from db.sc_roles.
        // IWorkflowUserDirectory has no role-name lookup (only membership),
        // so the plan text below falls back to "role #<id>" — add a
        // GetRoleNameAsync-style member if the display text matters before
        // cutover (see EXTRACTION-PLAN.md).

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
                    .Select(r => $"role #{r.roleid}")
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
        var nameByUser = (await _users.GetUsersAsync(userIds, ct)).ToDictionary(u => u.UserId, u => u.FullName);

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
        var pending = await context.job_user_lists
            .Where(IsLiveApprovalRow)
            .Where(a => a.jobmasterid == jobMasterId)
            .Select(a => new { a.wlevel, a.userid })
            .ToListAsync(ct);
        if (pending.Count == 0) return "";

        var userIds = pending.Where(p => p.userid.HasValue).Select(p => p.userid!.Value).Distinct().ToList();
        var names = (await _users.GetUsersAsync(userIds, ct)).Select(u => u.FullName);
        return string.Join(" / ", names.Distinct());
    }
}
