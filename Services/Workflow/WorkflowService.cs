using HRM.Models;
using HRM.Services;
using HRM.Services.Shared;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Workflow;

// ============================================================================
//  WorkflowService — พอร์ตตรงจาก epms.Services.WorkflowServices ของ CEO
// ============================================================================
//
//  ทำไมเป็นไฟล์ใหม่ ไม่ใช่แก้ WorkflowEngineService:
//  ตัวเดิมมี 20+ โมดูลเรียกอยู่ (ลา, OT, payroll, IDP, LMS, ...) รื้อโครงมัน
//  ตามแบบนี้แล้วของที่ใช้อยู่พังหมด สองตัวใช้ตารางเดียวกันได้ เพราะโครงตาราง
//  เป็นของ CEO อยู่แล้ว
//
//  หลักที่ CEO วางไว้ (9 ก.ย. 2569):
//    1. wf_workflow = ชื่องาน / wf_sub_workflow_master = เส้นทางเดิน หนึ่งแถว = หนึ่งขั้น
//    2. เครื่องอ่านทีละขั้น ไม่รู้เส้นทางทั้งเส้น
//    3. ทุกเมธอดรับ WorkFlowViewModel ตัวเดียว คืนตัวเดียว (ส่งค่าไป-กลับ)
//    4. อ่าน config แล้ว stamp ลง job_master + job_subworkflow_master
//    5. ไม่ตีความ config เอง มีค่าอะไรก็ทำตามนั้น
//
//  งานขยับได้สามแบบ และตรงกับสามสถานะใน config พอดี:
//
//    Submit   forward   +1   งานเดินไปขั้นถัดไป      ผู้รับ: อ่านจาก config ของขั้นนั้น
//    Reject   backward  -1   งานถอยกลับ             ผู้รับ: คนที่เป็นคนส่งในขั้นนั้น
//    Approve  stand      0   อนุมัติที่ขั้นตัวเอง งานไม่วิ่ง
//
//  งานจบเมื่อ "ยืนอยู่ที่ขั้นที่ไม่มีที่ให้ไปต่อ" (istop) ไม่ใช่เพราะปุ่มชื่อ Approve
//
// ============================================================================


// ── ตัวขนค่าไป-กลับ ── ตรงกับ epms.ViewModel.WorkFlowViewModel
public class WorkFlowViewModel
{
    // หน้าจอกรอกเข้ามา
    public long jobmasterid { get; set; }
    public long actorUserId { get; set; }        // คนที่กดปุ่ม (epms อ่านจาก session)
    public string? reason { get; set; }
    public long? mas_reason_id { get; set; }
    public decimal? loaValue { get; set; }

    // ทิศทางของก้าวนี้ = waction ของ JSP WorkFlowChain
    public string? direction { get; set; }

    // service เตรียมไว้ให้หน้าจอ
    public wf_workflow? wfWorkflow { get; set; }
    public job_master? jobMaster { get; set; }
    public wf_sub_workflow_master? subWorkflow { get; set; }   // config ของขั้นปัจจุบัน
    public job_subworkflow_master? jobsub { get; set; }        // รอยเท้าล่าสุด
    public List<job_user_list> jobUserList { get; set; } = new();
    public List<job_user_list> jobUserListPredict { get; set; } = new();
    public job_user_list? jobUserListSession { get; set; }
    public bool isCurrentUser { get; set; }
    public List<string> message { get; set; } = new();

    // ── ปุ่มส่งกลับ: ขั้นนี้ส่งกลับได้แบบไหน และไปหาใคร (เอาไว้ขึ้นชื่อบนปุ่ม) ──
    // ขั้นที่ติ๊ก isReturnSender -> ส่งกลับถึงผู้กรอกแบบฟอร์มได้เลย
    // ขั้นที่ไม่ติ๊ก              -> ส่งกลับได้ทีละขั้น ไปหาคนที่ส่งงานมาให้
    public bool canReturnToSender { get; set; }
    public string? sendBackToName { get; set; }
    public int? sendBackToLevel { get; set; }

    // ── ขั้นแบบงานกอง (isPool) ── ต้องกดรับงานก่อนถึงจะดำเนินการได้
    public bool isPoolLevel { get; set; }
    public bool poolClaimedByMe { get; set; }
    public string? poolClaimedByName { get; set; }
    // ทุกคนที่อยู่ในกองของขั้นนี้ (job_user_list ของ workflow+level+รอบนี้)
    public List<job_user_list> poolMembers { get; set; } = new();
}


// ============================================================================
//  WorkflowMove — หนึ่งคลาสต่อหนึ่งการขยับ
//
//  ทั้งหมดที่ทำให้ "ส่งต่อ" ต่างจาก "ส่งกลับ" ต่างจาก "อนุมัติอยู่กับที่"
//  อยู่ในคลาสลูกสามตัวนี้ที่เดียว WorkflowService จึงไม่ต้องมี if เช็คทิศทางเลย
//  เพิ่มการขยับแบบใหม่ = เพิ่มคลาสใหม่ ไม่ต้องแก้ service
// ============================================================================
public abstract class WorkflowMove
{
    public abstract string Name { get; }

