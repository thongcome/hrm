using Advance.Payroll.Contracts;
using Advance.Payroll.Domain;
using Advance.Payroll.Data;
using Microsoft.EntityFrameworkCore;

namespace Advance.Payroll.Engine;

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
    // เดิม: context.Hremployee + context.addresses โดยตรง — ตอนนี้ชื่อ/IdCard ผ่าน IEmployeeSource
    // TODO(seam): "ที่อยู่ตามทะเบียนบ้าน" (HRM's `addresses` table, address_type_id=1) has NO
    // Contracts interface — it's not one of the 7 (Hremployee/Att/HrwOt/Kptempreceive/
    // Wel+Pos/Lve holiday/workflow). RegisteredAddressBlock is left null until either
    // IEmployeeSource grows an address field or an 8th interface is added — see
    // EXTRACTION-PLAN.md "seams beyond the original 7".
    public static async Task<WithholdingCertificateData?> BuildAsync(PayrollDbContext context, IEmployeeSource employeeSource, long hremployeeId, int taxYear, CancellationToken ct = default)
    {
        var emp = (await employeeSource.GetByIdsAsync(new[] { hremployeeId }, ct)).GetValueOrDefault(hremployeeId);
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
        var totalTaxableIncome = payEmployees.Sum(pe => pe.TaxableIncome);   // persisted per period (audit M3)
        var totalTaxWithheld = payEmployees.Sum(pe => pe.TaxAmount);
        var totalSsf = payEmployees.Sum(pe => pe.SocialSecurityAmount);
        var totalPf = payEmployees.Sum(pe => pe.ProvidentFundEmployeeAmount);

        var settings = await context.Pay_PayslipSettings.FirstOrDefaultAsync(s => s.CompanyId == emp.CompanyId, ct);

        return new WithholdingCertificateData(
            $"{emp.EmpName} {emp.EmpSurname}",
            emp.IdCard,
            null, // TODO(seam): registered address — see method header comment
            settings?.CompanyName,
            settings?.CompanyTaxId,
            settings?.CompanyAddress,
            taxYear,
            totalTaxableIncome,
            totalTaxWithheld,
            totalSsf,
            totalPf);
    }
}
