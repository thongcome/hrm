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
            .Where(pe => pe.HremployeeId == hremployeeId
                && pe.Pay_PayrollRun.PeriodStart.Year == taxYear
                && pe.Pay_PayrollRun.Status >= PayrollRunStatus.Approved)
            .ToListAsync(ct);

        if (payEmployees.Count == 0) return null;

        var payEmployeeIds = payEmployees.Select(pe => pe.Id).ToList();

        var nonTaxableAdhocTotal = await TaxableIncomeHelper.GetNonTaxableAdhocTotalAsync(context, payEmployeeIds, ct);

        var totalGross = payEmployees.Sum(pe => pe.GrossEarnings);
        var totalTaxableIncome = totalGross - nonTaxableAdhocTotal;
        var totalTaxWithheld = payEmployees.Sum(pe => pe.TaxAmount);
        var totalSsf = payEmployees.Sum(pe => pe.SocialSecurityAmount);
        var totalPf = payEmployees.Sum(pe => pe.ProvidentFundEmployeeAmount);

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
