using HRM.Models;

namespace HRM.Services.Pay;

// Standard deduction pay item types (Category=Deduction, IsSystemReserved=true:
// SSO/PF/TAX/LOAN/INSURANCE/WELFAREFUND) must appear for every employee even
// when the calculated amount is zero (e.g. no loan this period, tax withheld
// came out nil after bracket exemptions) — HR needs to see "this was checked
// and came out to zero", not silently see nothing at all. One-off/ad-hoc
// types (ADJUST/BONUS/ADHOC_DEDUCT) are intentionally excluded — those only
// ever exist when someone actually requested one.
public static class PayrollLineItemDisplayHelper
{
    public record DisplayRow(Pay_PayrollLineItem LineItem, bool IsCalculated);

    public static List<DisplayRow> BuildDisplayRows(IReadOnlyList<Pay_PayrollLineItem> actualItems, IReadOnlyList<Pay_PayItemType> allPayItemTypes)
    {
        var rows = actualItems
            .OrderBy(li => li.SeqNo)
            .Select(li => new DisplayRow(li, true))
            .ToList();

        var presentTypeIds = actualItems.Select(li => li.PayItemTypeId).ToHashSet();
        var missingStandard = allPayItemTypes
            .Where(t => t.Category == PayItemCategory.Deduction && t.IsSystemReserved && t.IsActive && !presentTypeIds.Contains(t.Id))
            .OrderBy(t => t.SortOrder);

        foreach (var t in missingStandard)
        {
            rows.Add(new DisplayRow(new Pay_PayrollLineItem
            {
                PayItemTypeId = t.Id,
                Pay_PayItemType = t,
                Amount = 0m,
                SignFlag = t.DefaultSignFlag,
                SeqNo = int.MaxValue,
            }, false));
        }

        return rows;
    }
}
