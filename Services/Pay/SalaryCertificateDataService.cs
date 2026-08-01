using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Pay;

// Base employee/company facts for a salary certification letter
// (หนังสือรับรองเงินเดือน). Position/department come through as *defaults*
// only — Hremployee.PosCode/DeptgrpCode are free-text codes that are blank
// for most real employees (same unreliable-master-data situation as the
// org linkage noted elsewhere), so the generate page lets HR override them
// before printing rather than trusting them silently.
public record SalaryCertificateData(
    string EmployeeName,
    string? IdCard,
    string? PositionDefault,
    string? DepartmentDefault,
    DateTime? WorkDate,
    DateTime? ResignDate,
    decimal? SalaryAmt,
    string CompanyName,
    string? CompanyAddress);

public static class SalaryCertificateDataService
{
    public static async Task<SalaryCertificateData?> BuildAsync(HRMContext context, long hremployeeId, string companyId, CancellationToken ct = default)
    {
        var emp = await context.Hremployee.FirstOrDefaultAsync(e => e.id == hremployeeId && e.companyid == companyId, ct);
        if (emp is null) return null;

        var settings = await context.Pay_PayslipSettings.FirstOrDefaultAsync(s => s.CompanyId == companyId, ct);

        return new SalaryCertificateData(
            $"{emp.EmpName} {emp.EmpSurname}",
            emp.IdCard,
            emp.PosCode,
            emp.DeptgrpCode,
            emp.WorkDate,
            emp.ResignDate,
            emp.SalaryAmt,
            settings?.CompanyName ?? companyId,
            settings?.CompanyAddress);
    }
}
