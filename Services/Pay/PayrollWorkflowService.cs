namespace HRM.Services.Pay;

using HRM.Models;
using HRM.Services.Pay.Exceptions;
using Microsoft.EntityFrameworkCore;

// Draft -> Calculated -> Reviewed -> Approved -> Posted -> Paid (or Cancelled
// from Draft/Calculated/Reviewed). Every transition validates the current
// status first, performs its effect, and writes a Pay_PayrollAuditLog row —
// this is the workflow the legacy Hrpayroll.PayrollStatus field never
// actually implemented (it was hard-coded to 0 and never transitioned
// anywhere in Components\Pages\Payroll\PayrollProcess.razor).
public class PayrollWorkflowService
{
    private readonly IDbContextFactory<HRMContext> _dbFactory;
    private readonly PayrollCalculationService _calculationService;
    // แยกหน้าที่ (audit H6): คนส่งตรวจกับคนอนุมัติต้องเป็นคนละคน — ปิดได้ใน appsettings
    // ("Payroll:RequireSeparateApprover": false) สำหรับ dev/demo ที่มีผู้ใช้คนเดียว
    private readonly bool _requireSeparateApprover;
    private readonly HRM.Services.Workflow.WorkflowEngineService? _engine;

    // อนุมัติผ่าน workflow engine (CEO, 18 ก.ย. 2569): เมื่อ workflow PAYROLL_RUN_APPROVAL เปิดอยู่
    // "ส่งอนุมัติ" (SubmitForReview) เปิดงานที่ reftable Pay_PayrollRun ผู้อนุมัติกดใน inbox และดูทั้งรอบที่
    // /pay/runs/{refid} ผลกลับมาทาง SyncStatusFromJobAsync (WorkflowDocumentWriteback เรียกตอนงานปิด และหน้า
    // รายละเอียดเรียกซ้ำตอนเปิดเป็นตาข่ายนิรภัย) การคำนวณไม่อยู่ใน workflow
    // ไม่มี workflow ที่เปิดอยู่ = ใช้ปุ่มอนุมัติขั้นเดียวเดิม ไม่ต้องตั้งค่าอะไรสำหรับบริษัทเล็ก
    public const string ApprovalWorkflowCode = "PAYROLL_RUN_APPROVAL";
    public const string ApprovalRefTable = "Pay_PayrollRun";

    public PayrollWorkflowService(IDbContextFactory<HRMContext> dbFactory, PayrollCalculationService calculationService,
        Microsoft.Extensions.Configuration.IConfiguration? configuration = null,
        HRM.Services.Workflow.WorkflowEngineService? engine = null)
    {
        _dbFactory = dbFactory;
        _calculationService = calculationService;
        _requireSeparateApprover = PayrollSeparationOfDuties.IsRequired(configuration);
        _engine = engine;
    }

    // Single source of truth for "what buttons should be enabled" — shared by
    // the server-side guards below and the UI, so they can't drift apart.
    public static IReadOnlySet<PayrollAction> GetAllowedActions(PayrollRunStatus status) => status switch
    {
        PayrollRunStatus.Draft => new HashSet<PayrollAction> { PayrollAction.Calculate, PayrollAction.Cancel },
        PayrollRunStatus.Calculated => new HashSet<PayrollAction> { PayrollAction.Calculate, PayrollAction.SubmitForReview, PayrollAction.Cancel },
        PayrollRunStatus.Reviewed => new HashSet<PayrollAction> { PayrollAction.Approve, PayrollAction.Cancel },
        PayrollRunStatus.Approved => new HashSet<PayrollAction> { PayrollAction.Post },
        PayrollRunStatus.Posted => new HashSet<PayrollAction> { PayrollAction.MarkPaid },
        // Paid is final. A wrong payment is corrected per employee with a one-off
        // earning/deduction in the next period (/pay/adhoc), never by reopening this run.
        PayrollRunStatus.Paid => new HashSet<PayrollAction>(),
        PayrollRunStatus.Cancelled => new HashSet<PayrollAction>(),
        _ => new HashSet<PayrollAction>(),
    };

