using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Pay;

// Shared by every report that needs the TAXABLE (not gross) income figure:
// WithholdingCertificateDataService (Form 50-Twi, per employee, annual) and
// Por1DataService (ภ.ง.ด.1 monthly / ภ.ง.ด.1ก annual, company-wide).
//
// Pay_PayrollEmployee.GrossEarnings folds in both taxable and non-taxable
// Pay_AdhocPayItem earnings (see PayrollCalculationService — the
// taxable-only figure used for the actual progressive tax bracket calc is a
// local variable, never persisted anywhere). This reconstructs the
// correction via the SourceRefTable/SourceRefId link ad-hoc-sourced line
// items carry back to Pay_AdhocPayItem.IsTaxable, so GrossEarnings minus
// this total = taxable income.
public static class TaxableIncomeHelper
{
    public static async Task<decimal> GetNonTaxableAdhocTotalAsync(HRMContext context, List<long> payEmployeeIds, CancellationToken ct = default)
    {
        if (payEmployeeIds.Count == 0) return 0m;

        return await context.Pay_PayrollLineItems
            .Where(li => payEmployeeIds.Contains(li.PayrollEmployeeId)
                && li.SourceRefTable == "Pay_AdhocPayItem"
                && li.SignFlag > 0)
            .Join(context.Pay_AdhocPayItems, li => li.SourceRefId, a => a.Id, (li, a) => new { li.Amount, a.IsTaxable, a.TaxExemptAmount })
            // ไม่ใช่เงินได้ = รายการที่ไม่ต้องเสียภาษีทั้งก้อน + ส่วนที่ยกเว้นของรายการที่ต้องเสียภาษีบางส่วน (ค่าชดเชยเลิกจ้าง)
            // ค่าใช้จ่ายที่หักจากส่วนเกินไม่ถูกหักออกจากเงินได้ที่รายงาน — เป็นเรื่องของการคำนวณภาษี ไม่ใช่ยอดที่จ่าย
            .SumAsync(x => x.IsTaxable ? (x.TaxExemptAmount ?? 0m) : x.Amount, ct);
    }
}
