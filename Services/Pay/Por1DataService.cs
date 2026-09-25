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
        // ภ.ง.ด.1 ยื่นตามเดือนที่จ่ายเงินและหักภาษีจริง (เกณฑ์เงินสด, audit M-01) — งวด ธ.ค. ที่จ่าย ม.ค. ยื่นในแบบเดือน ม.ค.
        var filingMonth = new DateOnly(int.Parse(payrollPeriod[..4]), int.Parse(payrollPeriod.Substring(4, 2)), 1);
        var nextMonth = filingMonth.AddMonths(1);
        var payEmployees = await context.Pay_PayrollEmployees
            .Include(pe => pe.Pay_PayrollRun)
            .Include(pe => pe.Hremployee)
            .Where(PayrollRunFilters.RowWasPaid)
            .Where(pe => pe.CompanyId == companyId
                && pe.Pay_PayrollRun.PayDate >= filingMonth && pe.Pay_PayrollRun.PayDate < nextMonth)
            .ToListAsync(ct);
        // กรอง "มีภาษี" หลังรวมทุกรอบของงวดต่อคน ไม่ใช่ต่อแถว (audit M4)
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
            .Where(PayrollRunFilters.RowWasPaid)
            .Where(pe => pe.CompanyId == companyId
                && pe.Pay_PayrollRun.PayDate.Year == taxYear)   // ปีภาษีตามวันจ่าย (เกณฑ์เงินสด, audit M-01)
            .ToListAsync(ct);

        var payEmployeeIds = payEmployees.Select(pe => pe.Id).ToList();
        var nonTaxableByPayEmployee = await GetNonTaxableByPayEmployeeAsync(context, payEmployeeIds, ct);

        // ยอดยกมาของบริษัทนี้เองก่อนเริ่มใช้ระบบกลางปี — ภ.ง.ด.1ก เป็นแบบสรุปทั้งปีของบริษัทนี้ จึงต้องรวมด้วย
        // (ภ.ง.ด.1 รายเดือนไม่รวม เพราะเดือนเหล่านั้นยื่นไปแล้วในระบบเดิม) — สองแหล่ง: แถว
        // Pay_EmployeePriorEmployerIncome แบบเดิม (IsSameEmployer=true) และตาราง Pay_EmployeeOpeningBalance
        // แบบใหม่ที่พอร์ตมาจาก Advance.Payroll (CEO order 22 ก.ย. 2569: mirror the payroll domain) — รวมกัน
        var opening = (await context.Pay_EmployeePriorEmployerIncomes
                .Where(p => p.TaxYear == taxYear && p.IsActive && p.IsSameEmployer && p.Hremployee.companyid == companyId)
                .ToListAsync(ct))
            .GroupBy(p => p.HremployeeId)
            .ToDictionary(g => g.Key, g => (Income: g.Sum(p => p.IncomeAmount), Tax: g.Sum(p => p.TaxWithheldAmount)));
        var newOpenings = await context.Pay_EmployeeOpeningBalances
            .Where(o => o.CompanyId == companyId && o.TaxYear == taxYear && o.IsActive)
            .GroupBy(o => o.HremployeeId)
            .Select(g => new { HremployeeId = g.Key, Taxable = g.Sum(o => o.TaxableIncome), Tax = g.Sum(o => o.TaxWithheld) })
            .ToDictionaryAsync(x => x.HremployeeId, ct);
        foreach (var (id, add) in newOpenings)
        {
            var cur = opening.GetValueOrDefault(id);
            opening[id] = (cur.Income + add.Taxable, cur.Tax + add.Tax);
        }

        if (payEmployees.Count == 0 && opening.Count == 0) return null;

        // employees whose only pay this year was an opening balance (no real run yet) still need a line
        var openingOnlyIds = opening.Keys.Except(payEmployees.Select(pe => pe.HremployeeId)).ToList();
        var openingOnlyPeople = openingOnlyIds.Count == 0 ? []
            : await context.Hremployee.Where(e => openingOnlyIds.Contains(e.id))
                .Select(e => new { e.id, e.EmpNo, e.EmpName, e.EmpSurname, e.IdCard }).ToListAsync(ct);

        var lines = payEmployees
            .GroupBy(pe => pe.HremployeeId)
            .Select(g =>
            {
                var first = g.First();
                var open = opening.GetValueOrDefault(g.Key);
                var taxableTotal = g.Sum(pe => pe.TaxableIncome) + open.Income;   // persisted per period (audit M3)
                return new Por1KorLineItem(
                    g.Key,
                    first.EmpNo ?? first.Hremployee.EmpNo,
                    $"{first.Hremployee.EmpName} {first.Hremployee.EmpSurname}",
                    first.Hremployee.IdCard,
                    taxableTotal,
                    g.Sum(pe => pe.TaxAmount) + open.Tax);
            })
            .Concat(openingOnlyPeople.Select(p =>
            {
                var open = opening[p.id];
                return new Por1KorLineItem(p.id, p.EmpNo, $"{p.EmpName} {p.EmpSurname}", p.IdCard, open.Income, open.Tax);
            }))
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
            .Join(context.Pay_AdhocPayItems, li => li.SourceRefId, a => a.Id, (li, a) => new { li.PayrollEmployeeId, li.Amount, a.IsTaxable, a.TaxExemptAmount })
            .GroupBy(x => x.PayrollEmployeeId)
            // ไม่ใช่เงินได้ = ไม่ต้องเสียภาษีทั้งก้อน + ส่วนที่ยกเว้นของรายการที่ต้องเสียภาษีบางส่วน (ดู TaxableIncomeHelper)
            .Select(g => new { PayrollEmployeeId = g.Key, Total = g.Sum(x => x.IsTaxable ? (x.TaxExemptAmount ?? 0m) : x.Amount) })
            .ToDictionaryAsync(x => x.PayrollEmployeeId, x => x.Total, ct);
    }
}
