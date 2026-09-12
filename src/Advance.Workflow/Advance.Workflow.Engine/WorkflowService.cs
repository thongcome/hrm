using Advance.Workflow.Contracts;
using Advance.Workflow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Advance.Workflow.Engine;

// ============================================================================
//  WorkflowService — ported from HRM's Services/Workflow/WorkflowService.cs
//  (1642 lines). See ../EXTRACTION-PLAN.md for the full list of what changed
//  and why; the short version:
//
//    HRMContext                          -> WorkflowDbContext (this project)
//    sc_user / db.sc_users                -> WorkflowUser / IWorkflowUserDirectory
//    Hremployee + EmployeeEmailResolver   -> IWorkflowUserDirectory (email) /
//                                             IOrgDirectorySource (org data)
//    EmailSender                          -> IWorkflowNotifier
//    HRM.Services.Audit.IAuditLogger      -> TODO(seam): dropped for Phase 0;
//                                             Serilog-only logging kept.
//                                             Advance.Platform (per the split
//                                             plan's level 0-1) is meant to own
//                                             audit/PDPA logging as a package —
//                                             wire that in before cutover, this
//                                             engine must not ship without it
//                                             (พ.ร.บ.คอมพิวเตอร์ compliance).
//    wf_sub_workflow_master.GetUserAsync/
//    GetUserBySourceAsync (an extension    -> WorkflowApproverResolver
//    of the entity in HRM)                    (see that file's own header)
//
//  Every method name, every business rule comment, and the overall shape
//  (WorkFlowViewModel, the three WorkflowMove subclasses, MoveAsync/
//  MoveCoreAsync) are unchanged from the original — this is a mechanical
//  seam substitution, not a redesign. Where the substitution couldn't be
//  done cleanly, a TODO(seam) marks the spot instead of guessing.
// ============================================================================

public class WorkFlowViewModel
{
    public long jobmasterid { get; set; }
    public long actorUserId { get; set; }
    public string? reason { get; set; }
    public long? mas_reason_id { get; set; }
    public decimal? loaValue { get; set; }

    public string? direction { get; set; }

    public wf_workflow? wfWorkflow { get; set; }
    public job_master? jobMaster { get; set; }
    public wf_sub_workflow_master? subWorkflow { get; set; }
    public job_subworkflow_master? jobsub { get; set; }
    public List<job_user_list> jobUserList { get; set; } = new();
    public List<job_user_list> jobUserListPredict { get; set; } = new();
    public job_user_list? jobUserListSession { get; set; }
    public bool isCurrentUser { get; set; }
    public List<string> message { get; set; } = new();

    public bool canReturnToSender { get; set; }
    public string? sendBackToName { get; set; }
    public int? sendBackToLevel { get; set; }

    public bool canView { get; set; }

    public bool canDecline { get; set; }
    public bool canCancel { get; set; }
    public bool isDraftHolder { get; set; }
    public bool canDeleteDraft { get; set; }
    public string? editUrl { get; set; }

    public bool isPoolLevel { get; set; }
    public bool poolClaimedByMe { get; set; }
    public string? poolClaimedByName { get; set; }
    public List<job_user_list> poolMembers { get; set; } = new();
}

public abstract class WorkflowMove
{
    public abstract string Name { get; }
    public abstract int Delta { get; }
    public abstract string? StampOn(wf_sub_workflow_master target);
    public abstract string ActorRowStatus { get; }

    public abstract Task<List<WorkflowUser>> RecipientsAsync(
        WorkflowDbContext db, WorkflowApproverResolver resolver, IWorkflowUserDirectory users,
        job_master job, wf_sub_workflow_master target, CancellationToken ct);

    public virtual bool LeavesLevel => true;
    public virtual bool ClosesJob(wf_sub_workflow_master target) => false;

    public static readonly WorkflowMove Forward = new ForwardMove();
    public static readonly WorkflowMove Backward = new BackwardMove();
    public static readonly WorkflowMove Stand = new StandMove();
}

public sealed class ForwardMove : WorkflowMove
{
    public override string Name => "Submit";
    public override int Delta => +1;
    public override string ActorRowStatus => WorkflowService.Approved;

    public override string? StampOn(wf_sub_workflow_master target) => target.sitinstatus ?? target.standstatus;

    public override Task<List<WorkflowUser>> RecipientsAsync(
        WorkflowDbContext db, WorkflowApproverResolver resolver, IWorkflowUserDirectory users,
        job_master job, wf_sub_workflow_master target, CancellationToken ct)
        => WorkflowService.GetUserRelateAsync(resolver, users, job, target, ct);
}

public class BackwardMove : WorkflowMove
{
    public override string Name => "Reject";
    public override int Delta => -1;
    public override string ActorRowStatus => WorkflowService.StatusReturned;

    public override string? StampOn(wf_sub_workflow_master target) => target.backwardstatus;

    public override Task<List<WorkflowUser>> RecipientsAsync(
        WorkflowDbContext db, WorkflowApproverResolver resolver, IWorkflowUserDirectory users,
        job_master job, wf_sub_workflow_master target, CancellationToken ct)
        => WorkflowService.GetWhoSentAsync(db, users, job, target.wlevel, ct);
}

public sealed class ReturnToSenderMove : BackwardMove
{
    private readonly int _creatorLevel;
    public ReturnToSenderMove(int creatorLevel, int fromLevel)
    {
        _creatorLevel = creatorLevel;
        Jump = creatorLevel - fromLevel;
    }

    public int Jump { get; }
    public override string Name => "ReturnToSender";
    public override int Delta => Jump;

    public override async Task<List<WorkflowUser>> RecipientsAsync(
        WorkflowDbContext db, WorkflowApproverResolver resolver, IWorkflowUserDirectory users,
        job_master job, wf_sub_workflow_master target, CancellationToken ct)
    {
        if (job.createuserid is not long creator) return new();
        var u = await users.GetUserAsync(creator, ct);
        return u is null ? new() : new List<WorkflowUser> { u };
    }
}

public sealed class StandMove : WorkflowMove
{
    public override string Name => "Approve";
    public override int Delta => 0;
    public override string ActorRowStatus => WorkflowService.Approved;
    public override bool LeavesLevel => false;

    public override string? StampOn(wf_sub_workflow_master target) => target.istop ? target.forwardstatus : target.standstatus;
    public override bool ClosesJob(wf_sub_workflow_master target) => target.istop;

    public override Task<List<WorkflowUser>> RecipientsAsync(
        WorkflowDbContext db, WorkflowApproverResolver resolver, IWorkflowUserDirectory users,
        job_master job, wf_sub_workflow_master target, CancellationToken ct)
        => Task.FromResult(new List<WorkflowUser>());
}

public class WorkflowService
{
    public const string Pending = WorkflowEngineService.StatusPending;
    public const string Approved = WorkflowEngineService.StatusApproved;
    public const string Rejected = WorkflowEngineService.StatusRejected;
    public const string StatusReturned = WorkflowEngineService.StatusReturned;
    public const string Completed = WorkflowEngineService.StatusCompleted;

    private readonly IDbContextFactory<WorkflowDbContext> _dbFactory;
    private readonly IWorkflowNotifier _notifier;
    private readonly IWorkflowUserDirectory _users;
    private readonly IOrgDirectorySource _org;
    private readonly IEnumerable<IWorkflowDocumentHandler> _handlers;

    public WorkflowService(IDbContextFactory<WorkflowDbContext> dbFactory, IWorkflowNotifier notifier,
        IWorkflowUserDirectory users, IOrgDirectorySource org, IEnumerable<IWorkflowDocumentHandler> handlers)
    {
        _dbFactory = dbFactory;
        _notifier = notifier;
        _users = users;
        _org = org;
        _handlers = handlers;
    }

