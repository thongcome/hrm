using HRM.Models;
using HRM.Services.Pay.Calculators;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Pay;

// Bridges SeveranceCalculator (pure math) into the existing ad-hoc-pay-item
// workflow — a severance payout is just a Pay_AdhocPayItem row with the
// SEVERANCE Pay_PayItemType, so it rides the same Pending -> Approved ->
// Consumed pipeline PayrollCalculationService already handles for every
// other ad-hoc item. No changes needed there.
public class SeveranceService
{
    private const string SeveranceCode = "SEVERANCE";

    private readonly IDbContextFactory<HRMContext> _dbFactory;

    public SeveranceService(IDbContextFactory<HRMContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<SeveranceCalculator.SeveranceResult> PreviewAsync(long hremployeeId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var emp = await context.Hremployee.FirstOrDefaultAsync(e => e.id == hremployeeId, ct)
            ?? throw new InvalidOperationException("ไม่พบพนักงาน");

        if (emp.ResignDate is null)
            throw new InvalidOperationException("ต้องลงวันที่ลาออกก่อนจึงจะคำนวณค่าชดเชยได้");
        if (emp.WorkDate is null)
            throw new InvalidOperationException("พนักงานคนนี้ไม่มีวันเริ่มงาน (WorkDate) ในระบบ ไม่สามารถคำนวณอายุงานได้");
        if (emp.SalaryAmt is null || emp.SalaryAmt <= 0)
            throw new InvalidOperationException("พนักงานคนนี้ไม่มีเงินเดือนฐาน (SalaryAmt) ในระบบ ไม่สามารถคำนวณค่าชดเชยได้");

        return SeveranceCalculator.Calculate(
            DateOnly.FromDateTime(emp.WorkDate.Value),
            DateOnly.FromDateTime(emp.ResignDate.Value),
            emp.SalaryAmt.Value);
    }

    public async Task<long> SubmitAsync(long hremployeeId, string targetPeriod, decimal amount, string reason, long actorUserId, CancellationToken ct = default)
    {
        // Re-validate server-side rather than trusting a cached dialog preview
        // — WorkDate/ResignDate/SalaryAmt could have changed between preview
        // and submit.
        await PreviewAsync(hremployeeId, ct);

        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var severanceTypeId = await context.Pay_PayItemTypes
            .Where(t => t.Code == SeveranceCode)
            .Select(t => t.Id)
            .FirstOrDefaultAsync(ct);
        if (severanceTypeId == 0)
            throw new InvalidOperationException("ไม่พบประเภทรายการ SEVERANCE ในระบบ — ต้องรัน migration ก่อน");

        var alreadyExists = await context.Pay_AdhocPayItems.AnyAsync(i =>
            i.HremployeeId == hremployeeId
            && i.PayItemTypeId == severanceTypeId
            && (i.Status == PayAdhocItemStatus.Pending || i.Status == PayAdhocItemStatus.Approved || i.Status == PayAdhocItemStatus.Consumed),
            ct);
        if (alreadyExists)
            throw new InvalidOperationException("มีการยื่นค่าชดเชยสำหรับพนักงานคนนี้ไปแล้ว");

        var item = new Pay_AdhocPayItem
        {
            HremployeeId = hremployeeId,
            PayItemTypeId = severanceTypeId,
            TargetPeriod = targetPeriod,
            Amount = amount,
            // Simplification: Thai tax-exemption rules for statutory severance
            // are more nuanced (exempt up to a formula based on 300x average
            // daily wage) than this system currently models — default to
            // non-taxable, HR can override the underlying Pay_AdhocPayItem
            // row via /pay/adhoc if a specific case needs different handling.
            IsTaxable = false,
            Reason = reason,
            Status = PayAdhocItemStatus.Pending,
            RequestedByUserId = actorUserId,
        };
        context.Pay_AdhocPayItems.Add(item);
        await context.SaveChangesAsync(ct);
        return item.Id;
    }
}
