namespace HRM.Endpoints;

using HRM.Models.Reporting;
using HRM.Services.Audit;
using HRM.Services.Reporting;

// Streams a report in a chosen format. One route serves every report × every
// format: it looks the report up in the registry, runs it with the query-string
// parameters under the caller's company scope, then hands the ReportResult to
// the matching exporter. Adding a report or a format needs no change here.
public static class ReportEndpoints
{
    public static void MapReportEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/reports/export").RequireAuthorization("Menu:REPORT_CENTER");

        group.MapGet("/{code}/{format}", async (
            string code, string format, HttpContext httpContext,
            ReportRegistry registry, IAuditLogger auditLogger) =>
        {
            var report = registry.Find(code);
            if (report is null) return Results.NotFound($"ไม่พบรายงาน {code}");
            var exporter = registry.Exporter(format);
            if (exporter is null) return Results.NotFound($"ไม่รองรับรูปแบบ {format}");

            var companyId = httpContext.User.FindFirst("payroll_company")?.Value;
            if (string.IsNullOrEmpty(companyId)) return Results.Forbid();
            long.TryParse(httpContext.User.FindFirst("sc_userid")?.Value, out var userId);

            var args = httpContext.Request.Query.ToDictionary(q => q.Key, q => (string?)q.Value.ToString());
            var ctx = new ReportContext(companyId, userId);

            ReportResult result;
            try
            {
                result = await report.RunAsync(args, ctx, httpContext.RequestAborted);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }

            // Reports can expose PDPA-sensitive figures (payroll, individual grades);
            // an export is a read of that data, so log it like the file downloads do.
            await auditLogger.LogAccessAsync("Report", code, isSensitive: true, note: $"export {format}");

            var bytes = exporter.Export(result);
            var fileName = $"{code}_{DateTime.Now:yyyyMMdd_HHmm}.{exporter.Extension}";
            return Results.File(bytes, exporter.ContentType, fileName);
        });
    }
}
