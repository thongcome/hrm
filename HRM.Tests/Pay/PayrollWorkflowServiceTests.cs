using HRM.Models;
using HRM.Services.Pay;
using Xunit;

namespace HRM.Tests.Pay;

// Tests the pure GetAllowedActions lookup that both PayrollWorkflowService's
// server-side guards and the UI's button-enable logic share, so they can't
// drift apart. The full state-machine methods themselves require a real
// HRMContext (exercised manually in end-to-end verification instead — see
// the project plan) since the legacy schema isn't SQLite-compatible enough
// to spin up an in-memory HRMContext cheaply (see the removed
// OvertimeAndLoanPeriodScopingTests for why).
public class PayrollWorkflowServiceTests
{
    [Theory]
    [InlineData(PayrollRunStatus.Draft, PayrollAction.Calculate, true)]
    [InlineData(PayrollRunStatus.Draft, PayrollAction.Approve, false)]
    [InlineData(PayrollRunStatus.Calculated, PayrollAction.SubmitForReview, true)]
    [InlineData(PayrollRunStatus.Calculated, PayrollAction.Post, false)]
    [InlineData(PayrollRunStatus.Reviewed, PayrollAction.Approve, true)]
    [InlineData(PayrollRunStatus.Reviewed, PayrollAction.Calculate, false)]
    [InlineData(PayrollRunStatus.Approved, PayrollAction.Post, true)]
    [InlineData(PayrollRunStatus.Approved, PayrollAction.Calculate, false)]
    [InlineData(PayrollRunStatus.Approved, PayrollAction.Cancel, false)]
    [InlineData(PayrollRunStatus.Posted, PayrollAction.MarkPaid, true)]
    [InlineData(PayrollRunStatus.Posted, PayrollAction.CreateAdjustment, true)]
    [InlineData(PayrollRunStatus.Paid, PayrollAction.CreateAdjustment, true)]
    [InlineData(PayrollRunStatus.Paid, PayrollAction.MarkPaid, false)]
    [InlineData(PayrollRunStatus.Cancelled, PayrollAction.Calculate, false)]
    public void GetAllowedActions_matches_the_defined_state_machine(PayrollRunStatus status, PayrollAction action, bool expectedAllowed)
    {
        var allowed = PayrollWorkflowService.GetAllowedActions(status);
        Assert.Equal(expectedAllowed, allowed.Contains(action));
    }

    [Fact]
    public void A_run_cannot_be_recalculated_or_edited_once_approved()
    {
        // Approved+ statuses must never allow Calculate — this is the lock
        // that forces a CreateAdjustmentRunAsync instead of mutating history.
        foreach (var lockedStatus in new[] { PayrollRunStatus.Approved, PayrollRunStatus.Posted, PayrollRunStatus.Paid })
        {
            var allowed = PayrollWorkflowService.GetAllowedActions(lockedStatus);
            Assert.DoesNotContain(PayrollAction.Calculate, allowed);
        }
    }
}
