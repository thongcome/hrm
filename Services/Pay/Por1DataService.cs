using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Pay;

// Per-employee line for ภ.ง.ด.1 (monthly) / ภ.ง.ด.1ก (annual) — withholding
// tax remittance forms the EMPLOYER files with the Revenue Department every
// month (ภ.ง.ด.1) plus an annual reconciliation summary (ภ.ง.ด.1ก),
// covering ALL employees paid in the period. This differs from
// WithholdingCertificateDataService (Form 50-Twi), which is issued
// per-employee as a certificate handed to that one employee.
public record Por1LineItem(
    long HremployeeId,
    string EmpNo,
    string EmployeeName,
    string? IdCard,
    decimal TaxableIncome,
    decimal TaxWithheld);

public record Por1MonthlyData(
    string CompanyName,
    string? CompanyTaxId,
    string? CompanyAddress,
    string PayrollPeriod,
    DateOnly PeriodStart,
    IReadOnlyList<Por1LineItem> Lines,
    decimal TotalTaxableIncome,
    decimal TotalTaxWithheld);

public record Por1KorLineItem(
    long HremployeeId,
    string EmpNo,
    string EmployeeName,
    string? IdCard,
    decimal TotalTaxableIncome,
    decimal TotalTaxWithheld);

public record Por1KorAnnualData(
    string CompanyName,
    string? CompanyTaxId,
    string? CompanyAddress,
    int TaxYear,
    IReadOnlyList<Por1KorLineItem> Lines,
    decimal TotalTaxableIncome,
    decimal TotalTaxWithheld);

// Pure query/aggregation logic. No PDF rendering here — see Por1PdfService.
public static class Por1DataService
{
    public static async Task<Por1MonthlyData?> BuildMonthlyAsync(HRMContext context, string companyId, string payrollPeriod, CancellationToken ct = default)
    {
        var payEmployees = await context.Pay_PayrollEmployees
            .Include(pe => pe.Pay_PayrollRun)
            .Include(pe => pe.Hremployee)
            .Where(pe => pe.CompanyId == companyId
                && pe.Pay_PayrollRun.PayrollPeriod == payrollPeriod
                && pe.Pay_PayrollRun.Status >= PayrollRunStatus.Approved
                && pe.Pay_PayrollRun.Status != PayrollRunStatus.Cancelled)
            .ToListAsync(ct);
        // กรอง "มีภาษี" หลังรวมทุกรอบของงวดต่อคน ไม่ใช่ต่อแถว — แถวของรอบกลับรายการเป็นค่าลบ ถ้ากรองต่อแถวจะหลุด
        // แล้วรอบต้นทาง + รอบปรับปรุงถูกนับซ้ำ (พบจากเทสทั้งปี 2568: ภ.ง.ด.1 ต.ค. เป็นสองเท่า)
        var withTax = payEmployees.GroupBy(pe => pe.HremployeeId).Where(g => g.Sum(x => x.TaxAmount) > 0).Select(g => g.Key).ToHashSet();
        payEmployees = payEmployees.Where(pe => withTax.Contains(pe.HremployeeId)).ToList();

        if (payEmployees.Count == 0) return null;

        var payEmployeeIds = payEmployees.Select(pe => pe.Id).ToList();
        var nonTaxableByPayEmployee = await GetNonTaxableByPayEmployeeAsync(context, payEmployeeIds, ct);

        // หนึ่งแถวต่อคนต่อเดือน (audit M4) — งวดที่มีทั้งรอบปกติและรอบโบนัสรวมยอดเป็นบรรทัดเดียวตามแบบ ภ.ง.ด.1
        var lines = payEmployees
            .GroupBy(pe => pe.HremployeeId)
            .Select(g =>
            {
                var pe = g.First();
                return new Por1LineItem(
                    g.Key,
                    pe.EmpNo ?? pe.Hremployee.EmpNo,
                    $"{pe.Hremployee.EmpName} {pe.Hremployee.EmpSurname}",
                    pe.Hremployee.IdCard,
                    g.Sum(x => x.TaxableIncome),   // persisted per period (audit M3)
                    g.Sum(x => x.TaxAmount));
            })
            .OrderBy(l => l.EmpNo)
            .ToList();

        var settings = await context.Pay_PayslipSettings.FirstOrDefaultAsync(s => s.CompanyId == companyId, ct);
        var periodStart = payEmployees.First().Pay_PayrollRun.PeriodStart;

        return new Por1MonthlyData(
            settings?.CompanyName ?? companyId,
            settings?.CompanyTaxId,
            settings?.CompanyAddress,
            payrollPeriod,
            periodStart,
            lines,
            lines.Sum(l => l.TaxableIncome),
            lines.Sum(l => l.TaxWithheld));
    }

