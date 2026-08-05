namespace HRM.Endpoints;

using HRM.Models;
using HRM.Services.Audit;
using HRM.Services.Pay;
using Microsoft.EntityFrameworkCore;

// Evidence download for disciplinary cases — HR-only (unlike ExpenseFileEndpoints,
// there is no employee self-view path here: the subject of a disciplinary case is
// not meant to browse their own evidence file through this endpoint).
public static class HrFileEndpoints
{
    public static void MapHrFileEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/hr/files").RequireAuthorization("Menu:HR_DISCIPLINE_ADMIN");

        group.MapGet("/disciplinary/{docCenterId:long}", async (
            long docCenterId, IDbContextFactory<HRMContext> dbFactory, PrivateFileStorage storage, IAuditLogger auditLogger) =>
        {
            await using var context = await dbFactory.CreateDbContextAsync();
            var doc = await context.doc_centers.FirstOrDefaultAsync(d => d.id == docCenterId && d.doctypecode == "HR_DISCIPLINARY");
            if (doc?.path is null) return Results.NotFound();

            await auditLogger.LogAccessAsync("Hr_DisciplinaryCase", (doc.refid ?? 0).ToString(), isSensitive: true, note: "evidence view");

            var bytes = await storage.ReadAsync(doc.path);
            return Results.File(bytes, "application/octet-stream", doc.files ?? "evidence");
        });
    }
}