    // Only Regular/Bonus runs are workable (CEO, 17 ก.ย. 2569: no more whole-period
    // reversal/adjustment — see PayrollRunTypes). A row of any other type (Adjustment/
    // Reversal — historical data only, kept because the enum/DB rows still exist) can
    // at most be cancelled — never calculated, approved, posted or paid.
    public static IReadOnlySet<PayrollAction> GetAllowedActions(Pay_PayrollRun run)
    {
        var allowed = GetAllowedActions(run.Status);
        // ส่งเข้า workflow อนุมัติแล้ว: การอนุมัติทำที่ inbox ของผู้อนุมัติ ไม่ใช่ปุ่มในหน้านี้
        if (run.JobMasterId is not null && allowed.Contains(PayrollAction.Approve))
            allowed = allowed.Where(a => a != PayrollAction.Approve).ToHashSet();
        if (PayrollRunTypes.IsSupported(run.RunType))
            return allowed;
        return allowed.Contains(PayrollAction.Cancel)
            ? new HashSet<PayrollAction> { PayrollAction.Cancel }
            : new HashSet<PayrollAction>();
    }

    public async Task<PayrollRunCalculationSummary> CalculateAsync(long runId, long actorUserId, CancellationToken ct = default)
    {
        await using (var context = await _dbFactory.CreateDbContextAsync(ct))
            await PayrollStepPermission.EnsureAsync(context, actorUserId, PayrollAction.Calculate, ct);
        await EnsurePreflightClearAsync(runId, "คำนวณ", ct);
        return await _calculationService.CalculateAsync(runId, actorUserId, progress: null, ct: ct);
    }

    // ตรวจก่อนประมวลผลเป็นด่าน ไม่ใช่รายงานให้อ่านเล่น — ที่เดียวที่บังคับ ใช้ทั้งคำนวณตรง คำนวณเบื้องหลัง และส่งอนุมัติ
    private Task EnsurePreflightClearAsync(long runId, string action, CancellationToken ct)
        => new PayrollPreflightService(_dbFactory).EnsureClearAsync(runId, action, ct);

    public async Task SubmitForReviewAsync(long runId, long actorUserId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var run = await LoadRunOrThrowAsync(context, runId, ct);
        EnsureAllowed(run, PayrollAction.SubmitForReview);
        await PayrollStepPermission.EnsureAsync(context, actorUserId, PayrollAction.SubmitForReview, ct);
        await EnsureReadyForReviewAsync(context, run, ct);
        await EnsurePreflightClearAsync(runId, "ส่งอนุมัติ", ct);   // ข้อมูลอาจถูกแก้หลังคำนวณ

        var workflow = _engine is null ? null : await context.wf_workflows
            .FirstOrDefaultAsync(w => w.workflowcode == ApprovalWorkflowCode && w.isactive == true, ct);
        if (workflow is not null)
            await EnsureNoUnresolvedNegativePayAsync(context, runId, ct);   // ด่านเดียวกับที่ปุ่มอนุมัติใช้

        var fromStatus = run.Status;
        run.Status = PayrollRunStatus.Reviewed;
        run.ReviewedByUserId = actorUserId;
        run.ReviewedDate = DateTime.Now;

        AddTransitionLog(context, run.Id, fromStatus, PayrollRunStatus.Reviewed, actorUserId,
            workflow is null ? null : "ส่งอนุมัติผ่าน workflow");
        await context.SaveChangesAsync(ct);

        if (workflow is null) return;

        // รอบต้องเป็น Reviewed ก่อนเปิดงาน: workflow แบบอนุมัติอัตโนมัติปิดงานภายใน StartJobAsync
        // แล้ว write-back อ่านรอบทันที
        long jobId;
        try
        {
            var totalNet = await context.Pay_PayrollEmployees
                .Where(e => e.PayrollRunId == runId && !e.IsExcluded)
                .SumAsync(e => (decimal?)e.NetPay, ct) ?? 0m;
            var requesterEmpId = await context.sc_users.Where(u => u.userid == actorUserId).Select(u => u.empid).FirstOrDefaultAsync(ct);
            jobId = await _engine!.StartJobAsync(workflow.workflowid, ApprovalRefTable, runId.ToString(),
                actorUserId, requesterEmpId,
                $"อนุมัติรอบเงินเดือน {run.CompanyId} งวด {run.PayrollPeriod} ({RunTypeLabel(run.RunType)}) — สุทธิ {totalNet:N2} บาท",
                totalNet, ct);
        }
        catch (Exception ex)
        {
            // หาผู้อนุมัติไม่เจอ ฯลฯ — คืนรอบกลับ ไม่ให้ค้างสถานะ "ส่งแล้ว"
            await using var undo = await _dbFactory.CreateDbContextAsync(ct);
            var r = await LoadRunOrThrowAsync(undo, runId, ct);
            r.Status = fromStatus;
            r.ReviewedByUserId = null;
            r.ReviewedDate = null;
            AddTransitionLog(undo, runId, PayrollRunStatus.Reviewed, fromStatus, actorUserId, $"ส่งอนุมัติไม่สำเร็จ: {ex.Message}");
            await undo.SaveChangesAsync(ct);
            throw new InvalidOperationException($"ส่งเข้า workflow อนุมัติไม่สำเร็จ: {ex.Message}", ex);
        }

        await using var after = await _dbFactory.CreateDbContextAsync(ct);
        var saved = await LoadRunOrThrowAsync(after, runId, ct);
        saved.JobMasterId = jobId;
        await after.SaveChangesAsync(ct);
        await SyncStatusFromJobAsync(runId, ct);   // ครอบกรณีอนุมัติอัตโนมัติ
    }