    public static async Task<Por1KorAnnualData?> BuildAnnualAsync(HRMContext context, string companyId, int taxYear, CancellationToken ct = default)
    {
        var payEmployees = await context.Pay_PayrollEmployees
            .Include(pe => pe.Pay_PayrollRun)
            .Include(pe => pe.Hremployee)
            .Where(pe => pe.CompanyId == companyId
                && pe.Pay_PayrollRun.PeriodStart.Year == taxYear
                && pe.Pay_PayrollRun.Status >= PayrollRunStatus.Approved)
            .ToListAsync(ct);

        if (payEmployees.Count == 0) return null;

        var payEmployeeIds = payEmployees.Select(pe => pe.Id).ToList();
        var nonTaxableByPayEmployee = await GetNonTaxableByPayEmployeeAsync(context, payEmployeeIds, ct);

        var lines = payEmployees
            .GroupBy(pe => pe.HremployeeId)
            .Select(g =>
            {
                var first = g.First();
                var taxableTotal = g.Sum(pe => pe.TaxableIncome);   // persisted per period (audit M3)
                return new Por1KorLineItem(
                    g.Key,
                    first.EmpNo ?? first.Hremployee.EmpNo,
                    $"{first.Hremployee.EmpName} {first.Hremployee.EmpSurname}",
                    first.Hremployee.IdCard,
                    taxableTotal,
                    g.Sum(pe => pe.TaxAmount));
            })
            .OrderBy(l => l.EmpNo)
            .ToList();

        var settings = await context.Pay_PayslipSettings.FirstOrDefaultAsync(s => s.CompanyId == companyId, ct);

        return new Por1KorAnnualData(
            settings?.CompanyName ?? companyId,
            settings?.CompanyTaxId,
            settings?.CompanyAddress,
            taxYear,
            lines,
            lines.Sum(l => l.TotalTaxableIncome),
            lines.Sum(l => l.TotalTaxWithheld));
    }

    // Same GrossEarnings correction as TaxableIncomeHelper, but grouped per
    // Pay_PayrollEmployee row instead of summed into one grand total — both
    // BuildMonthlyAsync and BuildAnnualAsync need a per-employee breakdown,
    // not just the company-wide sum TaxableIncomeHelper returns.
    private static async Task<Dictionary<long, decimal>> GetNonTaxableByPayEmployeeAsync(HRMContext context, List<long> payEmployeeIds, CancellationToken ct)
    {
        if (payEmployeeIds.Count == 0) return new Dictionary<long, decimal>();

        return await context.Pay_PayrollLineItems
            .Where(li => payEmployeeIds.Contains(li.PayrollEmployeeId)
                && li.SourceRefTable == "Pay_AdhocPayItem"
                && li.SignFlag > 0)
            .Join(context.Pay_AdhocPayItems, li => li.SourceRefId, a => a.Id, (li, a) => new { li.PayrollEmployeeId, li.Amount, a.IsTaxable })
            .Where(x => !x.IsTaxable)
            .GroupBy(x => x.PayrollEmployeeId)
            .Select(g => new { PayrollEmployeeId = g.Key, Total = g.Sum(x => x.Amount) })
            .ToDictionaryAsync(x => x.PayrollEmployeeId, x => x.Total, ct);
    }
}
