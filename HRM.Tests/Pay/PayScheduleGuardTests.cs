using HRM.Services.Pay;
using Xunit;

namespace HRM.Tests.Pay;

// กติกา "ตั้งรอบจ่ายต้องแก้ยากขึ้น" (CEO, 12 ก.ย. 2569) — ส่วนที่เป็นกติกาล้วน ๆ
public class PayScheduleGuardTests
{
    [Fact]
    public void Effective_from_must_be_the_first_of_a_month()
    {
        Assert.NotNull(PayScheduleGuard.ValidateEffectiveFrom(new DateOnly(2026, 10, 15), null));
        Assert.Null(PayScheduleGuard.ValidateEffectiveFrom(new DateOnly(2026, 10, 1), null));
    }

    [Fact]
    public void Effective_from_cannot_be_earlier_than_the_month_after_the_last_approved_period()
    {
        var lastApproved = new DateOnly(2026, 9, 1);
        Assert.Equal(new DateOnly(2026, 10, 1), PayScheduleGuard.MinEffectiveFrom(lastApproved));
        Assert.NotNull(PayScheduleGuard.ValidateEffectiveFrom(new DateOnly(2026, 9, 1), lastApproved));
        Assert.Null(PayScheduleGuard.ValidateEffectiveFrom(new DateOnly(2026, 10, 1), lastApproved));
        Assert.Null(PayScheduleGuard.ValidateEffectiveFrom(new DateOnly(2027, 1, 1), lastApproved));
    }

    [Fact]
    public void Effective_to_cannot_cut_below_an_approved_period_under_the_row()
    {
        var from = new DateOnly(2026, 1, 1);
        Assert.NotNull(PayScheduleGuard.ValidateEffectiveTo(new DateOnly(2026, 8, 31), from, minEffectiveTo: new DateOnly(2026, 9, 1)));
        Assert.Null(PayScheduleGuard.ValidateEffectiveTo(new DateOnly(2026, 9, 30), from, minEffectiveTo: new DateOnly(2026, 9, 1)));
        Assert.NotNull(PayScheduleGuard.ValidateEffectiveTo(new DateOnly(2025, 12, 31), from, null));
        Assert.Null(PayScheduleGuard.ValidateEffectiveTo(null, from, new DateOnly(2026, 9, 1)));
    }

    [Fact]
    public void A_reason_is_mandatory()
    {
        Assert.NotNull(PayScheduleGuard.ValidateReason(null));
        Assert.NotNull(PayScheduleGuard.ValidateReason("  ok "));
        Assert.Null(PayScheduleGuard.ValidateReason("เปลี่ยนเป็นจ่ายครึ่งเดือนตามมติผู้บริหาร"));
    }
}
