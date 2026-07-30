namespace HRM.Endpoints;

using HRM.Models;
using HRM.Services.Audit;
using HRM.Services.Pay;
using Microsoft.EntityFrameworkCore;

// Payslip/bank/GL files live under App_Data (outside wwwroot — see
// PrivateFileStorage), so these are the only way to reach them. Each route
// requires the same Menu:PAY_RUNS policy as the run-detail page, plus a
// company-ownership check — the menu policy alone only proves the caller
// works with SOME company's payroll, not that they own this specific file.
public static class PayrollFileEndpoints
{
    public static void MapPayrollFileEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/pay/files").RequireAuthorization("Menu:PAY_RUNS");

        group.MapGet("/payslip/{payslipId:long}", async (
            long payslipId, HttpContext httpContext, IDbContextFactory<HRMContext> dbFactory, PrivateFileStorage storage, IAuditLogger auditLogger) =>
        {
            await using var context = await dbFactory.CreateDbContextAsync();
            var payslip = await context.Pay_Payslips
                .Include(p => p.Pay_PayrollEmployee)
                .FirstOrDefaultAsync(p => p.Id == payslipId);
            if (payslip is null) return Results.NotFound();

            var companyId = httpContext.User.FindFirst("payroll_company")?.Value;
            if (payslip.Pay_PayrollEmployee.CompanyId != companyId)
                return Results.Forbid();

            await auditLogger.LogAccessAsync("Pay_Payslip", payslipId.ToString(), isSensitive: true,
                note: $"payslip PDF download for {payslip.Pay_PayrollEmployee.EmpNo}");

            var bytes = await storage.ReadAsync(payslip.PdfStoragePath);
            var fileName = $"payslip_{payslip.Pay_PayrollEmployee.EmpNo}.pdf";
            return Results.File(bytes, "application/pdf", fileName);
        });

        group.MapGet("/bank-export/{batchId:long}", async (
            long batchId, HttpContext httpContext, IDbContextFactory<HRMContext> dbFactory, PrivateFileStorage storage) =>
        {
            await using var context = await dbFactory.CreateDbContextAsync();
            var batch = await context.Pay_BankFileExportBatches
                .Include(b => b.Pay_PayrollRun)
                .FirstOrDefaultAsync(b => b.Id == batchId);
            if (batch is null) return Results.NotFound();

            var companyId = httpContext.User.FindFirst("payroll_company")?.Value;
            if (batch.Pay_PayrollRun.CompanyId != companyId)
                return Results.Forbid();

            var bytes = await storage.ReadAsync(batch.FilePath);
            return Results.File(bytes, "text/csv", $"bankfile_{batchId}.csv");
        });

        group.MapGet("/gl-export/{batchId:long}", async (
            long batchId, HttpContext httpContext, IDbContextFactory<HRMContext> dbFactory, PrivateFileStorage storage) =>
        {
            await using var context = await dbFactory.CreateDbContextAsync();
            var batch = await context.Pay_GLExportBatches
                .Include(b => b.Pay_PayrollRun)
                .FirstOrDefaultAsync(b => b.Id == batchId);
            if (batch is null) return Results.NotFound();

            var companyId = httpContext.User.FindFirst("payroll_company")?.Value;
            if (batch.Pay_PayrollRun.CompanyId != companyId)
                return Results.Forbid();

            var bytes = await storage.ReadAsync(batch.FilePath);
            return Results.File(bytes, "text/csv", $"gl_{batchId}.csv");
        });

        // Separate group: employee documents are managed under Menu:PAY_ADMIN
        // (PayrollEmployeeAdmin.razor), not Menu:PAY_RUNS like the group above.
        var employeeDocGroup = app.MapGroup("/pay/files").RequireAuthorization("Menu:PAY_ADMIN");

        employeeDocGroup.MapGet("/employee-doc/{docId:long}", async (
            long docId, HttpContext httpContext, IDbContextFactory<HRMContext> dbFactory, PrivateFileStorage storage, IAuditLogger auditLogger) =>
        {
            await using var context = await dbFactory.CreateDbContextAsync();
            var doc = await context.Pay_EmployeeDocuments
                .Include(d => d.Hremployee)
                .FirstOrDefaultAsync(d => d.Id == docId);
            if (doc is null) return Results.NotFound();

            var companyId = httpContext.User.FindFirst("payroll_company")?.Value;
            if (doc.Hremployee.companyid != companyId)
                return Results.Forbid();

            await auditLogger.LogAccessAsync("Pay_EmployeeDocument", docId.ToString(),
                isSensitive: doc.DocumentType == Pay_EmployeeDocumentType.IdCard,
                note: $"document download ({doc.DocumentType}) for {doc.Hremployee.EmpNo}");

            var bytes = await storage.ReadAsync(doc.StoragePath);
            return Results.File(bytes, "application/octet-stream", doc.FileName);
        });

        // Generated fresh on every request (deterministic from stored data,
        // not a pre-existing file in PrivateFileStorage like the routes above).
        employeeDocGroup.MapGet("/withholding-cert/{hremployeeId:long}/{taxYear:int}", async (
            long hremployeeId, int taxYear, HttpContext httpContext, IDbContextFactory<HRMContext> dbFactory, IAuditLogger auditLogger) =>
        {
            await using var context = await dbFactory.CreateDbContextAsync();
            var emp = await context.Hremployee.FirstOrDefaultAsync(e => e.id == hremployeeId);
            if (emp is null) return Results.NotFound();

            var companyId = httpContext.User.FindFirst("payroll_company")?.Value;
            if (emp.companyid != companyId)
                return Results.Forbid();

            var data = await WithholdingCertificateDataService.BuildAsync(context, hremployeeId, taxYear);
            if (data is null) return Results.NotFound();

            await auditLogger.LogAccessAsync("WithholdingCertificate", $"{hremployeeId}:{taxYear}", isSensitive: true,
                note: $"50-Twi download for {emp.EmpNo}, year {taxYear}");

            var bytes = WithholdingCertificatePdfService.Generate(data);
            return Results.File(bytes, "application/pdf", $"withholding_cert_{emp.EmpNo}_{taxYear}.pdf");
        });
    }
}