    private WorkflowApproverResolver MakeResolver(WorkflowDbContext db) => new(db, _org, _users);

    // TODO(seam): the original dispatches to WorkflowDocumentWriteback, a
    // scoped HRM service that resolves the right IWorkflowDocumentHandler by
    // job.reftable and calls it via DI (see Services/Workflow/
    // WorkflowDocumentWriteback.cs — the interface it uses is unchanged and
    // now lives in Advance.Workflow.Contracts). Ported inline here since this
    // project has no IServiceProvider-based handler dispatcher of its own yet.
    private async Task WriteBackAsync(job_master job, CancellationToken ct)
    {
        if (job.isJobClosed != true) return;
        if (string.IsNullOrWhiteSpace(job.reftable) || string.IsNullOrWhiteSpace(job.refid)) return;

        var handler = _handlers.FirstOrDefault(h => string.Equals(h.RefTable, job.reftable, StringComparison.OrdinalIgnoreCase));
        if (handler is null) return; // module has no write-back handler registered — nothing to do.

        var e = new WorkflowClosedEvent(job.jobmasterid, job.workflowcode ?? "", job.reftable!, job.refid!,
            WorkflowDocumentWritebackOutcome(job.reasonClosed), job.approvedUserID, job.remark);
        try
        {
            // TODO(seam): the original passes IServiceProvider so a handler can
            // resolve scoped module services (e.g. TimesheetService). This
            // project has no host DI container to hand it — pass null and let
            // handlers that need one throw until a real host wires this up.
            await handler.OnClosedAsync(null!, e, ct);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Job {Job}: write-back to {RefTable} #{RefId} failed", job.jobmasterid, job.reftable, job.refid);
        }
    }

    private static WorkflowCloseOutcome WorkflowDocumentWritebackOutcome(string? reasonClosed) => reasonClosed switch
    {
        WorkflowEngineService.ClosedByDecline => WorkflowCloseOutcome.Declined,
        WorkflowEngineService.ClosedByCancel => WorkflowCloseOutcome.Cancelled,
        _ => WorkflowCloseOutcome.Approved,
    };

