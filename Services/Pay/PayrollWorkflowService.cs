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
        PayrollRunStatus.Posted => new HashSet<PayrollAction> { PayrollAction.MarkPaid, PayrollAction.CreateAdjustment },
        PayrollRunStatus.Paid => new HashSet<PayrollAction> { PayrollAction.CreateAdjustment },
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
