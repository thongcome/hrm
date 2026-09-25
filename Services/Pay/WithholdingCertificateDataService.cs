using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Pay;

public record WithholdingCertificateData(
    string EmployeeName,
    string? IdCard,
    string? RegisteredAddressBlock,
    string? CompanyName,
    string? CompanyTaxId,
    string? CompanyAddress,
    int TaxYear,
    decimal TotalTaxableIncome,
    decimal TotalTaxWithheld,
    decimal TotalSocialSecurity,
    decimal TotalProvidentFund);

// Pure query/aggregation logic for Form 50-Twi (หนังสือรับรองหักภาษี ณ ที่จ่าย).
// No PDF rendering here — see WithholdingCertificatePdfService for that.
public static class WithholdingCertificateDataService
{
    public static async Task<WithholdingCertificateData?> BuildAsync(HRMContext context, long hremployeeId, int taxYear, CancellationToken ct = default)
    {
        var emp = await context.Hremployee.FirstOrDefaultAsync(e => e.id == hremployeeId, ct);
        if (emp is null) return null;

        var payEmployees = await context.Pay_PayrollEmployees
            .Include(pe => pe.Pay_PayrollRun)
            // 50 ทวิ = only pay actually made: no cancelled runs (C-04), no excluded rows (C-05)
            .Where(PayrollRunFilters.RowWasPaid)
            .Where(pe => pe.HremployeeId == hremployeeId
                && pe.Pay_PayrollRun.PayDate.Year == taxYear)   // ปีภาษีตามวันจ่าย (เกณฑ์เงินสด, audit M-01)
            .ToListAsync(ct);

        var payEmployeeIds = payEmployees.Select(pe => pe.Id).ToList();

        var nonTaxableAdhocTotal = await TaxableIncomeHelper.GetNonTaxableAdhocTotalAsync(context, payEmployeeIds, ct);

        var totalGross = payEmployees.Sum(pe => pe.GrossEarnings);
        var totalTaxableIncome = payEmployees.Sum(pe => pe.TaxableIncome);   // persisted per period (audit M3)
        var totalTaxWithheld = payEmployees.Sum(pe => pe.TaxAmount);
        var totalSsf = payEmployees.Sum(pe => pe.SocialSecurityAmount);
        var totalPf = payEmployees.Sum(pe => pe.ProvidentFundEmployeeAmount);

        // ยอดยกมาของบริษัทนี้เองก่อนเริ่มใช้ระบบกลางปี ต้องอยู่ในหนังสือรับรองของบริษัทนี้ (นายจ้างเดิมไม่รวม)
        var opening = await context.Pay_EmployeePriorEmployerIncomes
            .Where(p => p.HremployeeId == hremployeeId && p.TaxYear == taxYear && p.IsActive && p.IsSameEmployer)
            .ToListAsync(ct);
        totalTaxableIncome += opening.Sum(p => p.IncomeAmount);
        totalTaxWithheld += opening.Sum(p => p.TaxWithheldAmount);
        totalSsf += opening.Sum(p => p.SocialSecurityAmount);
        totalPf += opening.Sum(p => p.ProvidentFundAmount);

        // เดียวกัน แต่จากตาราง Pay_EmployeeOpeningBalance (พอร์ตมาจาก Advance.Payroll, CEO order
        // 22 ก.ย. 2569: mirror the payroll domain) — นำเข้าจาก Excel ชีต "ยอดยกมา" แยกจากแถวที่กรอกเองข้างบน
        var newOpenings = await context.Pay_EmployeeOpeningBalances
            .Where(o => o.HremployeeId == hremployeeId && o.TaxYear == taxYear && o.IsActive)
            .ToListAsync(ct);
        if (payEmployees.Count == 0 && opening.Count == 0 && newOpenings.Count == 0) return null;
        totalTaxableIncome += newOpenings.Sum(o => o.TaxableIncome);
        totalTaxWithheld += newOpenings.Sum(o => o.TaxWithheld);
        totalSsf += newOpenings.Sum(o => o.SsoEmployee);
        totalPf += newOpenings.Sum(o => o.PvdEmployee);

        var regAddr = await context.addresses
            .Where(a => a.hremployeeid == hremployeeId && a.address_type_id == 1 && a.isactive)
            .OrderByDescending(a => a.moddate ?? a.createdate)
            .FirstOrDefaultAsync(ct);

        var settings = await context.Pay_PayslipSettings.FirstOrDefaultAsync(s => s.CompanyId == emp.companyid, ct);

        return new WithholdingCertificateData(
            $"{emp.EmpName} {emp.EmpSurname}",
            emp.IdCard,
            FormatAddress(regAddr),
            settings?.CompanyName,
            settings?.CompanyTaxId,
            settings?.CompanyAddress,
            taxYear,
            totalTaxableIncome,
            totalTaxWithheld,
            totalSsf,
            totalPf);
    }

    private static string? FormatAddress(address? a)
    {
        if (a is null) return null;
        var parts = new[] { a.no, a.moo is null ? null : $"หมู่ {a.moo}", a.soi, a.road, a.subdistrict, a.districtid, a.province, a.postcode }
            .Where(s => !string.IsNullOrWhiteSpace(s));
        return string.Join(" ", parts);
    }
}