    // นำผลของงานอนุมัติมาใส่รอบ (idempotent, ทำเฉพาะตอนรอบรออยู่ใน Reviewed) อนุมัติ = รอบล็อกเป็น Approved
    // ไม่อนุมัติ/ตีกลับ/ยกเลิก = กลับเป็น Calculated พร้อมเหตุผล ผู้เตรียมแก้แล้วส่งใหม่ (เป็นงานใหม่)
    public async Task SyncStatusFromJobAsync(long runId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var run = await context.Pay_PayrollRuns.FirstOrDefaultAsync(r => r.Id == runId, ct);
        if (run is null || run.Status != PayrollRunStatus.Reviewed) return;

        var refId = runId.ToString();
        var job = run.JobMasterId is long id
            ? await context.job_masters.FirstOrDefaultAsync(j => j.jobmasterid == id, ct)
            : await context.job_masters.Where(j => j.reftable == ApprovalRefTable && j.refid == refId)
                .OrderByDescending(j => j.jobmasterid).FirstOrDefaultAsync(ct);
        if (job is null) return;
        run.JobMasterId ??= job.jobmasterid;

        var closed = job.isJobClosed == true;
        var status = job.status;
        if (closed && status == HRM.Services.Workflow.WorkflowEngineService.StatusCompleted)
        {
            var approverId = job.approvedUserID ?? 0;
            if (_requireSeparateApprover && approverId != 0 && (approverId == run.CalculatedByUserId || approverId == run.ReviewedByUserId))
            {
                SendBack(context, run, approverId, "ผู้อนุมัติใน workflow เป็นคนเดียวกับผู้คำนวณ/ผู้ส่ง (แยกหน้าที่) — ต้องให้ผู้อนุมัติคนอื่นอนุมัติ");
            }
            else
            {
                run.Status = PayrollRunStatus.Approved;
                run.ApprovedByUserId = approverId;
                run.ApprovedDate = DateTime.Now;
                AddTransitionLog(context, run.Id, PayrollRunStatus.Reviewed, PayrollRunStatus.Approved, approverId,
                    $"อนุมัติผ่าน workflow (job #{job.jobmasterid})");
            }
        }
        else if (closed
                 || status == HRM.Services.Workflow.WorkflowEngineService.StatusReturned
                 || status == HRM.Services.Workflow.WorkflowEngineService.StatusRejected)
        {
            var reason = string.IsNullOrWhiteSpace(job.remark) ? status : job.remark;
            SendBack(context, run, job.approvedUserID ?? 0, $"ตีกลับจาก workflow (job #{job.jobmasterid}): {reason}");
            await context.SaveChangesAsync(ct);
            // งานที่ถูกตีกลับแต่ยังเปิดอยู่: ปิดทิ้ง ส่งใหม่ = งานใหม่ ทำหลังบันทึกเพราะการปิดจะยิง write-back
            // ซึ่งวนกลับมาที่นี่และต้องเห็นรอบกลับสถานะเดิมแล้ว
            if (!closed && _engine is not null)
                await _engine.CancelAsync(job.jobmasterid, job.approvedUserID ?? 0, isAdminOverride: true, "ตีกลับ — ปิดงานเดิม ส่งใหม่เป็นงานใหม่", ct);
            return;
        }

        await context.SaveChangesAsync(ct);
    }