    // ขยับกี่ขั้น: +1 / -1 / 0
    public abstract int Delta { get; }

    // สถานะที่ตราลง job_master เมื่อไปถึงขั้นปลายทาง — อ่านจาก config ของขั้นนั้น
    public abstract string? StampOn(wf_sub_workflow_master target);

    // สถานะที่ตราลงแถวใบงานของคนที่กดปุ่ม
    public abstract string ActorRowStatus { get; }

    // ใครรับงานต่อ
    public abstract Task<List<sc_user>> RecipientsAsync(
        HRMContext db, job_master job, wf_sub_workflow_master target, CancellationToken ct);

    // งานออกจากขั้นนี้ไปไหม
    //   ออก   -> ต้องหาผู้รับขั้นใหม่ และใบงานของขั้นเดิมหมดหน้าที่ (isLast=false)
    //   ไม่ออก -> งานยังอยู่กับคนเดิม ใบงานของคนอื่นในขั้นนี้ยังถือต่อ
    public virtual bool LeavesLevel => true;

    // งานจบตรงนี้ไหม
    public virtual bool ClosesJob(wf_sub_workflow_master target) => false;

    public static readonly WorkflowMove Forward = new ForwardMove();
    public static readonly WorkflowMove Backward = new BackwardMove();
    public static readonly WorkflowMove Stand = new StandMove();
}

// ── ส่งต่อ: ไปขั้นถัดไป ผู้รับอ่านจาก config ของขั้นนั้น ─────────────────────
public sealed class ForwardMove : WorkflowMove
{
    public override string Name => "Submit";
    public override int Delta => +1;
    public override string ActorRowStatus => WorkflowService.Approved;

    // ไปถึงแล้วงาน "นั่ง" อยู่ที่ขั้นนั้น
    public override string? StampOn(wf_sub_workflow_master target)
        => target.sitinstatus ?? target.standstatus;

    public override Task<List<sc_user>> RecipientsAsync(
        HRMContext db, job_master job, wf_sub_workflow_master target, CancellationToken ct)
        => WorkflowService.GetUserRelateAsync(db, job, target, ct);
}

// ── ส่งกลับทีละขั้น: ถอย 1 ขั้น ผู้รับคือคนที่เป็นคนส่งในขั้นนั้น ─────────────
//    ใช้เมื่อขั้นที่ยืนอยู่ "ไม่ได้" ติ๊ก isReturnSender
public class BackwardMove : WorkflowMove
{
    public override string Name => "Reject";
    public override int Delta => -1;
    public override string ActorRowStatus => WorkflowService.Rejected;

    public override string? StampOn(wf_sub_workflow_master target)
        => target.backwardstatus;

    public override Task<List<sc_user>> RecipientsAsync(
        HRMContext db, job_master job, wf_sub_workflow_master target, CancellationToken ct)
        => WorkflowService.GetWhoSentAsync(db, job, target.wlevel, ct);
}

// ── ส่งกลับหาผู้กรอกแบบฟอร์ม: ข้ามกลับไปหาคนสร้างงานเลย ─────────────────────
//
//    CEO, 9 ก.ย. 2569: "ถ้า sub_workflow ไหน check isReturnSender แสดงว่าคนที่
//    อนุมัติระดับนั้นมีสิทธิส่งกลับไปที่คนสร้างงาน (มีปุ่มส่งกลับหาผู้กรอกแบบฟอร์ม)
//    แต่ต้อง notice คนที่เกี่ยวข้องที่ผ่านมาทั้งหมด (ใน job_userlist) ให้รู้ว่ามีการ
//    ส่งงานกลับไปแก้ ถ้าไม่ checked ให้ส่งกลับได้ทีละขั้น"
//
//    สิทธินี้เป็นของ "ขั้นที่ยืนอยู่" ไม่ใช่ของขั้นปลายทาง — หน้าจอจึงดู
//    subWorkflow.isReturnSender ของขั้นปัจจุบันเพื่อตัดสินว่าจะโชว์ปุ่มไหน
public sealed class ReturnToSenderMove : BackwardMove
{
    private readonly int _creatorLevel;
    public ReturnToSenderMove(int creatorLevel, int fromLevel)
    {
        _creatorLevel = creatorLevel;
        Jump = creatorLevel - fromLevel;      // ข้ามกลับกี่ขั้น
    }

    public int Jump { get; }
    public override string Name => "ReturnToSender";
    public override int Delta => Jump;

    // ผู้รับคือคนสร้างงานเสมอ ไม่ใช่คนที่ส่งมา
    public override async Task<List<sc_user>> RecipientsAsync(
        HRMContext db, job_master job, wf_sub_workflow_master target, CancellationToken ct)
    {
        if (job.createuserid is not long creator) return new();
        var u = await db.sc_users.FirstOrDefaultAsync(x => x.userid == creator, ct);
        return u is null ? new() : new List<sc_user> { u };
    }
}

