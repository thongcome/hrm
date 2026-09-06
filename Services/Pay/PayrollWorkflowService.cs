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

    public PayrollWorkflowService(IDbContextFactory<HRMContext> dbFactory, PayrollCalculationService calculationService)
    {
        _dbFactory = dbFactory;
        _calculationService = calculationService;
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

    public Task<PayrollRunCalculationSummary> CalculateAsync(long runId, long actorUserId, CancellationToken ct = default)
        => _calculationService.CalculateAsync(runId, actorUserId, progress: null, ct: ct);

    public async Task SubmitForReviewAsync(long runId, long actorUserId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var run = await LoadRunOrThrowAsync(context, runId, ct);
        EnsureAllowed(run.Status, PayrollAction.SubmitForReview);

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
        EnsureAllowed(run.Status, PayrollAction.Approve);

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
        EnsureAllowed(run.Status, PayrollAction.Post);

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
        EnsureAllowed(run.Status, PayrollAction.MarkPaid);

        var fromStatus = run.Status;
        run.Status = PayrollRunStatus.Paid;
        run.PaidByUserId = actorUserId;
        run.PaidDate = DateTime.Now;

        AddTransitionLog(context, run.Id, fromStatus, PayrollRunStatus.Paid, actorUserId);
        await context.SaveChangesAsync(ct);
    }

    public async Task CancelAsync(long runId, long actorUserId, string reason, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var run = await LoadRunOrThrowAsync(context, runId, ct);
        EnsureAllowed(run.Status, PayrollAction.Cancel);

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
        EnsureAllowed(original.Status, PayrollAction.CreateAdjustment);

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
        EnsureAllowed(original.Status, PayrollAction.Reverse);

        if (await context.Pay_PayrollRuns.AnyAsync(r => r.AdjustmentOfRunId == originalRunId && r.Remark != null && r.Remark.StartsWith("REVERSAL") && r.Status != PayrollRunStatus.Cancelled, ct))
            throw new InvalidOperationException($"รอบ #{originalRunId} ถูกกลับรายการไปแล้ว — กลับรายการซ้ำไม่ได้");

        var reversal = new Pay_PayrollRun
        {
            CompanyId = original.CompanyId,
            PayrollPeriod = original.PayrollPeriod,
            PeriodStart = original.PeriodStart,
            PeriodEnd = original.PeriodEnd,
            PayDate = original.PayDate,
            RunType = PayrollRunType.Adjustment,
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

    private static void EnsureAllowed(PayrollRunStatus status, PayrollAction action)
    {
        if (!GetAllowedActions(status).Contains(action))
            throw new InvalidPayrollStatusTransitionException(status, action.ToString());
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
