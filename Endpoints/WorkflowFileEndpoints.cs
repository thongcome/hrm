namespace HRM.Endpoints;

using HRM.Models;
using HRM.Services.Audit;
using HRM.Services.Pay;
using Microsoft.EntityFrameworkCore;

// Block 8: attachments per approval level, stored as doc_center rows
// (the existing generic document-registry table — see plan file) keyed by
// refid = job_user_list.jobapproverid, which already uniquely identifies a
// specific (job, level, approver-row), so no reftable discriminator column
// is needed. Upload happens in-process inside WfMyInbox.razor (Blazor
// Server — no HTTP endpoint required for that direction); this endpoint is
// only the download side, following the same PrivateFileStorage +
// IAuditLogger pattern as every other file route in this app.
public static class WorkflowFileEndpoints
{
    public static void MapWorkflowFileEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/wf/files").RequireAuthorization("Menu:WF_WORKFLOW_ADMIN");

        group.MapGet("/attachment/{docId:long}", async (
            long docId, HttpContext httpContext, IDbContextFactory<HRMContext> dbFactory, PrivateFileStorage storage, IAuditLogger auditLogger) =>
        {
            await using var context = await dbFactory.CreateDbContextAsync();
            var doc = await context.doc_centers.FirstOrDefaultAsync(d => d.id == docId && d.doctypecode == "WF_ATTACHMENT");
            if (doc is null || string.IsNullOrWhiteSpace(doc.path) || string.IsNullOrWhiteSpace(doc.files))
                return Results.NotFound();

            await auditLogger.LogAccessAsync("doc_center", docId.ToString(), isSensitive: false,
                note: $"workflow attachment download ({doc.files})");

            var bytes = await storage.ReadAsync(doc.path);
            return Results.File(bytes, "application/octet-stream", doc.files);
        });
    }
}
