using System.Collections.Concurrent;

namespace HRM.Services.Pay;

// In-memory, process-wide status of background payroll calculations, keyed by
// payroll run id. Registered as a SINGLETON so it outlives any one Blazor
// circuit: the page that started a calculation can be closed, and a page
// opened later (same user, another user, another browser) reads the same live
// progress. Progress % here is intentionally not persisted — a server restart
// loses it, and Pay_PayrollRun.IsCalculating/CalcStartedAt is what lets the
// app notice "a job was in flight and is no longer here" afterwards.
public sealed class PayrollCalcJobRegistry
{
    public enum JobStatus { Running, Done, Failed }

    public sealed record JobState(JobStatus Status, int Done, int Total, DateTime StartedAt, string? Error, PayrollRunCalculationSummary? Summary)
    {
        public double Percent => Total <= 0 ? 0 : Math.Round(100.0 * Done / Total, 1);
    }

    private readonly ConcurrentDictionary<long, JobState> _jobs = new();

    // Returns false when a job for this run is already running — prevents two
    // HR users (or a double-click) from calculating the same run concurrently.
    public bool TryStart(long runId)
    {
        var fresh = new JobState(JobStatus.Running, 0, 0, DateTime.Now, null, null);
        if (_jobs.TryAdd(runId, fresh)) return true;
        // A finished/failed entry may be replaced; a running one may not.
        return _jobs.TryGetValue(runId, out var cur) && cur.Status != JobStatus.Running && _jobs.TryUpdate(runId, fresh, cur);
    }

    public void Update(long runId, int done, int total)
    {
        if (_jobs.TryGetValue(runId, out var cur) && cur.Status == JobStatus.Running)
            _jobs[runId] = cur with { Done = done, Total = total };
    }

    public void Complete(long runId, PayrollRunCalculationSummary summary)
    {
        if (_jobs.TryGetValue(runId, out var cur))
            _jobs[runId] = cur with { Status = JobStatus.Done, Done = cur.Total, Summary = summary };
    }

    public void Fail(long runId, string error)
    {
        if (_jobs.TryGetValue(runId, out var cur))
            _jobs[runId] = cur with { Status = JobStatus.Failed, Error = error };
    }

    public JobState? Get(long runId) => _jobs.TryGetValue(runId, out var s) ? s : null;

    public bool IsRunning(long runId) => Get(runId)?.Status == JobStatus.Running;

    // Called by the page once it has shown a finished/failed result, so a stale
    // entry doesn't linger forever.
    public void Clear(long runId) => _jobs.TryRemove(runId, out _);
}
