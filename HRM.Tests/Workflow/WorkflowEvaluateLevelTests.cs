using HRM.Models;
using HRM.Services.Workflow;
using Xunit;
using Outcome = HRM.Services.Workflow.WorkflowEngineService.LevelOutcome;

namespace HRM.Tests.Workflow;

// The multi-level approval engine (WorkflowEngineService) resolves approvers
// against a real HRMContext, but the ONE decision that determines whether a
// level completes, fails, or stays open — EvaluateLevel — is pure. It is the
// heart of the two failure modes that hurt most in an approval system: a job
// that silently STAYS STUCK in someone's inbox forever, and a job that gets
// WRONGLY APPROVED before the required approvers actually acted. These tests
// pin every branch (unanimous / OR / AND-weighted, plus the empty-level and
// both-flags-set edge cases) so a refactor of the engine can't quietly change
// who has to approve what.
public class WorkflowEvaluateLevelTests
{
    private static job_subworkflow_master Level(bool and = false, bool or = false, decimal? andPercent = null)
        => new() { isandcondition = and, isorcondition = or, andpercent = andPercent };

    private static job_user_list Row(string status, decimal? weight = null)
        => new() { jobstatus = status, andPercent = weight };

    private const string Pending = WorkflowEngineService.StatusPending;
    private const string Approved = WorkflowEngineService.StatusApproved;
    private const string Rejected = WorkflowEngineService.StatusRejected;

    // ---- empty level ----

    [Fact]
    public void No_approver_rows_is_still_pending_not_complete()
    {
        // A level with no rows must never auto-complete — that would let a job
        // skip an approval stage entirely.
        Assert.Equal(Outcome.StillPending, WorkflowEngineService.EvaluateLevel(Level(), new()));
    }

    // ---- unanimous (default: neither AND nor OR) ----

    [Fact]
    public void Unanimous_all_approved_completes()
    {
        var rows = new List<job_user_list> { Row(Approved), Row(Approved) };
        Assert.Equal(Outcome.Complete, WorkflowEngineService.EvaluateLevel(Level(), rows));
    }

    [Fact]
    public void Unanimous_one_still_pending_stays_pending()
    {
        var rows = new List<job_user_list> { Row(Approved), Row(Pending) };
        Assert.Equal(Outcome.StillPending, WorkflowEngineService.EvaluateLevel(Level(), rows));
    }

    [Fact]
    public void Unanimous_any_rejection_fails_the_level()
    {
        // One rejection sinks the whole level even if others approved — a
        // rejection outranks a still-open peer (checked before pending).
        var rows = new List<job_user_list> { Row(Approved), Row(Rejected), Row(Pending) };
        Assert.Equal(Outcome.Failed, WorkflowEngineService.EvaluateLevel(Level(), rows));
    }

    // ---- OR-condition (any one approves) ----

    [Fact]
    public void Or_any_single_approval_completes_immediately()
    {
        var rows = new List<job_user_list> { Row(Pending), Row(Approved), Row(Pending) };
        Assert.Equal(Outcome.Complete, WorkflowEngineService.EvaluateLevel(Level(or: true), rows));
    }

    [Fact]
    public void Or_all_rejected_fails()
    {
        var rows = new List<job_user_list> { Row(Rejected), Row(Rejected) };
        Assert.Equal(Outcome.Failed, WorkflowEngineService.EvaluateLevel(Level(or: true), rows));
    }

    [Fact]
    public void Or_some_rejected_some_pending_stays_open()
    {
        // Nobody approved yet, but a pending approver could still approve —
        // must not fail prematurely.
        var rows = new List<job_user_list> { Row(Rejected), Row(Pending) };
        Assert.Equal(Outcome.StillPending, WorkflowEngineService.EvaluateLevel(Level(or: true), rows));
    }

    // ---- AND-condition (weighted threshold) ----

    [Fact]
    public void And_approved_weight_reaching_threshold_completes()
    {
        var rows = new List<job_user_list> { Row(Approved, 60), Row(Pending, 40) };
        Assert.Equal(Outcome.Complete, WorkflowEngineService.EvaluateLevel(Level(and: true, andPercent: 60), rows));
    }

    [Fact]
    public void And_default_threshold_is_100_percent_when_unset()
    {
        // andpercent null => needs the full 100%. 60% approved is not enough
        // while 40% is still pending.
        var rows = new List<job_user_list> { Row(Approved, 60), Row(Pending, 40) };
        Assert.Equal(Outcome.StillPending, WorkflowEngineService.EvaluateLevel(Level(and: true), rows));
    }

    [Fact]
    public void And_remaining_votes_cannot_reach_threshold_fails_early()
    {
        // 30% approved, 20% still pending, 50% already rejected: even if the
        // pending voter approves, 50% < 80% threshold — so fail now rather than
        // leave the job stuck waiting on a vote that can't matter.
        var rows = new List<job_user_list> { Row(Approved, 30), Row(Pending, 20), Row(Rejected, 50) };
        Assert.Equal(Outcome.Failed, WorkflowEngineService.EvaluateLevel(Level(and: true, andPercent: 80), rows));
    }

    [Fact]
    public void And_still_short_but_reachable_stays_pending()
    {
        // 30% approved, 60% pending, threshold 80: not there yet, but the
        // pending votes could still get it over the line.
        var rows = new List<job_user_list> { Row(Approved, 30), Row(Pending, 60) };
        Assert.Equal(Outcome.StillPending, WorkflowEngineService.EvaluateLevel(Level(and: true, andPercent: 80), rows));
    }

    [Fact]
    public void And_wins_when_both_condition_flags_are_set()
    {
        // A misconfigured level with both flags true must resolve by AND (a
        // defined precedence), not fall through to OR. 50% approved < 100%
        // default threshold, 50% pending => still pending. OR logic would have
        // completed on the single approval — proving AND took precedence.
        var rows = new List<job_user_list> { Row(Approved, 50), Row(Pending, 50) };
        Assert.Equal(Outcome.StillPending, WorkflowEngineService.EvaluateLevel(Level(and: true, or: true), rows));
    }
}
