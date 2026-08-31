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

        // ESS self-download of the employee's own profile documents
        // (doc_center, doctypecode="EMPLOYEE_PROFILE_DOC" — same rows HR
        // manages on /employee/personnel-profile, whose download route
        // /employee/files/doc/{id} requires Menu:PAY_ADMIN and is therefore
        // unreachable from ESS). Ownership rule: the doc's refid must be the
        // caller's own Hremployee.id (resolved via EssEmployeeResolver from
        // the empno claim), and only ACTIVE documents are served — superseded
        // versions are HR-only history.
        group.MapGet("/profile-doc/{docId:long}", async (
            long docId, HttpContext httpContext, IDbContextFactory<HRMContext> dbFactory, PrivateFileStorage storage, IAuditLogger auditLogger) =>
        {
            await using var context = await dbFactory.CreateDbContextAsync();
            var doc = await context.doc_centers.FirstOrDefaultAsync(d => d.id == docId && d.doctypecode == "EMPLOYEE_PROFILE_DOC");
            if (doc is null || string.IsNullOrWhiteSpace(doc.path) || string.IsNullOrWhiteSpace(doc.files))
                return Results.NotFound();

            var emp = await HRM.Services.Ess.EssEmployeeResolver.ResolveAsync(context, httpContext.User);
            if (emp is null || doc.refid != emp.id || doc.isActive != true)
                return Results.Forbid();

            await auditLogger.LogAccessAsync("Hremployee", emp.id.ToString(), isSensitive: true,
                note: $"ESS self-download of own profile document ({doc.files})");

            var bytes = await storage.ReadAsync(doc.path);
            return Results.File(bytes, "application/octet-stream", doc.files);
        });
    }
}
