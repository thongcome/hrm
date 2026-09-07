namespace HRM.Endpoints;

using HRM.Models;
using HRM.Services.Audit;
using HRM.Services.Perf;
using Microsoft.EntityFrameworkCore;

// Generated fresh on every request (deterministic from stored data), same
// pattern as PayrollFileEndpoints' withholding-cert/salary-cert routes —
// gated by Menu:PERF_ADMIN to match PerfInstanceDetail.razor, the only page
// that links here, rather than Menu:PAY_ADMIN like the payroll-originated
// documents.
public static class PerfFileEndpoints
{
    public static void MapPerfFileEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/perf/files").RequireAuthorization("Menu:PERF_ADMIN");

        group.MapGet("/salary-increase-order/{instanceId:long}", async (
            long instanceId, HttpContext httpContext, IDbContextFactory<HRMContext> dbFactory, IAuditLogger auditLogger) =>
        {
            var companyId = httpContext.User.FindFirst("payroll_company")?.Value;
            if (string.IsNullOrEmpty(companyId)) return Results.Forbid();

            await using var context = await dbFactory.CreateDbContextAsync();
            var data = await SalaryIncreaseOrderDataService.BuildAsync(context, instanceId, companyId);
            if (data is null) return Results.NotFound();

            await auditLogger.LogAccessAsync("SalaryIncreaseOrder", instanceId.ToString(), isSensitive: true,
                note: $"salary increase order PDF download, evaluation instance {instanceId}");

            var bytes = SalaryIncreaseOrderPdfService.Generate(data);
            return Results.File(bytes, "application/pdf", $"salary_increase_order_{instanceId}.pdf");
        });
    }
}
