namespace Advance.Payroll.Engine;

using Advance.Payroll.Contracts;
using Advance.Payroll.Domain;
using Advance.Payroll.Data;
using Microsoft.EntityFrameworkCore;

public record PayslipGenerationResult(int Generated, List<string> SkippedReasons);

// Generates (or regenerates) a PDF payslip for every non-excluded employee
// on an Approved+ run, reusing PayslipPasswordService (already built for
// /pay/admin/payslip-settings) to derive each employee's password without
// ever persisting it anywhere.
public class PayslipGenerationService
{
    private readonly IDbContextFactory<PayrollDbContext> _dbFactory;
    private readonly IEmployeeSource _employeeSource;
    private readonly PrivateFileStorage _fileStorage;

    public PayslipGenerationService(IDbContextFactory<PayrollDbContext> dbFactory, IEmployeeSource employeeSource, PrivateFileStorage fileStorage)
    {
        _dbFactory = dbFactory;
        _employeeSource = employeeSource;
        _fileStorage = fileStorage;
    }

    public async Task<PayslipGenerationResult> GenerateAsync(long runId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var run = await context.Pay_PayrollRuns.FirstOrDefaultAsync(r => r.Id == runId, ct)
            ?? throw new InvalidOperationException($"Pay_PayrollRun {runId} not found.");

        if (run.Status < PayrollRunStatus.Approved || run.Status == PayrollRunStatus.Cancelled)
            throw new InvalidOperationException("สร้างสลิปได้เฉพาะรอบที่อนุมัติแล้ว (Approved ขึ้นไป) เท่านั้น");

        var settings = await context.Pay_PayslipSettings.FirstOrDefaultAsync(s => s.CompanyId == run.CompanyId, ct)
            ?? throw new InvalidOperationException($"ไม่พบการตั้งค่าสลิปสำหรับบริษัท {run.CompanyId} — กรุณาตั้งค่าที่ /pay/admin/payslip-settings ก่อน");
        var companyName = settings.CompanyName ?? run.CompanyId;

        var employees = await context.Pay_PayrollEmployees
            .Include(e => e.Pay_PayrollRun)
            .Include(e => e.Pay_PayrollLineItems).ThenInclude(li => li.Pay_PayItemType)
            .Where(e => e.PayrollRunId == runId && !e.IsExcluded)
            .ToListAsync(ct);
        // เดิม: .Include(e => e.Hremployee) — ตอนนี้ join กับ IEmployeeSource แยกต่างหาก
        var people = await _employeeSource.GetByIdsAsync(employees.Select(e => e.HremployeeId).Distinct().ToList(), ct);

        var skippedReasons = new List<string>();
        var generated = 0;

        foreach (var emp in employees)
        {
            var person = people.GetValueOrDefault(emp.HremployeeId);
            if (person is null)
            {
                skippedReasons.Add($"ไม่พบข้อมูลพนักงาน HremployeeId={emp.HremployeeId} ({emp.EmpNo}) ใน IEmployeeSource");
                continue;
            }
            var passwordResult = PayslipPasswordService.Resolve(person, settings.PasswordTemplate);
            if (!passwordResult.Success)
            {
                skippedReasons.Add(passwordResult.ErrorReason!);
                continue;
            }

            var employeeName = $"{person.EmpName} {person.EmpSurname}";
            var pdfBytes = PayslipPdfService.Generate(emp, employeeName, emp.Pay_PayrollLineItems.ToList(), companyName, passwordResult.Password!);
            var fileName = $"payslip_{runId}_{emp.EmpNo}.pdf";
            var (relativePath, sha256) = await _fileStorage.SaveAsync("payslips", fileName, pdfBytes, ct);

            var existing = await context.Pay_Payslips.FirstOrDefaultAsync(p => p.PayrollEmployeeId == emp.Id, ct);
            if (existing is null)
            {
                context.Pay_Payslips.Add(new Pay_Payslip
                {
                    PayrollEmployeeId = emp.Id,
                    PdfStoragePath = relativePath,
                    PdfSha256 = sha256,
                    GeneratedDate = DateTime.Now,
                    // IsPublishedToEmployee existed in the schema since the
                    // very first Pay_* migration but nothing ever set it —
                    // ESS reads it as the visibility gate for /ess/payslips,
                    // so a (re)generated slip becomes visible to the
                    // employee immediately, same trust level as the PDF
                    // itself being ready.
                    IsPublishedToEmployee = true,
                    PublishedDate = DateTime.Now,
                });
            }
            else
            {
                existing.PdfStoragePath = relativePath;
                existing.PdfSha256 = sha256;
                existing.GeneratedDate = DateTime.Now;
                if (!existing.IsPublishedToEmployee)
                {
                    existing.IsPublishedToEmployee = true;
                    existing.PublishedDate = DateTime.Now;
                }
            }

            generated++;
        }

        await context.SaveChangesAsync(ct);
        return new PayslipGenerationResult(generated, skippedReasons);
    }
}