    private static void SendBack(HRMContext context, Pay_PayrollRun run, long actorUserId, string reason)
    {
        run.Status = PayrollRunStatus.Calculated;
        run.ReviewedByUserId = null;
        run.ReviewedDate = null;
        run.JobMasterId = null;
        AddTransitionLog(context, run.Id, PayrollRunStatus.Reviewed, PayrollRunStatus.Calculated, actorUserId, reason);
    }

    private static string RunTypeLabel(PayrollRunType type) => type switch
    {
        PayrollRunType.Bonus => "โบนัส",
        PayrollRunType.FinalPay => "จ่ายผู้พ้นสภาพ",
        _ => "ปกติ",
    };

    private static async Task EnsureNoUnresolvedNegativePayAsync(HRMContext context, long runId, CancellationToken ct)
    {
        if (await context.Pay_PayrollEmployees.AnyAsync(e => e.PayrollRunId == runId && e.IsNegativeNetPayFlag && !e.IsExcluded, ct))
            throw new InvalidOperationException(
                "มีพนักงานที่เงินสุทธิติดลบและยังไม่ได้แก้หรือกันออกจากรอบ — แก้ข้อมูลแล้วคำนวณใหม่ หรือกันคนนั้นออกพร้อมเหตุผลก่อนส่งอนุมัติ");
    }

    public async Task ApproveAsync(long runId, long actorUserId, string? comment, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var run = await LoadRunOrThrowAsync(context, runId, ct);
        EnsureAllowed(run, PayrollAction.Approve);
        await PayrollStepPermission.EnsureAsync(context, actorUserId, PayrollAction.Approve, ct);
        await EnsureReadyForReviewAsync(context, run, ct);
        PayrollSeparationOfDuties.EnsureNotPreparer(run, actorUserId, "การอนุมัติ", _requireSeparateApprover);

        var hasUnresolvedNegativePay = await context.Pay_PayrollEmployees
            .AnyAsync(e => e.PayrollRunId == runId && e.IsNegativeNetPayFlag && !e.IsExcluded, ct);
        if (hasUnresolvedNegativePay)
            throw new InvalidOperationException(
                "Cannot approve: one or more employees have a negative net pay that hasn't been resolved or excluded. Fix the underlying data and recalculate, or mark the employee row excluded with a reason.");

        var fromStatus = run.Status;
        run.Status = PayrollRunStatus.Approved;
        run.ApprovedByUserId = actorUserId;
        run.ApprovedDate = DateTime.Now;

        AddTransitionLog(context, run.Id, fromStatus, PayrollRunStatus.Approved, actorUserId, comment);
        await context.SaveChangesAsync(ct);
    }

    public async Task PostAsync(long runId, long actorUserId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var run = await LoadRunOrThrowAsync(context, runId, ct);
        EnsureAllowed(run, PayrollAction.Post);
        await PayrollStepPermission.EnsureAsync(context, actorUserId, PayrollAction.Post, ct);
        PayrollSeparationOfDuties.EnsureNotPreparer(run, actorUserId, "การบันทึกบัญชี (Post)", _requireSeparateApprover);

        var fromStatus = run.Status;
        run.Status = PayrollRunStatus.Posted;
        run.PostedByUserId = actorUserId;
        run.PostedDate = DateTime.Now;

        AddTransitionLog(context, run.Id, fromStatus, PayrollRunStatus.Posted, actorUserId);
        await context.SaveChangesAsync(ct);
    }