    private async Task NotifyRecipientsAsync(WorkflowDbContext db, job_master job,
        List<job_user_list> rows, wf_sub_workflow_master? level, CancellationToken ct)
    {
        foreach (var row in rows)
        {
            try
            {
                var empNo = row.empid;
                if (string.IsNullOrWhiteSpace(empNo) && row.userid is long uid)
                {
                    var u = await _users.GetUserAsync(uid, ct);
                    empNo = u?.EmpId;
                }
                if (string.IsNullOrWhiteSpace(empNo)) continue;

                var emp = await _org.GetEmployeeAsync(empNo, ct);
                if (emp is null || string.IsNullOrWhiteSpace(emp.Email)) continue;

                var stepName = level?.subject ?? $"ขั้นที่ {row.wlevel}";
                var subject = $"มีงานรอคุณดำเนินการ: {job.subject ?? job.wname}";
                var body = $"<p>งาน \"{job.subject}\" ({job.wname}) มาถึงขั้น <b>{stepName}</b> และรอคุณดำเนินการ</p>"
                         + (string.IsNullOrWhiteSpace(job.reqName) ? "" : $"<p>ผู้ขอ: {job.reqName}</p>")
                         + (job.reqamont is null ? "" : $"<p>จำนวนเงิน: {job.reqamont:N2} บาท</p>");
                await _notifier.SendAsync(emp.Email, subject, body, ct);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "แจ้งเตือนผู้รับงานของงาน {JobMasterId} ไม่สำเร็จ", job.jobmasterid);
            }
        }
    }

    // TODO(seam): every call site below that used to call AuditAsync now only
    // logs to Serilog. This is a compliance gap for a real deployment (see
    // the file header) — HRM's own IAuditLogger wrote every workflow action to
    // AuditLog for the พ.ร.บ.คอมพิวเตอร์ retention requirement, and nothing
    // here replaces that yet.
    private static void AuditLog(job_master job, string action, object? detail) =>
        Serilog.Log.Information("Workflow audit (TODO seam, not persisted): job {JobMasterId} action {Action} {@Detail}",
            job.jobmasterid, action, detail);

    public Task<WorkFlowViewModel> SubmitAsync(WorkFlowViewModel m, CancellationToken ct = default)
        => MoveAsync(m, WorkflowMove.Forward, ct);

    public Task<WorkFlowViewModel> RejectOneStepAsync(WorkFlowViewModel m, CancellationToken ct = default)
        => MoveAsync(m, WorkflowMove.Backward, ct);

    public async Task<WorkFlowViewModel> ActAsync(WorkFlowViewModel m, string actionKind, CancellationToken ct = default)
    {
        var cur = await DetailAsync(m.jobmasterid, m.actorUserId, ct);
        try
        {
            return actionKind switch
            {
                WorkflowButtonService.ActionSendBack when cur.canReturnToSender => await ReturnToSenderAsync(m, ct),
                WorkflowButtonService.ActionSendBack => await RejectOneStepAsync(m, ct),
                WorkflowButtonService.ActionDecline => await DeclineAsync(m, ct),
                _ when (cur.jobMaster?.lastLevel ?? 0) <= DraftLevel => await SubmitAsync(m, ct),
                _ => await ApproveAsync(m, ct),
            };
        }
        catch (DbUpdateConcurrencyException ex) { throw ConcurrentActionError(ex); }
    }

    internal static InvalidOperationException ConcurrentActionError(Exception inner)
        => new("มีผู้ดำเนินการงานนี้พร้อมกับคุณ ระบบบันทึกผลของคนที่กดก่อน กรุณาโหลดหน้าใหม่แล้วดูสถานะล่าสุด", inner);

    public async Task<WorkFlowViewModel> DeclineAsync(WorkFlowViewModel m, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var job = await db.job_masters.FirstOrDefaultAsync(j => j.jobmasterid == m.jobmasterid, ct)
            ?? throw new InvalidOperationException($"ไม่พบงาน id {m.jobmasterid}");
        if (job.isJobClosed == true) throw new InvalidOperationException("งานนี้ปิดแล้ว");

        var level = job.lastLevel ?? 0;
        var target = await db.wf_sub_workflow_masters
            .FirstOrDefaultAsync(s => s.workflowid == job.workflowid && s.wlevel == level, ct)
            ?? throw new InvalidOperationException($"ไม่พบการตั้งค่าของขั้นที่ {level}");
        if (!target.istop)
            throw new InvalidOperationException(
                "\"ไม่อนุมัติ\" ใช้ได้เฉพาะขั้นสุดท้าย — ขั้นนี้ยังไม่ใช่ผู้ตัดสินสุดท้าย ถ้าไม่เห็นด้วยให้ใช้ \"ส่งกลับ\" แทน");

        var rows = await db.job_user_lists
            .Where(a => a.jobmasterid == job.jobmasterid && a.wlevel == level && a.isLast == true)
            .ToListAsync(ct);
        var mine = rows.FirstOrDefault(a => a.userid == m.actorUserId
            && string.Equals(a.jobstatus, Pending, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("งานนี้ไม่ได้อยู่ที่คุณ");

        mine.jobstatus = Rejected;
        mine.approvedate = DateTime.Now;
        mine.reason = m.reason;
        mine.comment = m.reason;
        mine.mas_reason_id = m.mas_reason_id;
        foreach (var r in rows) r.isLast = false;

        var actor = await _users.GetUserAsync(m.actorUserId, ct);
        var stamp = CreateJobSubWorkflow(job, target);
        stamp.reason = m.reason;
        stamp.modby = actor?.FullName;
        stamp.remark = $"Decline {level}";
        stamp.endtime = DateTime.Now;
        db.job_subworkflow_masters.Add(stamp);
        job.jobseq = stamp.jobseq;

        job.status = target.declinestatus ?? target.backwardstatus ?? Rejected;
        job.isJobClosed = true;
        job.reasonClosed = WorkflowEngineService.ClosedByDecline;
        job.remark = m.reason;
        job.approvedDate = DateTime.Now;
        job.approvedUserID = m.actorUserId;
        job.enddate = DateTime.Now;

        await db.SaveChangesAsync(ct);
        AuditLog(job, "Decline", new { level, m.reason });
        await WriteBackAsync(job, ct);
        await NoticeEveryoneInvolvedAsync(db, job, m.actorUserId, m.reason, ct, declined: true);

        m.jobMaster = job; m.subWorkflow = target; m.jobsub = stamp;
        m.direction = "Decline";
        return m;
    }

    public async Task DeleteDraftAsync(long jobMasterId, long actorUserId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var job = await db.job_masters.FirstOrDefaultAsync(j => j.jobmasterid == jobMasterId, ct)
            ?? throw new InvalidOperationException($"ไม่พบงาน id {jobMasterId}");
        if (job.createuserid != actorUserId)
            throw new InvalidOperationException("ลบร่างได้เฉพาะผู้สร้างงานเองเท่านั้น");
        if (job.isJobClosed == true || (job.lastLevel ?? 0) != DraftLevel)
            throw new InvalidOperationException("งานนี้เข้าเส้นทางอนุมัติแล้ว ลบไม่ได้ — ใช้ \"ยกเลิกคำขอ\" แทน");
        if (await db.job_user_lists.AnyAsync(a => a.jobmasterid == jobMasterId && a.wlevel > DraftLevel, ct))
            throw new InvalidOperationException("งานนี้เคยวิ่งเข้าเส้นทางแล้ว ลบไม่ได้ — ใช้ \"ยกเลิกคำขอ\" แทน");

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.wf_adhoc_users.Where(a => a.jobmasterid == jobMasterId).ExecuteDeleteAsync(ct);
        await db.job_loas.Where(l => l.jobmasterid == jobMasterId).ExecuteDeleteAsync(ct);
        await db.job_user_lists.Where(a => a.jobmasterid == jobMasterId).ExecuteDeleteAsync(ct);
        await db.job_subworkflow_masters.Where(s => s.jobmasterid == jobMasterId).ExecuteDeleteAsync(ct);
        db.job_masters.Remove(job);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        AuditLog(job, "DeleteDraft", new { job.workflowcode, job.reftable, job.refid });
    }

    public async Task<WorkFlowViewModel> CancelAsync(WorkFlowViewModel m, bool isAdminOverride = false,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var job = await db.job_masters.FirstOrDefaultAsync(j => j.jobmasterid == m.jobmasterid, ct)
            ?? throw new InvalidOperationException($"ไม่พบงาน id {m.jobmasterid}");
        if (job.isJobClosed == true) throw new InvalidOperationException("งานนี้ปิดแล้ว");
        if (!isAdminOverride && job.createuserid != m.actorUserId)
            throw new InvalidOperationException("ยกเลิกได้เฉพาะผู้ยื่นคำขอเองเท่านั้น");

        var level = job.lastLevel ?? 0;
        var target = await db.wf_sub_workflow_masters
            .FirstOrDefaultAsync(s => s.workflowid == job.workflowid && s.wlevel == level, ct);

        foreach (var r in await db.job_user_lists
            .Where(a => a.jobmasterid == job.jobmasterid && a.jobstatus == Pending).ToListAsync(ct))
        {
            r.jobstatus = WorkflowEngineService.StatusCancelled;
            r.approvedate = DateTime.Now;
            r.comment = m.reason;
            r.isLast = false;
        }

        var actor = await _users.GetUserAsync(m.actorUserId, ct);
        var stamp = target is null
            ? DraftFootprint(job, actor?.FullName, $"Cancel {level}")
            : CreateJobSubWorkflow(job, target);
        if (target is not null)
        {
            stamp.modby = actor?.FullName;
            stamp.remark = $"Cancel {level}";
        }
        stamp.reason = m.reason;
        stamp.endtime = DateTime.Now;
        db.job_subworkflow_masters.Add(stamp);
        job.jobseq = stamp.jobseq;

        job.status = WorkflowEngineService.StatusCancelled;
        job.isJobClosed = true;
        job.reasonClosed = WorkflowEngineService.ClosedByCancel;
        job.remark = m.reason;
        job.enddate = DateTime.Now;

        await db.SaveChangesAsync(ct);
        AuditLog(job, "Cancel", new { level, m.reason, isAdminOverride });
        await WriteBackAsync(job, ct);

        m.jobMaster = job; m.jobsub = stamp;
        m.direction = "Cancel";
        return m;
    }

    public async Task<WorkFlowViewModel> AutoApproveAsync(WorkFlowViewModel m, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var job = await db.job_masters.FirstOrDefaultAsync(j => j.jobmasterid == m.jobmasterid, ct)
            ?? throw new InvalidOperationException($"ไม่พบงาน id {m.jobmasterid}");
        if (job.isJobClosed == true) { m.jobMaster = job; return m; }

        var stamp = DraftFootprint(job, "ระบบ", "AutoApprove -> ปิดงาน");
        stamp.endtime = DateTime.Now;
        job.jobseq = stamp.jobseq;
        db.job_subworkflow_masters.Add(stamp);

        job.status = Completed;
        job.isJobClosed = true;
        job.reasonClosed = WorkflowEngineService.ClosedByAutoApprove;
        job.remark = "อนุมัติอัตโนมัติ (workflow ตั้งค่า isautoapprove)";
        job.approvedDate = DateTime.Now;
        job.enddate = DateTime.Now;

        foreach (var row in await db.job_user_lists
            .Where(a => a.jobmasterid == job.jobmasterid && a.jobstatus == Pending).ToListAsync(ct))
        {
            row.jobstatus = Approved;
            row.approvedate = DateTime.Now;
            row.isLast = false;
        }

        await db.SaveChangesAsync(ct);
        AuditLog(job, "AutoApprove", new { job.workflowcode });
        await WriteBackAsync(job, ct);
        await NotifyRequesterClosedAsync(job, ct);

        m.jobMaster = job;
        m.jobsub = stamp;
        return m;
    }

    public async Task<WorkFlowViewModel> ReturnToSenderAsync(WorkFlowViewModel m, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var job = await db.job_masters.FirstOrDefaultAsync(j => j.jobmasterid == m.jobmasterid, ct)
            ?? throw new InvalidOperationException("ไม่พบงาน");
        var from = job.lastLevel ?? 0;

        var here = await db.wf_sub_workflow_masters
            .FirstOrDefaultAsync(s => s.workflowid == job.workflowid && s.wlevel == from, ct);
        if (here?.isReturnSender != true)
            throw new InvalidOperationException("ขั้นนี้ไม่ได้เปิดสิทธิ์ส่งกลับหาผู้กรอกแบบฟอร์ม (isReturnSender)");

        if (from <= DraftLevel)
            throw new InvalidOperationException("งานอยู่ที่ผู้กรอกแบบฟอร์มอยู่แล้ว ส่งกลับไม่ได้");

        return await MoveAsync(m, new ReturnToSenderMove(DraftLevel, from), ct);
    }

    public Task<WorkFlowViewModel> ApproveAsync(WorkFlowViewModel m, CancellationToken ct = default)
        => MoveAsync(m, WorkflowMove.Stand, ct);

    public const int DraftLevel = 0;
    public const string StatusDraft = "DRAFT";

    public async Task<WorkFlowViewModel> CreateAsync(
        long workflowid, long actorUserId, string? empid, string? subject,
        string? reftable, string? refid, decimal? amount, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var wf = await db.wf_workflows.FirstOrDefaultAsync(w => w.workflowid == workflowid, ct)
            ?? throw new InvalidOperationException($"ไม่พบ workflow id {workflowid}");
        if (wf.isactive != true)
            throw new InvalidOperationException($"workflow '{wf.wname}' ปิดใช้งานอยู่ ไม่สามารถเริ่มงานใหม่ได้");

        var levelCount = await db.wf_sub_workflow_masters.CountAsync(s => s.workflowid == workflowid, ct);
        if (levelCount == 0)
            throw new InvalidOperationException($"workflow {wf.workflowcode} ยังไม่มีเส้นทางเดิน (wf_sub_workflow_master ว่าง)");

        var user = await _users.GetUserAsync(actorUserId, ct);
        var fullName = user?.FullName ?? "";

        // TODO(seam): original reads Hremployee.orgcode/CostCenterCode for the
        // subject employee (CEO note: sc_user.orgcode is empty for ~7,005/7,006
        // users, so the supervisor chain must anchor on Hremployee, not
        // sc_user). Ported against IOrgDirectorySource.GetEmployeeAsync, which
        // exposes OrgCode but not CostCenterCode yet — see EXTRACTION-PLAN.md.
        var subjectEmpNo = empid ?? user?.EmpId;
        var subjectEmp = string.IsNullOrWhiteSpace(subjectEmpNo) ? null : await _org.GetEmployeeAsync(subjectEmpNo, ct);
        var subjectName = subjectEmp?.FullName ?? fullName;

        var job = new job_master
        {
            workflowid = workflowid,
            workflowcode = wf.workflowcode,
            wname = wf.wname,
            subject = subject,
            maxlevel = levelCount,
            lastLevel = DraftLevel,
            status = StatusDraft,
            reftable = reftable,
            refid = refid,
            createuserid = actorUserId,
            empid = subjectEmpNo,
            createusername = fullName,
            reqName = subjectName,
            reqOrg = subjectEmp?.OrgCode,
            createdate = DateTime.Now,
            reqdate = DateTime.Now,
            reqamont = amount,
            isactive = true,
            isJobClosed = false,
            jobseq = 0,
        };
        db.job_masters.Add(job);
        await db.SaveChangesAsync(ct);

        var jobSub = DraftFootprint(job, fullName, "Create -> 0");
        job.jobseq = jobSub.jobseq;
        db.job_subworkflow_masters.Add(jobSub);

        var model = new WorkFlowViewModel
        {
            jobmasterid = job.jobmasterid,
            actorUserId = actorUserId,
            direction = WorkflowMove.Forward.Name,
            jobMaster = job,
            wfWorkflow = wf,
            subWorkflow = null,
            jobsub = jobSub,
        };

        model.jobUserList = new List<job_user_list>
        {
            new()
            {
                jobmasterid = job.jobmasterid,
                workflowid = job.workflowid,
                wlevel = DraftLevel,
                userid = actorUserId,
                empid = job.empid,
                username = fullName,
                orgcode = subjectEmp?.OrgCode ?? user?.OrgCode,
                jobstatus = Pending,
                jobseq = job.jobseq,
                isLast = true,
                sendDate = DateTime.Now,
                recievedate = DateTime.Now,
                isAutoApprove = false,
                moddate = DateTime.Now,
            }
        };
        db.job_user_lists.AddRange(model.jobUserList);

        await db.SaveChangesAsync(ct);
        AuditLog(job, "Create", new { reftable, refid, amount });
        return model;
    }

    private async Task<WorkFlowViewModel> ReturnToDraftAsync(
        WorkflowDbContext db, WorkFlowViewModel model, job_master job, WorkflowMove move, int fromLevel, CancellationToken ct)
    {
        if (job.createuserid is not long creator)
            throw new InvalidOperationException("งานนี้ไม่มีผู้สร้าง จึงส่งกลับไม่ได้");

        var owner = await _users.GetUserAsync(creator, ct)
            ?? throw new InvalidOperationException("ไม่พบผู้สร้างงาน");
        var actor = await _users.GetUserAsync(model.actorUserId, ct);

        var leaving = await CurrentFootprintAsync(db, job.jobmasterid, fromLevel, ct);
        if (leaving is not null) leaving.endtime = DateTime.Now;

        foreach (var row in await db.job_user_lists
            .Where(a => a.jobmasterid == job.jobmasterid && a.wlevel == fromLevel && a.isLast == true).ToListAsync(ct))
        {
            if (row.userid == model.actorUserId)
            {
                row.jobstatus = move.ActorRowStatus;
                row.approvedate = DateTime.Now;
                row.reason = model.reason;
                row.comment = model.reason;
                row.mas_reason_id = model.mas_reason_id;
            }
            row.isLast = false;
        }

        job.lastLevel = DraftLevel;
        job.status = StatusReturned;
        job.remark = model.reason;

        var jobSub = DraftFootprint(job, actor?.FullName, $"{move.Name} {fromLevel} -> 0");
        jobSub.reason = model.reason;
        job.jobseq = jobSub.jobseq;
        db.job_subworkflow_masters.Add(jobSub);

        var back = new job_user_list
        {
            jobmasterid = job.jobmasterid,
            workflowid = job.workflowid,
            wlevel = DraftLevel,
            userid = owner.UserId,
            empid = owner.EmpId,
            username = owner.FullName,
            orgcode = job.reqOrg ?? owner.OrgCode,
            jobstatus = Pending,
            jobseq = job.jobseq,
            isLast = true,
            sendDate = DateTime.Now,
            recievedate = DateTime.Now,
            reason = model.reason,
            isAutoApprove = false,
            moddate = DateTime.Now,
        };
        db.job_user_lists.Add(back);

        model.direction = move.Name;
        model.jobMaster = job;
        model.subWorkflow = null;
        model.jobsub = jobSub;
        model.jobUserList = new List<job_user_list> { back };

        await db.SaveChangesAsync(ct);
        AuditLog(job, move.Name, new { from = fromLevel, to = DraftLevel, model.reason });
        await NotifyRecipientsAsync(db, job, new List<job_user_list> { back }, null, ct);

        if (move is ReturnToSenderMove)
            await NoticeEveryoneInvolvedAsync(db, job, model.actorUserId, model.reason, ct);

        return model;
    }

    private static job_subworkflow_master DraftFootprint(job_master job, string? actor, string remark)
        => new()
        {
            jobmasterid = job.jobmasterid,
            workflowid = job.workflowid,
            wlevel = DraftLevel,
            wfcode = job.workflowcode,
            jobseq = (job.jobseq ?? 0) + 1,
            subject = "ผู้กรอกแบบฟอร์ม (draft)",
            status = StatusDraft,
            istop = false,
            starttime = DateTime.Now,
            moddate = DateTime.Now,
            modby = actor,
            remark = remark,
        };

    public async Task<WorkFlowViewModel> DetailAsync(long id, long actorUserId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var model = new WorkFlowViewModel { jobmasterid = id, actorUserId = actorUserId };

        var job = await db.job_masters.FirstOrDefaultAsync(j => j.jobmasterid == id, ct);
        if (job is null) { model.message.Add("ไม่พบงาน"); return model; }

        var currentlevel = job.lastLevel ?? 0;
        model.jobMaster = job;
        model.wfWorkflow = await db.wf_workflows.FirstOrDefaultAsync(w => w.workflowid == job.workflowid, ct);
        model.subWorkflow = await db.wf_sub_workflow_masters
            .FirstOrDefaultAsync(s => s.workflowid == job.workflowid && s.wlevel == currentlevel, ct);
        model.jobsub = await CurrentFootprintAsync(db, job.jobmasterid, currentlevel, ct);

        model.jobUserList = await db.job_user_lists
            .Where(a => a.jobmasterid == id)
            .OrderBy(a => a.jobseq).ThenBy(a => a.jobapproverid)
            .ToListAsync(ct);

        model.jobUserListSession = model.jobUserList.FirstOrDefault(a =>
            a.userid == actorUserId && a.isLast == true && a.wlevel == currentlevel
            && string.Equals(a.jobstatus, Pending, StringComparison.OrdinalIgnoreCase));
        model.isCurrentUser = model.jobUserListSession is not null && job.isJobClosed != true;

        model.canView = job.createuserid == actorUserId
                     || model.jobUserList.Any(a => a.userid == actorUserId);

        model.canDecline = model.isCurrentUser && model.subWorkflow?.istop == true;
        model.canCancel = job.isJobClosed != true && job.createuserid == actorUserId;

        model.isDraftHolder = model.isCurrentUser && currentlevel == DraftLevel;
        model.canDeleteDraft = model.isDraftHolder && job.createuserid == actorUserId
            && !model.jobUserList.Any(a => a.wlevel > DraftLevel);
        if (model.isDraftHolder && !string.IsNullOrWhiteSpace(job.refid))
        {
            var route = await db.wf_sub_workflow_masters
                .Where(s => s.workflowid == job.workflowid && s.controller != null)
                .OrderBy(s => s.wlevel).Select(s => s.controller).FirstOrDefaultAsync(ct);
            if (!string.IsNullOrWhiteSpace(route))
                model.editUrl = route.Replace("{refid}", job.refid, StringComparison.OrdinalIgnoreCase);
        }

        if (model.subWorkflow?.isPool == true)
        {
            model.isPoolLevel = true;
            model.poolMembers = model.jobUserList
                .Where(a => a.wlevel == currentlevel && (a.jobseq ?? 0) == (job.jobseq ?? 0))
                .OrderBy(a => a.username).ToList();
        }

        if (model.subWorkflow?.isPool == true && model.isCurrentUser)
        {
            model.poolClaimedByMe = job.PoolClaimedByUserId == actorUserId
                                 && job.PoolClaimedWLevel == currentlevel
                                 && (job.PoolClaimedJobSeq ?? 0) == (job.jobseq ?? 0);
            model.poolClaimedByName = job.PoolClaimedByUserId is long pc && !model.poolClaimedByMe
                ? (await _users.GetUserAsync(pc, ct))?.FullName
                : null;
            if (!model.poolClaimedByMe) model.isCurrentUser = false;
        }

        if (model.isCurrentUser && model.subWorkflow is not null)
        {
            var firstLevel = await db.wf_sub_workflow_masters
                .Where(s => s.workflowid == job.workflowid).MinAsync(s => s.wlevel, ct);

            if (model.subWorkflow.isReturnSender && currentlevel > DraftLevel)
            {
                model.canReturnToSender = true;
                model.sendBackToLevel = DraftLevel;
                model.sendBackToName = job.createusername ?? job.reqName;
            }
            else if (currentlevel > firstLevel)
            {
                var senders = await GetWhoSentAsync(db, _users, job, currentlevel - 1, ct);
                if (senders.Count > 0)
                {
                    model.sendBackToLevel = currentlevel - 1;
                    model.sendBackToName = string.Join(", ", senders.Select(u => u.FullName));
                }
            }
        }

        if (job.isJobClosed != true && model.subWorkflow?.istop != true)
        {
            var next = await db.wf_sub_workflow_masters
                .FirstOrDefaultAsync(s => s.workflowid == job.workflowid && s.wlevel == currentlevel + 1, ct);
            if (next is not null)
            {
                var users = await GetUserRelateAsync(MakeResolver(db), _users, job, next, ct);
                model.jobUserListPredict = users.Select(u => new job_user_list
                {
                    wlevel = next.wlevel,
                    userid = u.UserId,
                    empid = u.EmpId,
                    username = u.FullName,
                }).ToList();
            }
        }

        return model;
    }

    public async Task ClaimPoolAsync(long jobmasterid, long actorUserId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var job = await db.job_masters.FirstOrDefaultAsync(j => j.jobmasterid == jobmasterid, ct)
            ?? throw new InvalidOperationException("ไม่พบงาน");

        var level = job.lastLevel ?? 0;
        var mine = await db.job_user_lists.AnyAsync(a =>
            a.jobmasterid == jobmasterid && a.wlevel == level && a.userid == actorUserId
            && a.isLast == true && a.jobstatus == Pending, ct);
        if (!mine) throw new InvalidOperationException("งานนี้ไม่ได้อยู่ในกลุ่มของคุณ");

        if (job.PoolClaimedByUserId is long other && other != actorUserId
            && job.PoolClaimedWLevel == level && (job.PoolClaimedJobSeq ?? 0) == (job.jobseq ?? 0))
            throw new InvalidOperationException("งานนี้ถูกรับไปแล้วโดยคนอื่น");

        job.PoolClaimedByUserId = actorUserId;
        job.PoolClaimedWLevel = level;
        job.PoolClaimedJobSeq = job.jobseq;
        job.PoolClaimedDate = DateTime.Now;
        await db.SaveChangesAsync(ct);
    }

    public async Task ReleasePoolAsync(long jobmasterid, long actorUserId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var job = await db.job_masters.FirstOrDefaultAsync(j => j.jobmasterid == jobmasterid, ct)
            ?? throw new InvalidOperationException("ไม่พบงาน");
        if (job.PoolClaimedByUserId != actorUserId)
            throw new InvalidOperationException("คุณไม่ใช่คนที่รับงานนี้ไว้");

        job.PoolClaimedByUserId = null;
        job.PoolClaimedWLevel = null;
        job.PoolClaimedJobSeq = null;
        job.PoolClaimedDate = null;
        await db.SaveChangesAsync(ct);
    }

    private async Task<WorkFlowViewModel> MoveAsync(WorkFlowViewModel model, WorkflowMove move, CancellationToken ct,
        bool internalHop = false, WorkflowDbContext? sharedDb = null)
    {
        if (sharedDb is not null)
            return await MoveCoreAsync(sharedDb, model, move, ct, internalHop);

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var result = await MoveCoreAsync(db, model, move, ct, internalHop);
        await tx.CommitAsync(ct);

        if (result.jobMaster?.isJobClosed == true)
        {
            await WriteBackAsync(result.jobMaster, ct);
            await NotifyRequesterClosedAsync(result.jobMaster, ct);
        }
        return result;
    }

    private async Task NotifyRequesterClosedAsync(job_master job, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(job.empid)) return;
            var emp = await _org.GetEmployeeAsync(job.empid, ct);
            if (emp is null || string.IsNullOrWhiteSpace(emp.Email)) return;

            var outcome = job.reasonClosed switch
            {
                WorkflowEngineService.ClosedByDecline => "ไม่ได้รับการอนุมัติ",
                WorkflowEngineService.ClosedByCancel => "ถูกยกเลิก",
                _ => "ได้รับการอนุมัติเรียบร้อยแล้ว",
            };
            await _notifier.SendAsync(emp.Email,
                $"ผลการอนุมัติ: {job.subject ?? job.wname}",
                $"<p>คำขอของคุณเรื่อง \"{job.subject}\" {outcome}</p><p>สถานะปัจจุบัน: {job.status}</p>"
                + (string.IsNullOrWhiteSpace(job.remark) ? "" : $"<p>หมายเหตุ: {job.remark}</p>"), ct);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "แจ้งผลปิดงาน {JobMasterId} ให้ผู้ยื่นไม่สำเร็จ", job.jobmasterid);
        }
    }

    private async Task<WorkFlowViewModel> MoveCoreAsync(WorkflowDbContext db, WorkFlowViewModel model, WorkflowMove move,
        CancellationToken ct, bool internalHop)
    {
        var job = await db.job_masters.FirstOrDefaultAsync(j => j.jobmasterid == model.jobmasterid, ct)
            ?? throw new InvalidOperationException("ไม่พบงาน");
        if (job.isJobClosed == true) throw new InvalidOperationException("งานนี้ปิดแล้ว");

        var fromLevel = job.lastLevel ?? 0;
        var toLevel = fromLevel + move.Delta;

        var holdsLiveRow = internalHop || await db.job_user_lists.AnyAsync(a =>
            a.jobmasterid == job.jobmasterid && a.userid == model.actorUserId
            && a.wlevel == fromLevel && a.isLast == true && a.jobstatus == Pending, ct);
        if (!holdsLiveRow)
            throw new InvalidOperationException(
                "งานนี้ไม่ได้อยู่ในมือคุณแล้ว — อาจถูกดำเนินการไปแล้วหรือเลื่อนไปขั้นอื่น กรุณาโหลดหน้าใหม่");

        if (!internalHop && fromLevel > DraftLevel)
        {
            var hereIsPool = await db.wf_sub_workflow_masters
                .Where(s => s.workflowid == job.workflowid && s.wlevel == fromLevel)
                .Select(s => s.isPool).FirstOrDefaultAsync(ct);
            if (hereIsPool)
            {
                if (job.PoolClaimedByUserId is null || job.PoolClaimedWLevel != fromLevel)
                    throw new InvalidOperationException("ขั้นนี้เป็นงานกลาง ต้องกด \"รับงาน\" ก่อนจึงจะดำเนินการได้");
                if (job.PoolClaimedByUserId != model.actorUserId)
                    throw new InvalidOperationException("งานนี้มีคนอื่นรับไปแล้ว");
            }
        }

        if (toLevel <= DraftLevel)
            return await ReturnToDraftAsync(db, model, job, move, fromLevel, ct);

        var target = await db.wf_sub_workflow_masters
            .FirstOrDefaultAsync(s => s.workflowid == job.workflowid && s.wlevel == toLevel, ct)
            ?? throw new InvalidOperationException($"ไม่พบขั้นที่ {toLevel} ของ workflow นี้");

        var resolver = MakeResolver(db);
        var recipients = await move.RecipientsAsync(db, resolver, _users, job, target, ct);

        var hopNow = 0;
        if (move.Delta == 0 && WorkflowApproverResolver.SupervisorLevels(target) > 1)
        {
            var mine = await db.job_user_lists
                .Where(a => a.jobmasterid == job.jobmasterid && a.wlevel == toLevel && a.userid == model.actorUserId)
                .OrderByDescending(a => a.jobseq).FirstOrDefaultAsync(ct);
            var doneHop = int.TryParse(mine?.emplevel, out var h) ? h : 1;

            if (doneHop < WorkflowApproverResolver.SupervisorLevels(target))
            {
                hopNow = doneHop + 1;
                var next = await resolver.SupervisorAtHopAsync(target, job, hopNow, ct);
                var ids = next?.UserIds ?? new List<long>();
                if (ids.Count == 0)
                    throw new InvalidOperationException(
                        $"ไต่หาหัวหน้าชั้นที่ {hopNow} ไม่พบ — {next?.Note ?? "ตรวจสอบผังองค์กร"}");
                var found = await _users.GetUsersAsync(ids, ct);
                recipients = await ApplyDelegationAsync(db, _users, job, found.ToList(), ct);
            }
        }

        var autoSkip = move.LeavesLevel && recipients.Count == 0 && target.isAutoApproveAllow && !target.istop;

        if (move.LeavesLevel && recipients.Count == 0 && !autoSkip)
            throw new InvalidOperationException($"ขั้นที่ {toLevel} ยังไม่มีผู้เกี่ยวข้อง — ตรวจสอบการตั้งค่า");

        var leaving = await CurrentFootprintAsync(db, job.jobmasterid, fromLevel, ct);
        if (leaving is not null) leaving.endtime = DateTime.Now;

        var currentRows = await db.job_user_lists
            .Where(a => a.jobmasterid == job.jobmasterid && a.wlevel == fromLevel && a.isLast == true)
            .ToListAsync(ct);
        foreach (var row in currentRows)
        {
            if (row.userid == model.actorUserId)
            {
                row.jobstatus = move.ActorRowStatus;
                row.approvedate = DateTime.Now;
                row.reason = model.reason;
                row.comment = model.reason;
                row.mas_reason_id = model.mas_reason_id;
                row.isLast = false;
            }
            else if (move.LeavesLevel)
            {
                row.isLast = false;
            }
        }

        var actor = await _users.GetUserAsync(model.actorUserId, ct);

        var jobSub = CreateJobSubWorkflow(job, target);
        jobSub.reason = model.reason;
        jobSub.modby = actor?.FullName;
        jobSub.remark = autoSkip
            ? $"AutoSkip {fromLevel} -> {toLevel} (ไม่มีผู้อนุมัติ)"
            : $"{move.Name} {fromLevel} -> {toLevel}";
        if (autoSkip) jobSub.endtime = DateTime.Now;
        db.job_subworkflow_masters.Add(jobSub);
        job.jobseq = jobSub.jobseq;

        var statusBefore = job.status;
        job.lastLevel = toLevel;
        job.status = move.StampOn(target) ?? job.status;
        job.remark = model.reason;

        var levelDone = true;
        var levelFailed = false;
        if (move.Delta == 0 && hopNow == 0)
        {
            var visits = await db.job_subworkflow_masters
                .Where(s => s.jobmasterid == job.jobmasterid && s.wlevel == toLevel)
                .Select(s => new { s.jobseq, s.remark }).ToListAsync(ct);
            var arrival = visits
                .Where(v => v.remark == null || !v.remark.StartsWith("Approve", StringComparison.Ordinal))
                .Select(v => v.jobseq ?? 0).DefaultIfEmpty(0).Max();

            var roundRows = await db.job_user_lists
                .Where(a => a.jobmasterid == job.jobmasterid && a.wlevel == toLevel && (a.jobseq ?? 0) >= arrival)
                .ToListAsync(ct);

            var outcome = WorkflowEngineService.EvaluateLevel(jobSub, roundRows);
            levelDone = outcome == WorkflowEngineService.LevelOutcome.Complete;
            levelFailed = outcome == WorkflowEngineService.LevelOutcome.Failed;

            if (!levelDone)
                job.status = target.standstatus ?? statusBefore;
        }

        if (hopNow == 0 && levelDone && move.ClosesJob(target))
        {
            job.isJobClosed = true;
            job.reasonClosed = WorkflowEngineService.ClosedByApprove;
            job.approvedDate = DateTime.Now;
            job.approvedUserID = model.actorUserId;
            job.enddate = DateTime.Now;
            jobSub.endtime = DateTime.Now;
        }

        model.direction = move.Name;
        model.jobMaster = job;
        model.subWorkflow = target;
        model.jobsub = jobSub;
        if (move.LeavesLevel || hopNow > 0)
        {
            var hop = hopNow > 0 ? hopNow : (WorkflowApproverResolver.SupervisorLevels(target) > 0 ? 1 : (int?)null);
            model.jobUserList = CreateJobUserList(model, recipients, job, target, hop);
            db.job_user_lists.AddRange(model.jobUserList);
        }

        await db.SaveChangesAsync(ct);
        AuditLog(job, move.Name, new { from = fromLevel, to = toLevel, hop = hopNow, closed = job.isJobClosed, model.reason });

        if (autoSkip)
            return await MoveAsync(model, WorkflowMove.Forward, ct, internalHop: true, sharedDb: db);

        if (model.jobUserList.Count > 0)
            await NotifyRecipientsAsync(db, job, model.jobUserList, target, ct);

        if (move is ReturnToSenderMove)
            await NoticeEveryoneInvolvedAsync(db, job, model.actorUserId, model.reason, ct);

        if (move.Delta == 0 && hopNow == 0 && levelDone && !target.istop)
            return await MoveAsync(model, WorkflowMove.Forward, ct, internalHop: true, sharedDb: db);

        if (move.Delta == 0 && hopNow == 0 && levelFailed)
            return await MoveAsync(model, WorkflowMove.Backward, ct, internalHop: true, sharedDb: db);

        return model;
    }

    private async Task NoticeEveryoneInvolvedAsync(
        WorkflowDbContext db, job_master job, long actorUserId, string? reason, CancellationToken ct, bool declined = false)
    {
        try
        {
            var empIds = await db.job_user_lists
                .Where(a => a.jobmasterid == job.jobmasterid && a.empid != null)
                .Select(a => a.empid!).Distinct().ToListAsync(ct);
            if (empIds.Count == 0) return;

            var actor = await _users.GetUserAsync(actorUserId, ct);

            var subject = declined
                ? $"งานไม่ได้รับการอนุมัติ: {job.subject ?? job.wname}"
                : $"งานถูกส่งกลับไปแก้ไข: {job.subject ?? job.wname}";
            var body = (declined
                        ? $"<p>งาน \"{job.subject}\" (Workflow: {job.wname}) ไม่ได้รับการอนุมัติ และปิดเรื่องแล้ว</p><p>ผู้พิจารณา: {actor?.FullName}</p>"
                        : $"<p>งาน \"{job.subject}\" (Workflow: {job.wname}) ถูกส่งกลับไปยังผู้กรอกแบบฟอร์มเพื่อแก้ไข</p><p>ผู้ส่งกลับ: {actor?.FullName}</p>")
                     + (string.IsNullOrWhiteSpace(reason) ? "" : $"<p>เหตุผล: {reason}</p>")
                     + "<p>แจ้งเพื่อทราบ เนื่องจากท่านเคยเกี่ยวข้องกับงานนี้</p>";

            foreach (var empId in empIds)
            {
                var emp = await _org.GetEmployeeAsync(empId, ct);
                if (emp is not null && !string.IsNullOrWhiteSpace(emp.Email))
                    await _notifier.SendAsync(emp.Email, subject, body, ct);
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "แจ้งเตือนการส่งกลับของงาน {JobMasterId} ไม่สำเร็จ", job.jobmasterid);
        }
    }

    internal static async Task<List<WorkflowUser>> GetUserRelateAsync(
        WorkflowApproverResolver resolver, IWorkflowUserDirectory users, job_master job, wf_sub_workflow_master sub, CancellationToken ct)
    {
        var ids = await resolver.GetApproverUserIdsAsync(sub, job, ct);
        if (ids.Count == 0) return new();
        var db = resolver.Db;
        var found = await users.GetUsersAsync(ids, ct);
        return await ApplyDelegationAsync(db, users, job, found.Where(u => !u.IsDisabled).ToList(), ct);
    }

    // มอบฉันทะ: แทนที่ผู้อนุมัติที่ไม่อยู่ ด้วยคนที่เขามอบไว้ — see
    // Wf_ApproverDelegation.cs for the design intent (unchanged from the
    // original). Static (not an instance method) so both WorkflowMove
    // subclasses and WorkflowService itself can call it with whatever
    // WorkflowDbContext/IWorkflowUserDirectory pair they already have.
    internal static async Task<List<WorkflowUser>> ApplyDelegationAsync(
        WorkflowDbContext db, IWorkflowUserDirectory directory, job_master job, List<WorkflowUser> users, CancellationToken ct)
    {
        if (users.Count == 0) return users;

        var ids = users.Select(u => u.UserId).ToList();
        var today = DateTime.Now.Date;
        var active = await db.Wf_ApproverDelegations
            .Where(d => d.IsActive && ids.Contains(d.FromUserId)
                     && d.StartDate <= today && today <= d.EndDate
                     && (d.WorkflowId == null || d.WorkflowId == job.workflowid))
            .ToListAsync(ct);
        if (active.Count == 0) return users;

        var byFrom = active
            .GroupBy(d => d.FromUserId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(d => d.WorkflowId.HasValue).First().ToUserId);

        var replacementIds = byFrom.Values.Distinct().ToList();
        var replacements = await directory.GetUsersAsync(replacementIds, ct);

        var result = new List<WorkflowUser>();
        foreach (var u in users)
        {
            if (byFrom.TryGetValue(u.UserId, out var toId)
                && replacements.FirstOrDefault(r => r.UserId == toId) is WorkflowUser stand)
            {
                Serilog.Log.Information(
                    "Job {JobMasterId}: งานที่จะไปหา {From} ถูกมอบให้ {To} ตามที่ตั้งไว้",
                    job.jobmasterid, u.UserId, toId);
                if (result.All(x => x.UserId != stand.UserId)) result.Add(stand);
            }
            else if (result.All(x => x.UserId != u.UserId))
            {
                result.Add(u);
            }
        }
        return result;
    }

    internal static async Task<List<WorkflowUser>> GetWhoSentAsync(
        WorkflowDbContext db, IWorkflowUserDirectory users, job_master job, int wlevel, CancellationToken ct)
    {
        for (var level = wlevel; level >= 0; level--)
        {
            var ids = await db.job_user_lists
                .Where(a => a.jobmasterid == job.jobmasterid && a.wlevel == level && a.userid != null)
                .Select(a => a.userid!.Value).Distinct().ToListAsync(ct);
            if (ids.Count > 0)
                return (await users.GetUsersAsync(ids, ct)).ToList();
        }
        return new();
    }

    private static Task<job_subworkflow_master?> CurrentFootprintAsync(
        WorkflowDbContext db, long jobmasterid, int wlevel, CancellationToken ct)
        => db.job_subworkflow_masters
            .Where(s => s.jobmasterid == jobmasterid && s.wlevel == wlevel)
            .OrderByDescending(s => s.jobseq).ThenByDescending(s => s.jobsubworkflowid)
            .FirstOrDefaultAsync(ct);

    private static job_subworkflow_master CreateJobSubWorkflow(job_master job, wf_sub_workflow_master sub)
        => new()
        {
            jobmasterid = job.jobmasterid,
            workflowid = job.workflowid,
            wlevel = sub.wlevel,
            wfcode = job.workflowcode,
            jobseq = (job.jobseq ?? 0) + 1,
            subject = sub.subject,
            status = sub.sitinstatus ?? sub.standstatus,
            forwardstatus = sub.forwardstatus,
            backwardstatus = sub.backwardstatus,
            sitinstatus = sub.sitinstatus,
            istop = sub.istop,
            isupperrole = sub.isupperrole,
            isupperuser = sub.isupperuser,
            iscustomUser = sub.iscustomUser,
            iscustomRole = sub.iscustomRole,
            iscondition = sub.iscondition,
            isorcondition = sub.isorcondition,
            isandcondition = sub.isandcondition,
            andpercent = sub.andpercent,
            isReturnSender = sub.isReturnSender,
            backwardlevel = sub.backwardlevel,
            isshow = sub.isshow,
            isLOA = sub.isLOA,
            isPool = sub.isPool,
            empLevel = sub.empLevel,
            isNeedsupervisorapprove = sub.isNeedsupervisorapprove,
            verticalMaxLevel = sub.verticalMaxLevel,
            isAdhocUser = sub.isAdhocUser,
            iscustomApprover = sub.iscustomApprover,
            approvedstatus = sub.approvedstatus,
            declinestatus = sub.declinestatus,
            loacode = sub.loacode,
            isAutoApproveAllow = sub.isAutoApproveAllow,
            isNeedBudgetApproval = sub.isNeedBudgetApproval,
            controller = sub.controller,
            action = sub.action,
            actionEdit = sub.actionEdit,
            displayName = sub.displayName,
            starttime = DateTime.Now,
            moddate = DateTime.Now,
        };

    private static List<job_user_list> CreateJobUserList(
        WorkFlowViewModel model, List<WorkflowUser> approverList, job_master job, wf_sub_workflow_master sub,
        int? supervisorHop = null)
        => approverList.Select(u => new job_user_list
        {
            jobmasterid = job.jobmasterid,
            workflowid = job.workflowid,
            wlevel = sub.wlevel,
            subworkflowmasterid = sub.subworkflowid,
            userid = u.UserId,
            empid = u.EmpId,
            username = u.FullName,
            orgcode = u.OrgCode,
            jobstatus = Pending,
            jobseq = job.jobseq,
            isLast = true,
            sendDate = DateTime.Now,
            recievedate = DateTime.Now,
            reason = model.reason,
            reftable = sub.controller,
            emplevel = supervisorHop?.ToString(),
            andPercent = sub.isandcondition && !sub.isorcondition && approverList.Count > 0
                ? Math.Round(100m / approverList.Count, 2) : null,
            isAutoApprove = false,
            moddate = DateTime.Now,
        }).ToList();

    // ── GetRouteAsync — "ผู้อนุมัติทั้งเส้นทาง" ────────────────────────────────
    public record RouteApprover(string Name, string? EmpNo, long? HremployeeId);

    public record RouteStep(long SubWorkflowId, int Level, string Name, bool IsTop,
        bool IsCurrent, bool IsDone, List<RouteApprover> Approvers, string Source, string? Happened);

    private static string ActionText(string? remark) => remark switch
    {
        null or "" => "ดำเนินการ",
        _ when remark.StartsWith("Approve", StringComparison.Ordinal) => "อนุมัติ",
        _ when remark.StartsWith("Submit", StringComparison.Ordinal) => "ส่งต่อ",
        _ when remark.StartsWith("Reject", StringComparison.Ordinal) => "ส่งกลับ",
        _ when remark.StartsWith("ReturnToSender", StringComparison.Ordinal) => "ส่งกลับหาผู้ยื่น",
        _ when remark.StartsWith("Decline", StringComparison.Ordinal) => "ไม่อนุมัติ",
        _ when remark.StartsWith("Cancel", StringComparison.Ordinal) => "ยกเลิกคำขอ",
        _ when remark.StartsWith("AutoSkip", StringComparison.Ordinal) => "ข้ามอัตโนมัติ (ไม่มีผู้อนุมัติ)",
        _ when remark.StartsWith("AutoApprove", StringComparison.Ordinal) => "อนุมัติอัตโนมัติ",
        _ when remark.StartsWith("Create", StringComparison.Ordinal) => "สร้างคำขอ",
        _ => remark,
    };

    private static string SourceText(string field) => field switch
    {
        "iscustomUser" => "ระบุรายชื่อไว้",
        "iscustomRole" => "ตามบทบาท (role)",
        "isupperrole" or "isupperuser" or "SupervisorChain" => "หัวหน้าตามผังองค์กร",
        "isLOA" => "ตามวงเงินอนุมัติ (LOA)",
        "isAdhocUser" => "ผู้อนุมัติที่ถูกเรียกเข้ามาเฉพาะงานนี้",
        "isApproverSameOrg" => "อยู่หน่วยงานเดียวกับผู้ขอ",
        "isApproverSameCostCenter" => "อยู่ cost center เดียวกับผู้ขอ",
        "userid1/2/3" => "ระบุ userid ไว้ที่ขั้นนี้",
        _ => field,
    };

    public async Task<List<RouteStep>> GetRouteAsync(long jobMasterId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var job = await db.job_masters.FirstOrDefaultAsync(j => j.jobmasterid == jobMasterId, ct);
        if (job is null) return new();

        var levels = await db.wf_sub_workflow_masters
            .Where(s => s.workflowid == job.workflowid && s.isshow)
            .OrderBy(s => s.wlevel).ToListAsync(ct);

        var rows = await db.job_user_lists
            .Where(a => a.jobmasterid == jobMasterId)
            .Select(a => new { a.wlevel, a.username, a.userid, a.empid, a.jobstatus, a.approvedate })
            .ToListAsync(ct);

        var stamps = await db.job_subworkflow_masters
            .Where(s => s.jobmasterid == jobMasterId)
            .Select(s => new { s.wlevel, s.remark, s.modby, s.starttime, s.endtime, s.jobseq })
            .ToListAsync(ct);

        var current = job.lastLevel ?? 0;
        var steps = new List<RouteStep>(levels.Count);
        var resolver = MakeResolver(db);

        foreach (var lv in levels)
        {
            var mine = rows.Where(h => (h.wlevel ?? 0) == lv.wlevel).ToList();
            var trail = stamps.Where(s => s.wlevel == lv.wlevel).OrderBy(s => s.jobseq ?? 0).ToList();

            if (mine.Count > 0 || trail.Count > 0)
            {
                var happened = trail.Count == 0 ? null : string.Join(" · ", trail
                    .Select(t => $"{ActionText(t.remark)}{(t.modby is null ? "" : $" โดย {t.modby}")}" +
                                 $"{(t.starttime is null ? "" : $" ({t.starttime:d MMM HH:mm})")}"));

                steps.Add(new RouteStep(lv.subworkflowid, lv.wlevel, lv.subject ?? lv.displayName ?? $"ขั้นที่ {lv.wlevel}", lv.istop,
                    lv.wlevel == current, lv.wlevel < current,
                    mine.Select(p => new RouteApprover(p.username ?? $"#{p.userid}", p.empid, null))
                        .DistinctBy(a => a.Name + "|" + a.EmpNo).ToList(),
                    "จากประวัติของงานนี้", happened));
                continue;
            }

            if (job.isJobClosed == true)
            {
                steps.Add(new RouteStep(lv.subworkflowid, lv.wlevel, lv.subject ?? lv.displayName ?? $"ขั้นที่ {lv.wlevel}", lv.istop,
                    false, false, new(), "งานปิดแล้ว ไม่ได้เดินมาถึงขั้นนี้", null));
                continue;
            }

            var names = new List<RouteApprover>();
            var source = "ยังหาผู้อนุมัติจากการตั้งค่าไม่ได้";
            try
            {
                var found = await resolver.GetUserBySourceAsync(lv, job, ct);
                var ids = found.SelectMany(f => f.UserIds).Distinct().ToList();
                if (ids.Count > 0)
                {
                    var users = await _users.GetUsersAsync(ids, ct);
                    var applied = await ApplyDelegationAsync(db, _users, job, users.ToList(), ct);
                    // TODO(seam): original also resolves each approver's Hremployee.id
                    // here for a UI deep-link (/employee/{id}) — dropped; HremployeeId
                    // stays null. That enrichment is a Blazor-layer concern, not
                    // something the engine itself should need to know about.
                    names = applied.Select(u => new RouteApprover(u.FullName, u.EmpId, null))
                        .Where(a => a.Name.Length > 0).DistinctBy(a => a.Name + "|" + a.EmpNo).ToList();
                }
                var why = found.Where(f => f.UserIds.Count > 0).Select(f => SourceText(f.Field)).Distinct().ToList();
                if (why.Count > 0) source = string.Join(" · ", why);
            }
            catch { /* config ไม่ครบไม่ควรทำให้ทั้งหน้าพัง — ปล่อยให้ขึ้นว่าหาไม่ได้ */ }

            steps.Add(new RouteStep(lv.subworkflowid, lv.wlevel, lv.subject ?? lv.displayName ?? $"ขั้นที่ {lv.wlevel}", lv.istop,
                lv.wlevel == current, false, names, source, null));
        }

        return steps;
    }
}
