namespace HRM.Services.Pay;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

public record PayslipGenerationResult(int Generated, List<string> SkippedReasons);

// Generates (or regenerates) a PDF payslip for every non-excluded employee
// on an Approved+ run, reusing PayslipPasswordService (already built for
// /pay/admin/payslip-settings) to derive each employee's password without
// ever persisting it anywhere.
public class PayslipGenerationService
{
    private readonly IDbContextFactory<HRMContext> _dbFactory;
    private readonly PrivateFileStorage _fileStorage;

    public PayslipGenerationService(IDbContextFactory<HRMContext> dbFactory, PrivateFileStorage fileStorage)
    {
        _dbFactory = dbFactory;
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
            .Include(e => e.Hremployee)
            .Include(e => e.Pay_PayrollRun)
            .Include(e => e.Pay_PayrollLineItems).ThenInclude(li => li.Pay_PayItemType)
            .Where(e => e.PayrollRunId == runId && !e.IsExcluded)
            .ToListAsync(ct);

        var skippedReasons = new List<string>();
        var generated = 0;

        foreach (var emp in employees)
        {
            var passwordResult = PayslipPasswordService.Resolve(emp.Hremployee, settings.PasswordTemplate);
            if (!passwordResult.Success)
            {
                skippedReasons.Add(passwordResult.ErrorReason!);
                continue;
            }

            var pdfBytes = PayslipPdfService.Generate(emp, emp.Pay_PayrollLineItems.ToList(), companyName, passwordResult.Password!);
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
                });
            }
            else
            {
                existing.PdfStoragePath = relativePath;
                existing.PdfSha256 = sha256;
                existing.GeneratedDate = DateTime.Now;
            }

            generated++;
        }

        await context.SaveChangesAsync(ct);
        return new PayslipGenerationResult(generated, skippedReasons);
    }
}
