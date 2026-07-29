namespace HRM.Endpoints;

using HRM.Models;
using HRM.Services.Pay;
using Microsoft.EntityFrameworkCore;

// Payslip/bank/GL files live under App_Data (outside wwwroot — see
// PrivateFileStorage), so these are the only way to reach them. Each route
// requires the same Menu:PAY_RUNS policy as the run-detail page itself.
public static class PayrollFileEndpoints
{
    public static void MapPayrollFileEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/pay/files").RequireAuthorization("Menu:PAY_RUNS");

        group.MapGet("/payslip/{payslipId:long}", async (
            long payslipId, IDbContextFactory<HRMContext> dbFactory, PrivateFileStorage storage) =>
        {
            await using var context = await dbFactory.CreateDbContextAsync();
            var payslip = await context.Pay_Payslips
                .Include(p => p.Pay_PayrollEmployee)
                .FirstOrDefaultAsync(p => p.Id == payslipId);
            if (payslip is null) return Results.NotFound();

            var bytes = await storage.ReadAsync(payslip.PdfStoragePath);
            var fileName = $"payslip_{payslip.Pay_PayrollEmployee.EmpNo}.pdf";
            return Results.File(bytes, "application/pdf", fileName);
        });

        group.MapGet("/bank-export/{batchId:long}", async (
            long batchId, IDbContextFactory<HRMContext> dbFactory, PrivateFileStorage storage) =>
        {
            await using var context = await dbFactory.CreateDbContextAsync();
            var batch = await context.Pay_BankFileExportBatches.FirstOrDefaultAsync(b => b.Id == batchId);
            if (batch is null) return Results.NotFound();

            var bytes = await storage.ReadAsync(batch.FilePath);
            return Results.File(bytes, "text/csv", $"bankfile_{batchId}.csv");
        });

        group.MapGet("/gl-export/{batchId:long}", async (
            long batchId, IDbContextFactory<HRMContext> dbFactory, PrivateFileStorage storage) =>
        {
            await using var context = await dbFactory.CreateDbContextAsync();
            var batch = await context.Pay_GLExportBatches.FirstOrDefaultAsync(b => b.Id == batchId);
            if (batch is null) return Results.NotFound();

            var bytes = await storage.ReadAsync(batch.FilePath);
            return Results.File(bytes, "text/csv", $"gl_{batchId}.csv");
        });
    }
}