// ── อยู่กับที่: อนุมัติที่ขั้นตัวเอง งานไม่วิ่ง ────────────────────────────────
//    (CEO: "approve ก็คือ stand คือ อนุมัติที่คุณนั่น แล้ว workflow ไม่วิ่ง")
//    จบงานก็ต่อเมื่อขั้นนั้นเป็น istop — ไม่มีที่ให้ไปต่อแล้ว
public sealed class StandMove : WorkflowMove
{
    public override string Name => "Approve";
    public override int Delta => 0;
    public override string ActorRowStatus => WorkflowService.Approved;
    public override bool LeavesLevel => false;       // งานไม่วิ่ง ยังอยู่กับคนเดิม

    public override string? StampOn(wf_sub_workflow_master target)
        => target.istop ? target.forwardstatus : target.standstatus;

    public override bool ClosesJob(wf_sub_workflow_master target) => target.istop;

    public override Task<List<sc_user>> RecipientsAsync(
        HRMContext db, job_master job, wf_sub_workflow_master target, CancellationToken ct)
        => Task.FromResult(new List<sc_user>());
}


public class WorkflowService
{
    // ใช้ค่าสถานะชุดเดียวกับ engine เดิม หน้าจอ/รายงานที่มีอยู่จะอ่านออกเหมือนกัน
    public const string Pending = WorkflowEngineService.StatusPending;
    public const string Approved = WorkflowEngineService.StatusApproved;
    public const string Rejected = WorkflowEngineService.StatusRejected;
    public const string StatusReturned = WorkflowEngineService.StatusReturned;
    public const string Completed = WorkflowEngineService.StatusCompleted;

    private readonly IDbContextFactory<HRMContext> _dbFactory;
    private readonly EmailSender _emailSender;

    public WorkflowService(IDbContextFactory<HRMContext> dbFactory, EmailSender emailSender)
    {
        _dbFactory = dbFactory;
        _emailSender = emailSender;
    }

    // ── ปุ่มสามปุ่ม = สามการขยับ ────────────────────────────────────────────
    public Task<WorkFlowViewModel> SubmitAsync(WorkFlowViewModel m, CancellationToken ct = default)
        => MoveAsync(m, WorkflowMove.Forward, ct);

    public Task<WorkFlowViewModel> RejectOneStepAsync(WorkFlowViewModel m, CancellationToken ct = default)
        => MoveAsync(m, WorkflowMove.Backward, ct);

    // ส่งกลับหาผู้กรอกแบบฟอร์ม — ใช้ได้เฉพาะขั้นที่ติ๊ก isReturnSender ไว้
    // ปลายทางคือขั้นแรกสุดของเส้นทาง (ขั้นที่ผู้ขอถือแบบฟอร์มอยู่)
    public async Task<WorkFlowViewModel> ReturnToSenderAsync(WorkFlowViewModel m, CancellationToken ct = default)
    {
        await using (var db = await _dbFactory.CreateDbContextAsync(ct))
        {
            var job = await db.job_masters.FirstOrDefaultAsync(j => j.jobmasterid == m.jobmasterid, ct)
                ?? throw new InvalidOperationException("ไม่พบงาน");
            var from = job.lastLevel ?? 0;

            var here = await db.wf_sub_workflow_masters
                .FirstOrDefaultAsync(s => s.workflowid == job.workflowid && s.wlevel == from, ct);
            if (here?.isReturnSender != true)
                throw new InvalidOperationException("ขั้นนี้ไม่ได้เปิดสิทธิ์ส่งกลับหาผู้กรอกแบบฟอร์ม (isReturnSender)");

            var creatorLevel = await db.wf_sub_workflow_masters
                .Where(s => s.workflowid == job.workflowid)
                .MinAsync(s => s.wlevel, ct);
            if (creatorLevel >= from)
                throw new InvalidOperationException("ขั้นนี้เป็นขั้นแรกอยู่แล้ว ส่งกลับไม่ได้");

            return await MoveAsync(m, new ReturnToSenderMove(creatorLevel, from), ct);
        }
    }

    public Task<WorkFlowViewModel> ApproveAsync(WorkFlowViewModel m, CancellationToken ct = default)
        => MoveAsync(m, WorkflowMove.Stand, ct);
    // ========================================================================
    //  Create — บันทึกข้อมูลลงตาราง ref แล้วงานเกิดเป็น "draft" ที่ระดับ 0
    //
    //  CEO, 10 ก.ย. 2569: "0 ไม่ต้องสร้างสิ มันอัตโนมัติ เป็น draft
    //  (บันทึกข้อมูลใน table ref)"
    //  -> ระดับ 0 ไม่ใช่ config ไม่มีแถวใน wf_sub_workflow_master
    //     เป็นสถานะของงานตอนอยู่ในมือผู้กรอก ก่อนส่งเข้าเส้นทางจริง
    //     เส้นทางที่ config ไว้จึงเริ่มที่ wlevel 1 เสมอ
    //     (epms: WorkflowController.Create -> RedirectToAction("Detail"))
    // ========================================================================
    public const int DraftLevel = 0;
    public const string StatusDraft = "DRAFT";

