namespace HRM.Endpoints;

using HRM.Services.Hr.EmployeeImport;

// Download of the employee import template — same Menu:PAY_ADMIN policy as the employee
// admin page. The file carries only the company's lookups (banks, positions, org tree),
// never employee data, so no PDPA access log is needed here.
public static class EmployeeImportEndpoints
{
    public static void MapEmployeeImportEndpoints(this WebApplication app)
    {
        app.MapGet("/hr/employee-import/template", async (HttpContext httpContext, EmployeeImportTemplateService templates,
            IConfiguration configuration, IHostEnvironment environment, CancellationToken ct) =>
        {
            // เครื่องมือช่วงติดตั้ง — ปิดแล้วต้องปิดทั้งหน้าและลิงก์ดาวน์โหลด ไม่งั้นยังโหลดฟอร์มได้จาก URL ตรง ๆ
            if (!HRM.Services.Deploy.InstallerToolsGate.IsEnabled(configuration, environment))
                return Results.Problem(HRM.Services.Deploy.InstallerToolsGate.DisabledMessage, statusCode: StatusCodes.Status403Forbidden);

            var companyId = httpContext.User.FindFirst("payroll_company")?.Value;
            if (string.IsNullOrEmpty(companyId)) return Results.Forbid();

            var bytes = await templates.BuildAsync(companyId, ct);
            return Results.File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"EmployeeImport_{companyId}_v{EmployeeImportSchema.TemplateVersion}.xlsx");
        }).RequireAuthorization("Menu:PAY_ADMIN");
    }
}
