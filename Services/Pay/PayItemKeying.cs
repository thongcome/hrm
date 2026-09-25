using System.Linq.Expressions;
using HRM.Models;

namespace HRM.Services.Pay;

// Which pay item types a person may key by hand as a one-off item (audit M-14/M-15).
// System-reserved types (BASE, SSO, PF, TAX, LOAN, SEVERANCE, NOTICE_PAY, LATE, ABSENT, ...) are
// produced by the engine or by their own module with its own rules — severance through
// SeveranceService (ม.118 minimum, tax split), notice pay/leave payout through FinalPayService.
// Keying them by hand skips those rules, so every manual-entry screen offers only the rest.
// Module code that creates items programmatically is not affected.
public static class PayItemKeying
{
    public static readonly Expression<Func<Pay_PayItemType, bool>> ManuallyKeyable =
        t => t.IsActive && !t.IsSystemReserved && t.Category != PayItemCategory.Informational;

    public static bool IsManuallyKeyable(Pay_PayItemType t) =>
        t.IsActive && !t.IsSystemReserved && t.Category != PayItemCategory.Informational;

    public const string NotKeyableMessage =
        "ประเภทรายการนี้ระบบคำนวณเองหรือมีหน้าจอเฉพาะ (เช่น ค่าชดเชย สินจ้างแทนการบอกกล่าว ภาษี ประกันสังคม) — คีย์เป็นรายการเฉพาะกิจไม่ได้";
}
