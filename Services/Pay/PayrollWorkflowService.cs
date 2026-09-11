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
        _requireSeparateApprover = configuration?.GetValue<bool?>("Payroll:RequireSeparateApprover") ?? true;
    }

    // Single source of truth for "what buttons should be enabled" — shared by
    // the server-side guards below and the UI, so they can't drift apart.
    public static IReadOnlySet<PayrollAction> GetAllowedActions(PayrollRunStatus status) => status switch
    {
        PayrollRunStatus.Draft => new HashSet<PayrollAction> { PayrollAction.Calculate, PayrollAction.Cancel },
        PayrollRunStatus.Calculated => new HashSet<PayrollAction> { PayrollAction.Calculate, PayrollAction.SubmitForReview, PayrollAction.Cancel },
        PayrollRunStatus.Reviewed => new HashSet<PayrollAction> { PayrollAction.Approve, PayrollAction.Cancel },
        PayrollRunStatus.Approved => new HashSet<PayrollAction> { PayrollAction.Post },
        PayrollRunStatus.Posted => new HashSet<PayrollAction> { PayrollAction.MarkPaid, PayrollAction.CreateAdjustment, PayrollAction.Reverse },
        PayrollRunStatus.Paid => new HashSet<PayrollAction> { PayrollAction.CreateAdjustment, PayrollAction.Reverse },
        PayrollRunStatus.Cancelled => new HashSet<PayrollAction>(),
        _ => new HashSet<PayrollAction>(),
    };

    // ชนิดของรอบตัดสิทธิ์เพิ่มจากสถานะ (11 ก.ย. 2569, audit C2/M12):
    //   รอบกลับรายการ (Reversal) = ค่าลบของรอบต้นทางเสมอ ห้าม "คำนวณใหม่" (จะกลายเป็นบวก
    //   แล้วจ่ายซ้ำ) ห้ามกลับรายการซ้อน และห้ามสร้างรอบปรับปรุงจากมัน
    public static IReadOnlySet<PayrollAction> GetAllowedActions(Pay_PayrollRun run)
    {
        var allowed = new HashSet<PayrollAction>(GetAllowedActions(run.Status));
        if (run.RunType == PayrollRunType.Reversal)
        {
            allowed.Remove(PayrollAction.Calculate);
            allowed.Remove(PayrollAction.Reverse);
            allowed.Remove(PayrollAction.CreateAdjustment);
        }
        return allowed;
    }

    public Task<PayrollRunCalculationSummary> CalculateAsync(long runId, long actorUserId, CancellationToken ct = default)
        => _calculationService.CalculateAsync(runId, actorUserId, progress: null, ct: ct);

    public async Task SubmitForReviewAsync(long runId, long actorUserId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var run = await LoadRunOrThrowAsync(context, runId, ct);
        EnsureAllowed(run, PayrollAction.SubmitForReview);
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
        await EnsureReadyForReviewAsync(context, run, ct);
        if (_requireSeparateApprover && (run.ReviewedByUserId == actorUserId || run.CalculatedByUserId == actorUserId))
            throw new InvalidOperationException(
                "ผู้อนุมัติต้องเป็นคนละคนกับผู้คำนวณ/ผู้ส่งตรวจ (แยกหน้าที่) — ให้ผู้มีสิทธิ์อีกคนเป็นผู้อนุมัติ");

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
            throw new InvalidOperationException("รอบที่อนุมัติแล้วแก้รายชื่อไม่ได้ — ใช้การกลับรายการ/รอบปรับปรุง");
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

    // Creates a brand-new Draft run linked back to the original instead of
    // mutating an already-Posted/Paid run in place — the only path to change
    // numbers once a run is locked.
    public async Task<long> CreateAdjustmentRunAsync(long originalRunId, long actorUserId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var original = await LoadRunOrThrowAsync(context, originalRunId, ct);
        EnsureAllowed(original, PayrollAction.CreateAdjustment);

        var adjustment = new Pay_PayrollRun
        {
            CompanyId = original.CompanyId,
            PayrollPeriod = original.PayrollPeriod,
            PeriodStart = original.PeriodStart,
            PeriodEnd = original.PeriodEnd,
            PayDate = original.PayDate,
            RunType = PayrollRunType.Adjustment,
            Status = PayrollRunStatus.Draft,
            AdjustmentOfRunId = original.Id,
            CreatedByUserId = actorUserId,
        };

        context.Pay_PayrollRuns.Add(adjustment);
        await context.SaveChangesAsync(ct);

        context.Pay_PayrollAuditLogs.Add(new Pay_PayrollAuditLog
        {
            PayrollRunId = adjustment.Id,
            EventType = PayAuditEventType.StatusTransition,
            ToStatus = PayrollRunStatus.Draft,
            ActorUserId = actorUserId,
            Comment = $"Adjustment run created from run #{original.Id}",
        });
        await context.SaveChangesAsync(ct);

        return adjustment.Id;
    }


    // BA item #2 — the ONLY way to change the numbers of a Posted/Paid run.
    // The original stays byte-for-byte intact (a DB trigger also refuses
    // UPDATE/DELETE on its employee/line rows once Posted); this creates a
    // linked Adjustment run whose lines are the exact negation of the
    // original's, already in Calculated status, so the two net to zero and
    // the corrected figures go into a fresh run. Standard document-reversal
    // semantics (SAP FB08 style): reverse, never edit.
    public async Task<long> CreateReversalRunAsync(long originalRunId, long actorUserId, string reason, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("ต้องระบุเหตุผลการกลับรายการ");

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var original = await LoadRunOrThrowAsync(context, originalRunId, ct);
        EnsureAllowed(original, PayrollAction.Reverse);

        if (await context.Pay_PayrollRuns.AnyAsync(r => r.AdjustmentOfRunId == originalRunId
                && r.RunType == PayrollRunType.Reversal && r.Status != PayrollRunStatus.Cancelled, ct))
            throw new InvalidOperationException($"รอบ #{originalRunId} ถูกกลับรายการไปแล้ว — กลับรายการซ้ำไม่ได้");

        var reversal = new Pay_PayrollRun
        {
            CompanyId = original.CompanyId,
            PayrollPeriod = original.PayrollPeriod,
            PeriodStart = original.PeriodStart,
            PeriodEnd = original.PeriodEnd,
            PayDate = original.PayDate,
            RunType = PayrollRunType.Reversal,
            Status = PayrollRunStatus.Calculated,
            AdjustmentOfRunId = original.Id,
            CreatedByUserId = actorUserId,
            CalculatedByUserId = actorUserId,
            CalculatedDate = DateTime.Now,
            Remark = $"REVERSAL of run #{original.Id}: {reason.Trim()}",
        };
        context.Pay_PayrollRuns.Add(reversal);
        await context.SaveChangesAsync(ct);

        var employees = await context.Pay_PayrollEmployees
            .Include(e => e.Pay_PayrollLineItems)
            .Where(e => e.PayrollRunId == originalRunId)
            .AsNoTracking()
            .ToListAsync(ct);

        foreach (var src in employees)
        {
            var neg = new Pay_PayrollEmployee
            {
                PayrollRunId = reversal.Id,
                HremployeeId = src.HremployeeId,
                EmpNo = src.EmpNo,
                CompanyId = src.CompanyId,
                ProrationFactor = src.ProrationFactor,
                WorkingDaysInPeriod = src.WorkingDaysInPeriod,
                ActualWorkingDays = src.ActualWorkingDays,
                GrossEarnings = -src.GrossEarnings,
                TotalDeductions = -src.TotalDeductions,
                NetPay = -src.NetPay,
                TaxAmount = -src.TaxAmount,
                TaxableIncome = -src.TaxableIncome,
                TaxDeductionAmount = -src.TaxDeductionAmount,
                SocialSecurityAmount = -src.SocialSecurityAmount,
                ProvidentFundEmployeeAmount = -src.ProvidentFundEmployeeAmount,
                ProvidentFundCompanyAmount = -src.ProvidentFundCompanyAmount,
                InsuranceEmployeeAmount = -src.InsuranceEmployeeAmount,
                InsuranceCompanyAmount = -src.InsuranceCompanyAmount,
                WelfareFundEmployeeAmount = -src.WelfareFundEmployeeAmount,
                WelfareFundCompanyAmount = -src.WelfareFundCompanyAmount,
                IsNegativeNetPayFlag = false,
                IsExcluded = src.IsExcluded,
                ExcludeReason = src.ExcludeReason,
                BankCode = src.BankCode,
                BankBranchCode = src.BankBranchCode,
                BankAccountNo = src.BankAccountNo,
                CostCenterCode = src.CostCenterCode,
                Remark = $"กลับรายการจากรอบ #{original.Id}",
            };
            foreach (var li in src.Pay_PayrollLineItems.OrderBy(l => l.SeqNo))
            {
                neg.Pay_PayrollLineItems.Add(new Pay_PayrollLineItem
                {
                    PayItemTypeId = li.PayItemTypeId,
                    SourceType = li.SourceType,
                    SourceRefTable = "Pay_PayrollLineItem",
                    SourceRefId = li.Id,
                    Amount = -li.Amount,
                    SignFlag = li.SignFlag,
                    SeqNo = li.SeqNo,
                    Description = $"กลับรายการ (reversal) ของรอบ #{original.Id}: {li.Description}",
                });
            }
            context.Pay_PayrollEmployees.Add(neg);
        }

        context.Pay_PayrollAuditLogs.Add(new Pay_PayrollAuditLog
        {
            PayrollRunId = reversal.Id,
            EventType = PayAuditEventType.StatusTransition,
            ToStatus = PayrollRunStatus.Calculated,
            ActorUserId = actorUserId,
            Comment = $"Reversal run created from run #{original.Id} ({employees.Count} employees): {reason.Trim()}",
        });
        context.Pay_PayrollAuditLogs.Add(new Pay_PayrollAuditLog
        {
            PayrollRunId = original.Id,
            EventType = PayAuditEventType.ManualAdjustment,
            FromStatus = original.Status,
            ToStatus = original.Status,
            ActorUserId = actorUserId,
            Comment = $"Reversed by run #{reversal.Id}: {reason.Trim()}",
        });
        await context.SaveChangesAsync(ct);
        return reversal.Id;
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
