namespace HRM.Endpoints;

using HRM.Models;
using HRM.Services.Audit;
using HRM.Services.Pay;
using Microsoft.EntityFrameworkCore;

// ESS's own file download endpoint, deliberately separate from
// Endpoints/PayrollFileEndpoints.cs's /pay/files/payslip/{id} — that one
// checks the caller's payroll_company (HR: "does this belong to my
// company"), this one checks the caller's own empno (ESS: "is this MY
// payslip"). Different ownership rule, so a different route rather than
// branching one endpoint two ways.
public static class EssFileEndpoints
{
    public static void MapEssFileEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/ess/files").RequireAuthorization("Menu:ESS_ACCESS");

        group.MapGet("/payslip/{payslipId:long}", async (
            long payslipId, HttpContext httpContext, IDbContextFactory<HRMContext> dbFactory, PrivateFileStorage storage, IAuditLogger auditLogger) =>
        {
            await using var context = await dbFactory.CreateDbContextAsync();
            var payslip = await context.Pay_Payslips
                .Include(p => p.Pay_PayrollEmployee)
                .FirstOrDefaultAsync(p => p.Id == payslipId);
            if (payslip is null) return Results.NotFound();

            var empno = httpContext.User.FindFirst("empno")?.Value;
            if (string.IsNullOrWhiteSpace(empno) || payslip.Pay_PayrollEmployee.EmpNo != empno || !payslip.IsPublishedToEmployee)
                return Results.Forbid();

            await auditLogger.LogAccessAsync("Pay_Payslip", payslipId.ToString(), isSensitive: true,
                note: "ESS self-download");

            var bytes = await storage.ReadAsync(payslip.PdfStoragePath);
            var fileName = $"payslip_{payslip.Pay_PayrollEmployee.EmpNo}.pdf";
            return Results.File(bytes, "application/pdf", fileName);
        });
    }
}
