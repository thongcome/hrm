namespace HRM.Endpoints;

using HRM.Models;
using HRM.Services.Audit;
using HRM.Services.Pay;
using Microsoft.EntityFrameworkCore;

// Internal-only download of a candidate's resume — the file itself is
// written by the PUBLIC apply-handler (Endpoints/CareerEndpoints.cs) via
// PrivateFileStorage + doc_center, but reading it back always requires
// Menu:REC_ADMIN, same generic-refid pattern as WorkflowFileEndpoints.cs.
public static class RecFileEndpoints
{
    public static void MapRecFileEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/rec/files").RequireAuthorization("Menu:REC_ADMIN");

        group.MapGet("/resume/{docId:long}", async (
            long docId, IDbContextFactory<HRMContext> dbFactory, PrivateFileStorage storage, IAuditLogger auditLogger) =>
        {
            await using var context = await dbFactory.CreateDbContextAsync();
            var doc = await context.doc_centers.FirstOrDefaultAsync(d => d.id == docId && d.doctypecode == "CANDIDATE_RESUME");
            if (doc is null || string.IsNullOrWhiteSpace(doc.path) || string.IsNullOrWhiteSpace(doc.files))
                return Results.NotFound();

            await auditLogger.LogAccessAsync("Rec_Candidate", doc.refid?.ToString() ?? docId.ToString(), isSensitive: true,
                note: $"candidate resume download ({doc.files})");

            var bytes = await storage.ReadAsync(doc.path);
            return Results.File(bytes, "application/octet-stream", doc.files);
        });
    }
}