    public async Task<WorkFlowViewModel> CreateAsync(
        long workflowid, long actorUserId, string? empid, string? subject,
        string? reftable, string? refid, decimal? amount, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var wf = await db.wf_workflows.FirstOrDefaultAsync(w => w.workflowid == workflowid, ct)
            ?? throw new InvalidOperationException($"ไม่พบ workflow id {workflowid}");

        var levelCount = await db.wf_sub_workflow_masters.CountAsync(s => s.workflowid == workflowid, ct);
        if (levelCount == 0)
            throw new InvalidOperationException($"workflow {wf.workflowcode} ยังไม่มีเส้นทางเดิน (wf_sub_workflow_master ว่าง)");

        var user = await db.sc_users.FirstOrDefaultAsync(u => u.userid == actorUserId, ct);
        var fullName = $"{user?.firstname} {user?.lastname}".Trim();

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
            empid = empid ?? user?.empid,
            createby = user?.loginname,
            createusername = fullName,
            reqName = fullName,
            reqOrg = user?.orgcode,
            createdate = DateTime.Now,
            reqdate = DateTime.Now,
            reqamont = amount,
            isactive = true,
            isJobClosed = false,
            jobseq = 0,
        };
        db.job_masters.Add(job);
        await db.SaveChangesAsync(ct);   // ต้องได้ jobmasterid ก่อนจึงประทับรอยเท้าได้

