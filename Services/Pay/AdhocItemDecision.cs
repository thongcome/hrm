using HRM.Models;

namespace HRM.Services.Pay;

// Approve / reject / cancel of a one-off pay item — one rule for every screen that offers these
// buttons (/pay/adhoc and /pay/employees/{id}/pay-items). Throws InvalidOperationException with a
// Thai message the pages show as-is.
public static class AdhocItemDecision
{
    // audit M-14: whoever keyed an item may not approve it — same rule the run approval uses
    // (Payroll:RequireSeparateApprover, see PayrollSeparationOfDuties).
    public static void Approve(Pay_AdhocPayItem item, long actorUserId, bool requireSeparateApprover, DateTime now)
    {
        EnsurePending(item);
        if (requireSeparateApprover && item.RequestedByUserId == actorUserId)
            throw new InvalidOperationException("อนุมัติรายการที่ตัวเองเป็นคนคีย์ไม่ได้ (แยกหน้าที่) — ให้ผู้มีสิทธิ์อีกคนเป็นผู้อนุมัติ");
        item.Status = PayAdhocItemStatus.Approved;
        item.ApprovedByUserId = actorUserId;
        item.ApprovedDate = now;
    }

    public static void Reject(Pay_AdhocPayItem item, long actorUserId, DateTime now)
    {
        EnsurePending(item);
        item.Status = PayAdhocItemStatus.Rejected;
        item.ApprovedByUserId = actorUserId;
        item.ApprovedDate = now;
    }

    // A consumed (already paid) item is history — cancelling it would reopen duplicate checks
    // (severance, final pay) for money that has already gone out.
    public static void Cancel(Pay_AdhocPayItem item)
    {
        if (item.Status is not (PayAdhocItemStatus.Pending or PayAdhocItemStatus.Approved))
            throw new InvalidOperationException("ยกเลิกได้เฉพาะรายการที่รออนุมัติหรืออนุมัติแล้วแต่ยังไม่ได้จ่าย");
        item.Status = PayAdhocItemStatus.Cancelled;
    }

    private static void EnsurePending(Pay_AdhocPayItem item)
    {
        if (item.Status != PayAdhocItemStatus.Pending)
            throw new InvalidOperationException("รายการนี้ไม่ได้อยู่ในสถานะรออนุมัติแล้ว");
    }
}