    public async Task MarkPaidAsync(long runId, long actorUserId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var run = await LoadRunOrThrowAsync(context, runId, ct);
        EnsureAllowed(run, PayrollAction.MarkPaid);
        await PayrollStepPermission.EnsureAsync(context, actorUserId, PayrollAction.MarkPaid, ct);
        PayrollSeparationOfDuties.EnsureNotPreparer(run, actorUserId, "การยืนยันว่าจ่ายเงินแล้ว", _requireSeparateApprover);

        var fromStatus = run.Status;
        run.Status = PayrollRunStatus.Paid;
        run.PaidByUserId = actorUserId;
        run.PaidDate = DateTime.Now;

        AddTransitionLog(context, run.Id, fromStatus, PayrollRunStatus.Paid, actorUserId);
        await context.SaveChangesAsync(ct);
    }

    // กันพนักงานออกจากรอบ / เอากลับเข้า — ก่อนอนุมัติเท่านั้น (audit M11: IsExcluded ไม่เคยมีใครเขียน
    // ทั้งที่ ApproveAsync บอกให้ "กันคนที่ยอดติดลบออก")
    public async Task SetEmployeeExclusionAsync(long runId, long payrollEmployeeId, bool excluded, string? reason, long actorUserId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var run = await LoadRunOrThrowAsync(context, runId, ct);
        if (run.Status >= PayrollRunStatus.Approved)
            throw new InvalidOperationException("รอบที่อนุมัติแล้วแก้รายชื่อไม่ได้ — ถ้าต้องแก้ ให้บันทึกเงินได้/เงินหักรายครั้งในงวดถัดไป");
        if (excluded && string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("กรุณาระบุเหตุผลที่กันพนักงานออกจากรอบ");

        var row = await context.Pay_PayrollEmployees.FirstOrDefaultAsync(e => e.Id == payrollEmployeeId && e.PayrollRunId == runId, ct)
            ?? throw new InvalidOperationException("ไม่พบรายการพนักงานในรอบนี้");
        row.IsExcluded = excluded;
        row.ExcludeReason = excluded ? reason!.Trim() : null;

        // คนที่ถูกกันออกไม่ได้เงินสักบาท (ถูกตัดออกจากไฟล์ธนาคาร/สลิป/GL/ยอดสะสม) รายการที่การคำนวณ
        // "กิน" ไว้ให้เขาจึงต้องคืนทันที ไม่งั้นโบนัส/ค่าคอมหายถาวร และยอดหนี้เงินกู้ลดทั้งที่ไม่เคยหักเงิน
        // เอากลับเข้ารอบ = ผูกรายการตามแถวผลลัพธ์เดิมคืน (ถ้ารอบอื่นหยิบไปแล้วจะให้คำนวณรอบนี้ใหม่)
        if (excluded)
            await PayrollItemConsumption.ReleaseAsync(context, runId, new[] { row.HremployeeId }, ct);
        else
            await PayrollItemConsumption.ReconsumeAsync(context, runId, row.Id, row.EmpNo, ct);

        context.Pay_PayrollAuditLogs.Add(new Pay_PayrollAuditLog
        {
            PayrollRunId = runId,
            PayrollEmployeeId = payrollEmployeeId,
            EventType = PayAuditEventType.StatusTransition,
            FromStatus = run.Status,
            ToStatus = run.Status,
            ActorUserId = actorUserId,
            Comment = excluded ? $"Excluded {row.EmpNo}: {reason!.Trim()}" : $"Re-included {row.EmpNo}",
        });
        await context.SaveChangesAsync(ct);
    }

    public async Task CancelAsync(long runId, long actorUserId, string reason, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var run = await LoadRunOrThrowAsync(context, runId, ct);
        EnsureAllowed(run, PayrollAction.Cancel);
        await PayrollStepPermission.EnsureAsync(context, actorUserId, PayrollAction.Cancel, ct);

        var fromStatus = run.Status;
        run.Status = PayrollRunStatus.Cancelled;

        // คืนรายการที่รอบนี้กินไปแล้ว (โบนัส/ค่าคอม, งวดผ่อนเงินกู้, เงินเบิกล่วงหน้า) ให้รอบหน้าหยิบต่อได้
        // — กติกาเดียวกับตอนคำนวณใหม่และตอนกันคนออกจากรอบ ดู PayrollItemConsumption
        await PayrollItemConsumption.ReleaseAsync(context, runId, ct: ct);

        AddTransitionLog(context, run.Id, fromStatus, PayrollRunStatus.Cancelled, actorUserId, reason);
        await context.SaveChangesAsync(ct);

        // รอบที่รออยู่ใน workflow อนุมัติ: ปิดงานนั้นด้วย ไม่งั้นค้างใน inbox — ทำหลังบันทึก
        // เพราะการปิดยิง write-back ซึ่งต้องเห็นรอบเป็น Cancelled แล้ว
        if (run.JobMasterId is long jobId && _engine is not null)
        {
            await using var check = await _dbFactory.CreateDbContextAsync(ct);
            if (await check.job_masters.AnyAsync(j => j.jobmasterid == jobId && j.isJobClosed != true, ct))
                await _engine.CancelAsync(jobId, actorUserId, isAdminOverride: true, $"ยกเลิกรอบเงินเดือน: {reason}", ct);
        }
    }

    private static void EnsureAllowed(Pay_PayrollRun run, PayrollAction action)
    {
        if (!GetAllowedActions(run).Contains(action))
            throw new InvalidPayrollStatusTransitionException(run.Status, action.ToString());
    }

    // ส่งตรวจ/อนุมัติได้ต่อเมื่อ (1) ไม่มีงานคำนวณค้างอยู่ — ไม่งั้นงานที่ค้างจะเขียนทับ
    // รอบที่อนุมัติไปแล้ว และ (2) มีแถวพนักงานจริง — การคำนวณที่ล้มกลางทางทิ้งรอบว่างไว้
    // ในสถานะ Calculated ซึ่งเดิมอนุมัติและ post ได้ (audit H5)
    private static async Task EnsureReadyForReviewAsync(HRMContext context, Pay_PayrollRun run, CancellationToken ct)
    {
        if (run.IsCalculating)
            throw new InvalidOperationException("รอบนี้กำลังคำนวณอยู่ รอให้เสร็จก่อนจึงส่งตรวจ/อนุมัติได้");
        if (!string.IsNullOrEmpty(run.CalcError))
            throw new InvalidOperationException($"การคำนวณล่าสุดล้มเหลว ({run.CalcError}) — คำนวณใหม่ให้สำเร็จก่อน");
        if (!await context.Pay_PayrollEmployees.AnyAsync(e => e.PayrollRunId == run.Id, ct))
            throw new InvalidOperationException("รอบนี้ยังไม่มีรายการพนักงาน — คำนวณก่อนส่งตรวจ/อนุมัติ");
    }

    private static async Task<Pay_PayrollRun> LoadRunOrThrowAsync(HRMContext context, long runId, CancellationToken ct)
        => await context.Pay_PayrollRuns.FirstOrDefaultAsync(r => r.Id == runId, ct)
           ?? throw new InvalidOperationException($"Pay_PayrollRun {runId} not found.");

    private static void AddTransitionLog(HRMContext context, long runId, PayrollRunStatus fromStatus, PayrollRunStatus toStatus, long actorUserId, string? comment = null)
    {
        context.Pay_PayrollAuditLogs.Add(new Pay_PayrollAuditLog
        {
            PayrollRunId = runId,
            EventType = PayAuditEventType.StatusTransition,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            ActorUserId = actorUserId,
            Comment = comment,
        });
    }
}
