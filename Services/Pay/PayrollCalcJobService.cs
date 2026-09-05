using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Pay;

// Runs a payroll calculation as a detached server-side job (Phase A).
//
// Why this exists: PayrollRunDetail used to `await CalculateAsync(...)` inside
// the Blazor component. In Blazor Server that ties the work to the SignalR
// circuit — closing the tab, a dropped connection or a navigation disposes the
// circuit and cancels the calculation half-way through 7,000 employees, and
// there is no clean way back. Here the work runs on its own Task with its OWN
// DI scope and DbContext, driven by CancellationToken.None, so the page is just
// an observer: it can be closed and reopened and the job keeps going. Progress
// is published to PayrollCalcJobRegistry (singleton) and the in-flight flag is
// persisted on Pay_PayrollRun so other pages/browsers see it too.
//
// Registered as a SINGLETON; it must never capture a scoped service directly —
// everything scoped is resolved inside the job's own scope.
public sealed class PayrollCalcJobService(
    IServiceScopeFactory scopeFactory,
    PayrollCalcJobRegistry registry,
    ILogger<PayrollCalcJobService> logger)
{
    // Kicks off the job and returns immediately. False = a job for this run is
    // already running (the caller should just show its progress).
    public async Task<bool> StartAsync(long runId, long actorUserId)
    {
        if (!registry.TryStart(runId)) return false;

        // Mark the run as in-flight up front, on a short-lived scope, so the
        // flag is visible before the first employee is even processed.
        await using (var scope = scopeFactory.CreateAsyncScope())
        {
            var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<HRMContext>>();
            await using var ctx = await dbFactory.CreateDbContextAsync();
            var run = await ctx.Pay_PayrollRuns.FirstOrDefaultAsync(r => r.Id == runId);
            if (run is null) { registry.Fail(runId, "ไม่พบรอบเงินเดือน"); return true; }
            run.IsCalculating = true;
            run.CalcStartedAt = DateTime.Now;
            run.CalcError = null;
            await ctx.SaveChangesAsync();
        }

        // Fire-and-forget on the thread pool. Deliberately NOT awaited by the
        // caller and NOT tied to any request/circuit cancellation token.
        _ = Task.Run(() => RunJobAsync(runId, actorUserId));
        return true;
    }

    private async Task RunJobAsync(long runId, long actorUserId)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var calc = scope.ServiceProvider.GetRequiredService<PayrollCalculationService>();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<HRMContext>>();

        // Progress<T> created here has no SynchronizationContext, so the
        // callback runs inline on the worker thread — fine, the registry is
        // thread-safe and the page polls it rather than being pushed to.
        var progress = new Progress<(int Done, int Total)>(p => registry.Update(runId, p.Done, p.Total));

        try
        {
            var summary = await calc.CalculateAsync(runId, actorUserId, progress, CancellationToken.None);
            registry.Complete(runId, summary);
            await ClearInFlightAsync(dbFactory, runId, error: null);
            logger.LogInformation("Payroll run {RunId}: background calculation finished ({Count} employees).", runId, summary.EmployeeCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Payroll run {RunId}: background calculation FAILED.", runId);
            registry.Fail(runId, ex.Message);
            await ClearInFlightAsync(dbFactory, runId, error: ex.Message);
        }
    }

    private static async Task ClearInFlightAsync(IDbContextFactory<HRMContext> dbFactory, long runId, string? error)
    {
        try
        {
            await using var ctx = await dbFactory.CreateDbContextAsync();
            var run = await ctx.Pay_PayrollRuns.FirstOrDefaultAsync(r => r.Id == runId);
            if (run is null) return;
            run.IsCalculating = false;
            run.CalcError = error is null ? null : (error.Length > 1000 ? error[..1000] : error);
            await ctx.SaveChangesAsync();
        }
        catch { /* never let bookkeeping mask the real outcome */ }
    }
}