        // ประทับรอยเท้าของ draft — ไม่มี config ให้ snapshot จึงลงเท่าที่มีจริง
        // (CEO: "draft ก็ต้องประทับด้วย" — ทุก action ต้องมีรอยเท้า)
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
            subWorkflow = null,      // draft ไม่มี config ของตัวเอง
            jobsub = jobSub,
        };

        // ผู้ถือ draft คือผู้สร้างงาน เพราะเขาเป็นคนกรอก ไม่ใช่เพราะ config
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
                orgcode = user?.orgcode,
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
        return model;
    }

    // งานถูกส่งกลับถึงระดับ draft — กลับไปอยู่ในมือผู้กรอกเหมือนตอนยังไม่ส่ง
    // ไม่มี config ให้อ่านเพราะ draft ไม่ใช่ขั้นที่ตั้งค่า ผู้รับคือผู้สร้างงานเสมอ
    private async Task<WorkFlowViewModel> ReturnToDraftAsync(
        HRMContext db, WorkFlowViewModel model, job_master job, WorkflowMove move, int fromLevel, CancellationToken ct)
    {
        if (job.createuserid is not long creator)
            throw new InvalidOperationException("งานนี้ไม่มีผู้สร้าง จึงส่งกลับไม่ได้");

        var owner = await db.sc_users.FirstOrDefaultAsync(u => u.userid == creator, ct)
            ?? throw new InvalidOperationException("ไม่พบผู้สร้างงาน");
        var actor = await db.sc_users.Where(u => u.userid == model.actorUserId)
            .Select(u => (u.firstname + " " + u.lastname).Trim()).FirstOrDefaultAsync(ct);

        // ปิดรอยเท้าและใบงานของขั้นที่ส่งกลับมา
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

        var jobSub = DraftFootprint(job, actor, $"{move.Name} {fromLevel} -> 0");
        jobSub.reason = model.reason;
        job.jobseq = jobSub.jobseq;
        db.job_subworkflow_masters.Add(jobSub);

        var back = new job_user_list
        {
            jobmasterid = job.jobmasterid,
            workflowid = job.workflowid,
            wlevel = DraftLevel,
            userid = owner.userid,
            empid = owner.empid,
            username = $"{owner.firstname} {owner.lastname}".Trim(),
            orgcode = owner.orgcode,
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

        if (move is ReturnToSenderMove)
            await NoticeEveryoneInvolvedAsync(db, job, model.actorUserId, model.reason, ct);

        return model;
    }

    // รอยเท้าของระดับ draft — ไม่มีแถว config ให้ snapshot
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

    // ========================================================================
    //  Detail — หน้าจออ่านค่าผ่านตัวนี้ (epms: WorkflowController.Detail GET)
    //    1. jobmaster
    //    2. subworkflowmaster ของขั้นที่งานอยู่ตอนนี้ (ใช้เตรียมค่า + ตัดสินว่าปุ่มไหนขึ้น)
    //    3. ทายผู้อนุมัติขั้นถัดไป ถ้ายังไม่ใช่ขั้นสุดท้าย
    // ========================================================================
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

        // คนที่กำลังดูหน้านี้ถืองานอยู่จริงไหม — ตัดสินว่าจะโชว์ปุ่มไหม
        model.jobUserListSession = model.jobUserList.FirstOrDefault(a =>
            a.userid == actorUserId && a.isLast == true && a.wlevel == currentlevel
            && string.Equals(a.jobstatus, Pending, StringComparison.OrdinalIgnoreCase));
        model.isCurrentUser = model.jobUserListSession is not null && job.isJobClosed != true;

        // isPool — ขั้นแบบงานกอง: ทุกคนในกลุ่มเห็น แต่ต้องกดรับงานก่อนถึงจะทำได้
        // กันสองคนทำพร้อมกัน ใครกดรับก่อนได้ไป (ใช้ช่อง PoolClaimed* บน job_master
        // ที่มีอยู่แล้ว ไม่เพิ่มคอลัมน์)
        if (model.subWorkflow?.isPool == true)
        {
            model.isPoolLevel = true;
            // ใครอยู่ในกองของขั้นนี้บ้าง — รอบปัจจุบันเท่านั้น
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
                ? await db.sc_users.Where(u => u.userid == pc)
                    .Select(u => (u.firstname + " " + u.lastname).Trim()).FirstOrDefaultAsync(ct)
                : null;
            // ยังไม่มีใครรับ หรือคนอื่นรับไปแล้ว -> ยังกดปุ่มดำเนินการไม่ได้
            if (!model.poolClaimedByMe) model.isCurrentUser = false;
        }

        // ── ส่งกลับได้แบบไหน ไปหาใคร ──────────────────────────────────────
        if (model.isCurrentUser && model.subWorkflow is not null)
        {
            var firstLevel = await db.wf_sub_workflow_masters
                .Where(s => s.workflowid == job.workflowid).MinAsync(s => s.wlevel, ct);

            if (model.subWorkflow.isReturnSender && currentlevel > firstLevel)
            {
                // ส่งกลับถึงผู้กรอกแบบฟอร์มได้เลย — ชื่อคือผู้สร้างงาน
                model.canReturnToSender = true;
                model.sendBackToLevel = firstLevel;
                model.sendBackToName = job.createusername ?? job.reqName;
            }
            else if (currentlevel > firstLevel)
            {
                // ส่งกลับทีละขั้น — ไปหา "คนที่ส่งงานมาให้" คือผู้อนุมัติขั้นก่อนหน้า
                // (โดยธรรมชาติเขาอนุมัติผ่านมาแล้ว จึงต้องเป็นคนแก้ก่อน)
                var senders = await GetWhoSentAsync(db, job, currentlevel - 1, ct);
                if (senders.Count > 0)
                {
                    model.sendBackToLevel = currentlevel - 1;
                    model.sendBackToName = string.Join(", ",
                        senders.Select(u => $"{u.firstname} {u.lastname}".Trim()));
                }
            }
        }

        // ทายผู้อนุมัติขั้นถัดไป (เฉพาะเมื่อยังไม่ใช่ขั้นสุดท้าย)
        if (model.subWorkflow is not null && !model.subWorkflow.istop && job.isJobClosed != true)
        {
            var next = await db.wf_sub_workflow_masters
                .FirstOrDefaultAsync(s => s.workflowid == job.workflowid && s.wlevel == currentlevel + 1, ct);
            if (next is not null)
            {
                var users = await GetUserRelateAsync(db, job, next, ct);
                model.jobUserListPredict = users.Select(u => new job_user_list
                {
                    wlevel = next.wlevel,
                    userid = u.userid,
                    empid = u.empid,
                    username = $"{u.firstname} {u.lastname}".Trim(),
                }).ToList();
            }
        }

        return model;
    }

    // ========================================================================
    //  ขั้นแบบงานกอง (isPool): กดรับงาน / คืนงาน
    //  ใครกดรับก่อนได้ไป กันสองคนทำพร้อมกัน — จองด้วย (user, level, jobseq)
    //  ถ้างานเดินไปขั้นถัดไปแล้ว การจองเก่าจะไม่ตรง jobseq จึงหมดอายุเอง
    // ========================================================================
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

    // ========================================================================
    //  MoveAsync — หนึ่งก้าวของงาน ใช้ร่วมกันทั้งสามการขยับ
    //  ไม่มี if เช็คทิศทางเลย ทุกความต่างอยู่ในคลาส WorkflowMove
    // ========================================================================
    private async Task<WorkFlowViewModel> MoveAsync(WorkFlowViewModel model, WorkflowMove move, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var job = await db.job_masters.FirstOrDefaultAsync(j => j.jobmasterid == model.jobmasterid, ct)
            ?? throw new InvalidOperationException("ไม่พบงาน");
        if (job.isJobClosed == true) throw new InvalidOperationException("งานนี้ปิดแล้ว");

        var fromLevel = job.lastLevel ?? 0;
        var toLevel = fromLevel + move.Delta;

        // ส่งกลับถึงระดับ draft — ไม่มี config ให้อ่าน เพราะ draft ไม่ใช่ขั้นที่ตั้งค่า
        // งานกลับไปอยู่ในมือผู้กรอกเหมือนตอนยังไม่ส่ง
        if (toLevel <= DraftLevel)
            return await ReturnToDraftAsync(db, model, job, move, fromLevel, ct);

        // 1. อ่านสคริปต์ของขั้นปลายทาง
        var target = await db.wf_sub_workflow_masters
            .FirstOrDefaultAsync(s => s.workflowid == job.workflowid && s.wlevel == toLevel, ct)
            ?? throw new InvalidOperationException($"ไม่พบขั้นที่ {toLevel} ของ workflow นี้");

        // 2. ใครรับงานต่อ — คลาสของการขยับเป็นคนตอบ
        var recipients = await move.RecipientsAsync(db, job, target, ct);

        // ── ไต่หัวหน้าในระดับเดียวกัน ──────────────────────────────────────
        // CEO, 10 ก.ย. 2569: "ถ้ามี isNeedsupervisorapprove ผลที่เกิดคือ jobsequence
        // ต้อง +1 ด้วย แต่ level ยังไม่เดิน แล้วต้อง stamp ทุกครั้งที่มี workflow action"
        //
        // ระดับที่ตั้งให้ผ่านหัวหน้า N ชั้น: หัวหน้าชั้นที่ 1 เซ็นแล้วงาน "ไม่เดิน"
        // ยังอยู่ระดับเดิม แต่ส่งต่อให้ชั้นที่ 2 ... จนครบ N ชั้นถึงจะไประดับถัดไป
        // ใช้ Stand ที่มีอยู่ ไม่ใช่การขยับแบบใหม่ — ต่างกันแค่ผู้รับเปลี่ยนเป็นชั้นถัดไป
        var hopNow = 0;
        if (move.Delta == 0 && target.SupervisorLevels > 1)
        {
            var mine = await db.job_user_lists
                .Where(a => a.jobmasterid == job.jobmasterid && a.wlevel == toLevel && a.userid == model.actorUserId)
                .OrderByDescending(a => a.jobseq).FirstOrDefaultAsync(ct);
            var doneHop = int.TryParse(mine?.emplevel, out var h) ? h : 1;

            if (doneHop < target.SupervisorLevels)
            {
                hopNow = doneHop + 1;
                var next = await target.SupervisorAtHopAsync(db, job, hopNow, ct);
                var ids = next?.UserIds ?? new List<long>();
                if (ids.Count == 0)
                    throw new InvalidOperationException(
                        $"ไต่หาหัวหน้าชั้นที่ {hopNow} ไม่พบ — {next?.Note ?? "ตรวจสอบผังองค์กร"}");
                recipients = await db.sc_users.Where(u => ids.Contains(u.userid)).ToListAsync(ct);
            }
        }

        // isAutoApproveAllow — ขั้นนี้ไม่มีใครเลย ให้ข้ามไปขั้นถัดไปเองแทนที่จะค้าง
        // (เช่นหน่วยงานยังไม่ได้ตั้งผู้อนุมัติ หรือ role นั้นยังไม่มีสมาชิก)
        // ประทับรอยเท้าไว้ด้วยว่าขั้นนี้ถูกข้าม จะได้อ่านประวัติออกว่าเกิดอะไรขึ้น
        if (move.LeavesLevel && recipients.Count == 0 && target.isAutoApproveAllow && !target.istop)
        {
            var skipper = await db.sc_users.Where(u => u.userid == model.actorUserId)
                .Select(u => (u.firstname + " " + u.lastname).Trim()).FirstOrDefaultAsync(ct);

            var skipStamp = CreateJobSubWorkflow(job, target);
            skipStamp.modby = skipper;
            skipStamp.remark = $"AutoSkip {fromLevel} -> {toLevel} (ไม่มีผู้อนุมัติ)";
            skipStamp.endtime = DateTime.Now;
            db.job_subworkflow_masters.Add(skipStamp);

            job.jobseq = skipStamp.jobseq;
            job.lastLevel = toLevel;
            await db.SaveChangesAsync(ct);

            return await MoveAsync(model, WorkflowMove.Forward, ct);   // ไปขั้นถัดไปต่อ
        }

        if (move.LeavesLevel && recipients.Count == 0)
            throw new InvalidOperationException($"ขั้นที่ {toLevel} ยังไม่มีผู้เกี่ยวข้อง — ตรวจสอบการตั้งค่า");

        // 3. ปิดรอยเท้าของขั้นที่เพิ่งทำเสร็จ + ตราสถานะลงแถวของคนที่กด
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
                row.isLast = false;                  // คนนี้ทำไปแล้ว
            }
            else if (move.LeavesLevel)
            {
                row.isLast = false;                  // งานออกจากขั้นนี้ ใบของคนอื่นหมดหน้าที่
            }
        }

        // 4. ประทับรอยเท้า — ทุกการกระทำประทับ ไม่ใช่เฉพาะตอนเดิน
        //    (CEO, 9 ก.ย. 2569: "StandMove เก็บประวัติด้วยสิ คุณมาดู workflow
        //    ได้รู้ว่าเกิดอะไรขึ้น") ยืนอยู่กับที่ก็ได้แถวใหม่ที่ wlevel เดิม
        //    ต่างกันที่ jobseq — เปิดดูแล้วเห็นครบว่าใครทำอะไรตอนไหน
        var actorName = await db.sc_users
            .Where(u => u.userid == model.actorUserId)
            .Select(u => (u.firstname + " " + u.lastname).Trim())
            .FirstOrDefaultAsync(ct);

        var jobSub = CreateJobSubWorkflow(job, target);
        jobSub.reason = model.reason;
        jobSub.modby = actorName;                                  // ใครทำ
        jobSub.remark = $"{move.Name} {fromLevel} -> {toLevel}";   // ทำอะไร
        db.job_subworkflow_masters.Add(jobSub);
        job.jobseq = jobSub.jobseq;   // jobseq เป็นของรอยเท้า job คัดลอกกลับ

        // 5. stamp ลง job_master
        job.lastLevel = toLevel;
        job.status = move.StampOn(target) ?? job.status;
        job.remark = model.reason;

        // ── ระดับนี้ครบเงื่อนไขหรือยัง ─────────────────────────────────────
        // ขั้นที่มีผู้อนุมัติหลายคน ใครกดคนแรกไม่ได้แปลว่าจบ ต้องดูเงื่อนไขของขั้น:
        //   isandcondition + andpercent -> รวมน้ำหนักให้ถึงเกณฑ์
        //   isorcondition               -> ใครอนุมัติคนแรกก็พอ
        //   ไม่ตั้งอะไร                  -> ต้องครบทุกคน
        // ใช้ EvaluateLevel ของ engine เดิม ซึ่งเป็น pure method และมีเทสอยู่แล้ว
        // ไม่เขียน logic ซ้ำ สองเครื่องจึงตัดสินเหมือนกันเสมอ
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
        }

        // ยังไต่หัวหน้าไม่ครบ หรือยังรอคนอื่นในขั้นเดียวกัน = ระดับนี้ยังไม่จบ
        if (hopNow == 0 && levelDone && move.ClosesJob(target))
        {
            var actor = await db.sc_users.FirstOrDefaultAsync(u => u.userid == model.actorUserId, ct);
            job.isJobClosed = true;
            job.reasonClosed = model.reason;
            job.approvedDate = DateTime.Now;
            job.approvedUserID = model.actorUserId;
            job.approvedBy = actor?.loginname;
            job.enddate = DateTime.Now;
            jobSub.endtime = DateTime.Now;
        }

        // 6. ออกใบงานให้ผู้รับ
        model.direction = move.Name;
        model.jobMaster = job;
        model.subWorkflow = target;
        model.jobsub = jobSub;
        // ออกใบงานเมื่องานเดินไประดับใหม่ หรือเมื่อไต่หัวหน้าชั้นถัดไปในระดับเดิม
        if (move.LeavesLevel || hopNow > 0)
        {
            // ชั้นที่เท่าไหร่ของการไต่ ติดไว้บนใบงาน จะได้รู้ว่าไต่ถึงไหนแล้ว
            var hop = hopNow > 0 ? hopNow : (target.SupervisorLevels > 0 ? 1 : (int?)null);
            model.jobUserList = CreateJobUserList(model, recipients, job, target, hop);
            db.job_user_lists.AddRange(model.jobUserList);
        }

        await db.SaveChangesAsync(ct);

        // ส่งกลับหาผู้กรอกแบบฟอร์ม = ทุกคนที่เคยผ่านงานนี้มาต้องรู้ว่าถูกส่งกลับไปแก้
        // (CEO: "ต้อง notice คนที่เกี่ยวข้องที่ผ่านมาทั้งหมด ใน job_userlist")
        if (move is ReturnToSenderMove)
            await NoticeEveryoneInvolvedAsync(db, job, model.actorUserId, model.reason, ct);

        // ระดับนี้ครบเงื่อนไขแล้วและไม่ใช่ขั้นสุดท้าย -> งานเดินต่อเอง
        // ผู้อนุมัติแค่กด "อนุมัติ" ไม่ต้องมีใครมากดส่งต่ออีกที
        if (move.Delta == 0 && hopNow == 0 && levelDone && !target.istop)
            return await MoveAsync(model, WorkflowMove.Forward, ct);

        // ระดับนี้ล่ม (ถูกปฏิเสธ / น้ำหนักไม่ถึงเกณฑ์แล้ว) -> ถอยกลับตาม config
        if (move.Delta == 0 && hopNow == 0 && levelFailed)
            return await MoveAsync(model, WorkflowMove.Backward, ct);

        return model;
    }

    // ------------------------------------------------------------------------
    //  แจ้งทุกคนที่เคยถือ/เคยเกี่ยวข้องกับงานนี้ ว่างานถูกส่งกลับไปแก้แล้ว
    //  อ่านรายชื่อจาก job_user_list ทั้งหมดของงาน ไม่ใช่เฉพาะขั้นปัจจุบัน
    // ------------------------------------------------------------------------
    private async Task NoticeEveryoneInvolvedAsync(
        HRMContext db, job_master job, long actorUserId, string? reason, CancellationToken ct)
    {
        try
        {
            var empIds = await db.job_user_lists
                .Where(a => a.jobmasterid == job.jobmasterid && a.empid != null)
                .Select(a => a.empid!).Distinct().ToListAsync(ct);
            if (empIds.Count == 0) return;

            var actor = await db.sc_users.Where(u => u.userid == actorUserId)
                .Select(u => (u.firstname + " " + u.lastname).Trim()).FirstOrDefaultAsync(ct);

            var subject = $"งานถูกส่งกลับไปแก้ไข: {job.subject ?? job.wname}";
            var body = $"<p>งาน \"{job.subject}\" (Workflow: {job.wname}) ถูกส่งกลับไปยังผู้กรอกแบบฟอร์มเพื่อแก้ไข</p>"
                     + $"<p>ผู้ส่งกลับ: {actor}</p>"
                     + (string.IsNullOrWhiteSpace(reason) ? "" : $"<p>เหตุผล: {reason}</p>")
                     + "<p>แจ้งเพื่อทราบ เนื่องจากท่านเคยเกี่ยวข้องกับงานนี้</p>";

            var emps = await db.Hremployee.Where(e => empIds.Contains(e.EmpNo)).ToListAsync(ct);
            foreach (var emp in emps)
            {
                var email = await EmployeeEmailResolver.ResolveAsync(db, emp.id, ct);
                if (!string.IsNullOrWhiteSpace(email))
                    await _emailSender.SendEmailAsync(email, subject, body);
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "แจ้งเตือนการส่งกลับของงาน {JobMasterId} ไม่สำเร็จ", job.jobmasterid);
        }
    }

    // ========================================================================
    //  getUserRelate — "ขั้นนี้ใครเกี่ยวข้อง"
    //  เรียกเมธอดของตัว config เอง (wf_sub_workflow_master.GetUserAsync)
    //  ไม่มีคลาสอะไรมาห่อมันอีกชั้น — มีคลาสเดียวคือ wf_sub_workflow_master
    // ========================================================================
    internal static Task<List<sc_user>> GetUserRelateAsync(
        HRMContext db, job_master job, wf_sub_workflow_master sub, CancellationToken ct)
        => sub.GetUserAsync(db, job, ct);


    // ========================================================================
    //  หาคนที่เป็นคนส่งในขั้นนั้น — ใช้ตอนส่งกลับ
    //  epms RejectOneStep: ถ้าขั้นนั้นไม่มีใครเลย ให้ถอยลงอีกขั้นจนกว่าจะเจอ
    // ========================================================================
    internal static async Task<List<sc_user>> GetWhoSentAsync(
        HRMContext db, job_master job, int wlevel, CancellationToken ct)
    {
        for (var level = wlevel; level >= 0; level--)
        {
            var ids = await db.job_user_lists
                .Where(a => a.jobmasterid == job.jobmasterid && a.wlevel == level && a.userid != null)
                .Select(a => a.userid!.Value).Distinct().ToListAsync(ct);
            if (ids.Count > 0)
                return await db.sc_users.Where(u => ids.Contains(u.userid)).ToListAsync(ct);
        }
        return new();
    }

    // รอยเท้าล่าสุดของขั้นนั้น — หนึ่งขั้นมีได้หลายรอยเท้าเมื่องานวนกลับมา
    private static Task<job_subworkflow_master?> CurrentFootprintAsync(
        HRMContext db, long jobmasterid, int wlevel, CancellationToken ct)
        => db.job_subworkflow_masters
            .Where(s => s.jobmasterid == jobmasterid && s.wlevel == wlevel)
            .OrderByDescending(s => s.jobseq).ThenByDescending(s => s.jobsubworkflowid)
            .FirstOrDefaultAsync(ct);

    // ========================================================================
    //  CreateJobSubWorkflow — ประทับรอยเท้าหนึ่งแถวต่อการมาถึงหนึ่งครั้ง
    //  jobseq เป็นของรอยเท้า (job.jobseq + 1) แล้ว job คัดลอกกลับ ตามต้นฉบับ
    // ========================================================================
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
            controller = sub.controller,
            action = sub.action,
            actionEdit = sub.actionEdit,
            displayName = sub.displayName,
            starttime = DateTime.Now,
            moddate = DateTime.Now,
        };

    // ========================================================================
    //  CreateJobUserList — ออกใบงานให้ทุกคนที่เกี่ยวข้องกับขั้นนี้
    // ========================================================================
    private static List<job_user_list> CreateJobUserList(
        WorkFlowViewModel model, List<sc_user> approverList, job_master job, wf_sub_workflow_master sub,
        int? supervisorHop = null)
        => approverList.Select(u => new job_user_list
        {
            jobmasterid = job.jobmasterid,
            workflowid = job.workflowid,
            wlevel = sub.wlevel,
            subworkflowmasterid = sub.subworkflowid,
            userid = u.userid,
            empid = u.empid,
            username = $"{u.firstname} {u.lastname}".Trim(),
            orgcode = u.orgcode,
            jobstatus = Pending,
            jobseq = job.jobseq,
            isLast = true,
            sendDate = DateTime.Now,
            recievedate = DateTime.Now,
            reason = model.reason,
            reftable = sub.controller,
            emplevel = supervisorHop?.ToString(),   // ไต่หัวหน้าถึงชั้นที่เท่าไหร่แล้ว
            // ขั้นแบบ AND: แบ่งน้ำหนักเท่า ๆ กันในบรรดาผู้อนุมัติของขั้นนี้
            // (schema ไม่มีช่องน้ำหนักรายคน แบ่งเท่ากันคือค่าเริ่มต้นเดียวที่อธิบายได้)
            andPercent = sub.isandcondition && !sub.isorcondition && approverList.Count > 0
                ? Math.Round(100m / approverList.Count, 2) : null,
            isAutoApprove = false,
            moddate = DateTime.Now,
        }).ToList();
}


