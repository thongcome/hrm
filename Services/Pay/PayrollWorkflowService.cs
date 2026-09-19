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

    public PayrollWorkflowService(IDbContextFactory<HRMContext> dbFactory, PayrollCalculationService calculationService,
        Microsoft.Extensions.Configuration.IConfiguration? configuration = null)
    {
        _dbFactory = dbFactory;
        _calculationService = calculationService;
        _requireSeparateApprover = PayrollSeparationOfDuties.IsRequired(configuration);
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
        return await _calculationService.CalculateAsync(runId, actorUserId, progress: null, ct: ct);
    }

    public async Task SubmitForReviewAsync(long runId, long actorUserId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var run = await LoadRunOrThrowAsync(context, runId, ct);
        EnsureAllowed(run, PayrollAction.SubmitForReview);
        await PayrollStepPermission.EnsureAsync(context, actorUserId, PayrollAction.SubmitForReview, ct);
        await EnsureReadyForReviewAsync(context, run, ct);

        var fromStatus = run.Status;
        run.Status = PayrollRunStatus.Reviewed;
        run.ReviewedByUserId = actorUserId;
        run.ReviewedDate = DateTime.Now;

        AddTransitionLog(context, run.Id, fromStatus, PayrollRunStatus.Reviewed, actorUserId);
        await context.SaveChangesAsync(ct);
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

        // release any ad-hoc items this run had consumed back to Approved so
        // they can be picked up by a future run for the same period instead
        // of being stranded forever in Consumed status
        var consumedItems = await context.Pay_AdhocPayItems
            .Where(a => a.ConsumedByPayrollRunId == runId)
            .ToListAsync(ct);
        foreach (var item in consumedItems)
        {
            item.Status = PayAdhocItemStatus.Approved;
            item.ConsumedByPayrollRunId = null;
        }
        // same for salary advances this run had recovered
        var consumedAdvances = await context.Pay_SalaryAdvances
            .Where(a => a.ConsumedByPayrollRunId == runId)
            .ToListAsync(ct);
        foreach (var adv in consumedAdvances)
        {
            adv.Status = PaySalaryAdvanceStatus.Approved;
            adv.ConsumedByPayrollRunId = null;
        }
        // และงวดผ่อนเงินกู้ที่รอบนี้หักไปแล้ว (audit H2): เดิมไม่คืน งวดนั้นหายไปจากการหักตลอดกาล
        // เพราะยอดคงเหลือของเงินกู้ถูกลดไปแล้วและรอบใหม่หยิบเฉพาะงวด Pending
        var consumedInstallments = await context.Pay_EmployeeLoanInstallments
            .Include(i => i.Pay_EmployeeLoan)
            .Where(i => i.ConsumedByPayrollRunId == runId)
            .ToListAsync(ct);
        foreach (var inst in consumedInstallments)
        {
            inst.Status = Pay_LoanInstallmentStatus.Pending;
            inst.ConsumedByPayrollRunId = null;
            inst.Pay_EmployeeLoan.RemainingBalance = inst.BalanceAfter + inst.Amount;
            if (inst.Pay_EmployeeLoan.Status == Pay_EmployeeLoanStatus.PaidOff)
                inst.Pay_EmployeeLoan.Status = Pay_EmployeeLoanStatus.Active;
        }

        AddTransitionLog(context, run.Id, fromStatus, PayrollRunStatus.Cancelled, actorUserId, reason);
        await context.SaveChangesAsync(ct);
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
