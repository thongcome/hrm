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

        // พ.ร.บ.คุ้มครองแรงงาน มาตรา 118-119: severance is only a legal
        // entitlement for employer-initiated termination, and not even then
        // if it falls under a มาตรา 119 exception — never for voluntary
        // resignation. SeparationType is only ever written by
        // SeparationRequestService once an Hr_SeparationRequest is approved,
        // so this also means "no approved separation request yet" blocks
        // severance the same as any other disqualifying type.
        // เกษียณ (ม.118/1) และสิ้นสุดสัญญาจ้างมีกำหนดเวลา นับเป็นการเลิกจ้างที่ต้องจ่ายค่าชดเชยเช่นกัน
        if (emp.SeparationType is not (SeparationType.TerminationOrdinary or SeparationType.Retirement or SeparationType.ContractEnd))
        {
            var reason = emp.SeparationType switch
            {
                SeparationType.VoluntaryResignation => "พนักงานคนนี้ลาออกเอง (ลาออกเอง) ไม่เข้าเงื่อนไขได้รับค่าชดเชยตามกฎหมาย",
                SeparationType.TerminationSection119 => "การเลิกจ้างนี้เข้าข่ายข้อยกเว้นตามมาตรา 119 ไม่ต้องจ่ายค่าชดเชย",
                _ => "ยังไม่มีคำขอสิ้นสุดการจ้างงานที่อนุมัติแล้วสำหรับพนักงานคนนี้ — ต้องส่งคำขอผ่านระบบอนุมัติก่อน",
            };
            throw new InvalidOperationException(reason);
        }

        if (emp.WorkDate is null)
            throw new InvalidOperationException("พนักงานคนนี้ไม่มีวันเริ่มงาน (WorkDate) ในระบบ ไม่สามารถคำนวณอายุงานได้");
        var hire = DateOnly.FromDateTime(emp.WorkDate.Value);
        var last = DateOnly.FromDateTime(emp.ResignDate.Value);
        if (emp.SalaryAmt is decimal salary && salary > 0)
            return SeveranceCalculator.Calculate(hire, last, salary);
        // ลูกจ้างรายวัน (audit H-11): ค่าจ้างรายวันอัตราสุดท้าย × วันตามมาตรา 118
        if (emp.DailyWage is decimal daily && daily > 0)
            return SeveranceCalculator.CalculateForDailyWage(hire, last, daily);
        throw new InvalidOperationException("พนักงานคนนี้ไม่มีเงินเดือนฐานหรือค่าจ้างรายวันในระบบ ไม่สามารถคำนวณค่าชดเชยได้");
    }

    // ผลภาษีของยอดที่จะจ่าย ณ งวดที่เลือก — ใช้ทั้งแสดงตัวอย่างในหน้าจอและตอนส่งจ่ายจริง (null = ไม่มีกติกาที่มีผล)
    public async Task<SeveranceTaxCalculator.Result?> PreviewTaxAsync(long hremployeeId, decimal amount, string targetPeriod, CancellationToken ct = default)
    {
        var statutory = await PreviewAsync(hremployeeId, ct);
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await ComputeTaxAsync(context, hremployeeId, amount, statutory.DailyWage, targetPeriod, ct);
    }

    private static async Task<SeveranceTaxCalculator.Result?> ComputeTaxAsync(
        HRMContext context, long hremployeeId, decimal amount, decimal dailyWage, string targetPeriod, CancellationToken ct)
    {
        if (targetPeriod.Length != 6
            || !int.TryParse(targetPeriod.AsSpan(0, 4), out var year) || !int.TryParse(targetPeriod.AsSpan(4, 2), out var month)
            || month < 1 || month > 12)
            throw new InvalidOperationException("งวดที่จ่ายต้องเป็นรูปแบบ yyyyMM");
        var asOf = new DateOnly(year, month, 1);

        var rule = await context.Pay_SeveranceTaxRules.AsNoTracking()
            .Where(r => r.IsActive && r.EffectiveFrom <= asOf)
            .OrderByDescending(r => r.EffectiveFrom)
            .FirstOrDefaultAsync(ct);
        if (rule is null) return null;

        var emp = await context.Hremployee.AsNoTracking().FirstAsync(e => e.id == hremployeeId, ct);
        var hire = DateOnly.FromDateTime(emp.WorkDate!.Value);
        var last = DateOnly.FromDateTime(emp.ResignDate!.Value);
        return SeveranceTaxCalculator.Calculate(amount, dailyWage, hire, last,
            new SeveranceTaxCalculator.RuleValues(rule.ExemptDays, rule.ExemptCap, rule.ExpensePerYear,
                rule.RemainderExpenseRate, rule.RemainderExpenseCap));
    }

    public async Task<long> SubmitAsync(long hremployeeId, string targetPeriod, decimal amount, string reason, long actorUserId,
        CancellationToken ct = default, PayrollRunType targetRunType = PayrollRunType.Regular)
    {
        // Re-validate server-side rather than trusting a cached dialog preview
        // — WorkDate/ResignDate/SalaryAmt could have changed between preview
        // and submit.
        var statutory = await PreviewAsync(hremployeeId, ct);
        // The amount is editable on screen (e.g. a more generous settlement) but may never be
        // below the ม.118 minimum (audit M-15).
        if (amount < statutory.Amount)
            throw new InvalidOperationException(
                $"ยอดค่าชดเชย {amount:N2} ต่ำกว่าที่กฎหมายกำหนด ({statutory.EntitledDays} วัน = {statutory.Amount:N2} บาท) — ห้ามจ่ายต่ำกว่านี้");

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

        // ภาษี (H-01): แบ่งยอดที่จ่ายเป็นส่วนยกเว้น / ส่วนเกิน / ค่าใช้จ่ายที่หักจากส่วนเกิน ตามกติกา Pay_SeveranceTaxRule
        // ที่มีผล ณ วันแรกของงวดที่จ่าย เก็บผลไว้บนรายการ (snapshot) ไม่คำนวณซ้ำทีหลัง
        // ไม่มีกติกาเลย (ตารางว่าง) = พฤติกรรมเดิมคือไม่คิดภาษีค่าชดเชย ให้ผู้ดูแลตั้งค่าที่ /pay/admin/severance-tax
        var tax = await ComputeTaxAsync(context, hremployeeId, amount, statutory.DailyWage, targetPeriod, ct);

        var item = new Pay_AdhocPayItem
        {
            HremployeeId = hremployeeId,
            PayItemTypeId = severanceTypeId,
            TargetPeriod = targetPeriod,
            TargetRunType = targetRunType,   // FinalPay = จ่ายในรอบจ่ายคนออก (ม.70)
            Amount = amount,
            // ยังมีส่วนเกินที่ต้องเสียภาษีเมื่อหลังหักส่วนยกเว้นแล้วยังเหลือเงินได้ (แม้ค่าใช้จ่ายทำให้ฐานภาษีเป็น 0 ก็ยังต้องรายงานเป็นเงินได้)
            IsTaxable = tax is not null && tax.Excess > 0m,
            TaxExemptAmount = tax?.Exempt,
            TaxExpenseDeductionAmount = tax?.ExpenseDeduction,
            Reason = reason,
            Status = PayAdhocItemStatus.Pending,
            RequestedByUserId = actorUserId,
        };
        context.Pay_AdhocPayItems.Add(item);
        await context.SaveChangesAsync(ct);
        return item.Id;
    }
}
