using HRM.Models;
using HRM.Services.Pay;
using Xunit;

namespace HRM.Tests.Pay;

public class AdhocItemDecisionTests
{
    private static readonly DateTime Now = new(2026, 9, 25, 10, 0, 0);

    private static Pay_AdhocPayItem Item(PayAdhocItemStatus status = PayAdhocItemStatus.Pending, long requestedBy = 10) =>
        new() { Status = status, RequestedByUserId = requestedBy, Amount = 1000m, Reason = "test", TargetPeriod = "202609" };

    [Fact]
    public void Keyer_cannot_approve_own_item_when_separation_is_required()
    {
        var item = Item(requestedBy: 10);
        Assert.Throws<InvalidOperationException>(() => AdhocItemDecision.Approve(item, actorUserId: 10, requireSeparateApprover: true, Now));
        Assert.Equal(PayAdhocItemStatus.Pending, item.Status);
    }

    [Fact]
    public void Another_person_can_approve()
    {
        var item = Item(requestedBy: 10);
        AdhocItemDecision.Approve(item, actorUserId: 20, requireSeparateApprover: true, Now);
        Assert.Equal(PayAdhocItemStatus.Approved, item.Status);
        Assert.Equal(20, item.ApprovedByUserId);
        Assert.Equal(Now, item.ApprovedDate);
    }

    [Fact]
    public void Keyer_can_approve_own_item_only_when_separation_is_switched_off()
    {
        var item = Item(requestedBy: 10);
        AdhocItemDecision.Approve(item, actorUserId: 10, requireSeparateApprover: false, Now);
        Assert.Equal(PayAdhocItemStatus.Approved, item.Status);
    }

    [Theory]
    [InlineData(PayAdhocItemStatus.Approved)]
    [InlineData(PayAdhocItemStatus.Rejected)]
    [InlineData(PayAdhocItemStatus.Consumed)]
    public void Only_pending_items_can_be_approved_or_rejected(PayAdhocItemStatus status)
    {
        Assert.Throws<InvalidOperationException>(() => AdhocItemDecision.Approve(Item(status), 20, true, Now));
        Assert.Throws<InvalidOperationException>(() => AdhocItemDecision.Reject(Item(status), 20, Now));
    }

    [Fact]
    public void Paid_item_cannot_be_cancelled()
    {
        var item = Item(PayAdhocItemStatus.Consumed);
        Assert.Throws<InvalidOperationException>(() => AdhocItemDecision.Cancel(item));
        Assert.Equal(PayAdhocItemStatus.Consumed, item.Status);
    }

    [Theory]
    [InlineData(PayAdhocItemStatus.Pending)]
    [InlineData(PayAdhocItemStatus.Approved)]
    public void Unpaid_item_can_be_cancelled(PayAdhocItemStatus status)
    {
        var item = Item(status);
        AdhocItemDecision.Cancel(item);
        Assert.Equal(PayAdhocItemStatus.Cancelled, item.Status);
    }

    [Fact]
    public void System_reserved_and_informational_types_are_not_manually_keyable()
    {
        Assert.False(PayItemKeying.IsManuallyKeyable(new Pay_PayItemType { Code = "SEVERANCE", IsActive = true, IsSystemReserved = true, Category = PayItemCategory.Earning }));
        Assert.False(PayItemKeying.IsManuallyKeyable(new Pay_PayItemType { Code = "ADJUST", IsActive = true, Category = PayItemCategory.Informational }));
        Assert.False(PayItemKeying.IsManuallyKeyable(new Pay_PayItemType { Code = "BONUS", IsActive = false, Category = PayItemCategory.Earning }));
        Assert.True(PayItemKeying.IsManuallyKeyable(new Pay_PayItemType { Code = "BONUS", IsActive = true, Category = PayItemCategory.Earning }));
    }
}
