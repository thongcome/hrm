namespace HRM.Endpoints;

using HRM.Models;
using HRM.Services.Audit;
using HRM.Services.Ess;
using HRM.Services.Hr;
using HRM.Services.Pay;
using Microsoft.EntityFrameworkCore;

// Download side of the announcement attachment flow (upload happens
// in-process inside InfoMessageAdminDetail.razor via PrivateFileStorage,
// same as the workflow attachment pattern — see WorkflowFileEndpoints.cs).
// Re-checks visibility here rather than trusting the caller reached this
// link through the UI, so guessing a docCenterId can't leak a file from an
// announcement this employee isn't a target of.
public static class InfoMessageFileEndpoints
{
    public static void MapInfoMessageFileEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/hr/files").RequireAuthorization("Menu:HR_ANNOUNCE_ACCESS");

        group.MapGet("/announcement/{docId:long}", async (
            long docId, HttpContext httpContext, IDbContextFactory<HRMContext> dbFactory,
            InfoMessageService infoService, PrivateFileStorage storage, IAuditLogger auditLogger) =>
        {
            await using var context = await dbFactory.CreateDbContextAsync();

            var doc = await context.doc_centers.FirstOrDefaultAsync(d => d.id == docId && d.doctypecode == "HR_ANNOUNCEMENT");
            if (doc is null || doc.refid is not long infoMessageId || string.IsNullOrWhiteSpace(doc.path) || string.IsNullOrWhiteSpace(doc.files))
                return Results.NotFound();

            var employee = await EssEmployeeResolver.ResolveAsync(context, httpContext.User);
            if (employee is null)
                return Results.Forbid();

            var visible = await infoService.GetVisibleAnnouncementsAsync(context, employee, pinnedOnly: false);
            if (!visible.Any(m => m.Id == infoMessageId))
                return Results.Forbid();

            await infoService.MarkDownloadedAsync(infoMessageId, employee.id, docId);
            await auditLogger.LogAccessAsync("doc_center", docId.ToString(), isSensitive: false,
                note: $"announcement attachment download ({doc.files})");

            var bytes = await storage.ReadAsync(doc.path);
            return Results.File(bytes, "application/octet-stream", doc.files);
        });

        // Separate, deliberately anonymous group for attachments on
        // announcements HR explicitly marked IsPublicAnonymous — mirrors the
        // authenticated route above but checks that flag directly instead of
        // resolving an Hremployee (there isn't one for an anonymous visitor).
        // A docId belonging to any non-public announcement is refused here
        // even if guessed, since the check is against the live DB flag, not
        // trust in how the caller reached the link.
        var publicGroup = app.MapGroup("/public/announcement-files").AllowAnonymous();

        publicGroup.MapGet("/{docId:long}", async (
            long docId, IDbContextFactory<HRMContext> dbFactory, PrivateFileStorage storage) =>
        {
            await using var context = await dbFactory.CreateDbContextAsync();

            var doc = await context.doc_centers.FirstOrDefaultAsync(d => d.id == docId && d.doctypecode == "HR_ANNOUNCEMENT");
            if (doc is null || doc.refid is not long infoMessageId || string.IsNullOrWhiteSpace(doc.path) || string.IsNullOrWhiteSpace(doc.files))
                return Results.NotFound();

            var today = DateOnly.FromDateTime(DateTime.Today);
            var isPublicAndActive = await context.info_messages.AnyAsync(m => m.Id == infoMessageId
                && m.IsPublicAnonymous
                && m.isactive == true
                && (m.startdate == null || m.startdate <= today)
                && (m.enddate == null || m.enddate >= today));
            if (!isPublicAndActive)
                return Results.Forbid();

            var bytes = await storage.ReadAsync(doc.path);
            return Results.File(bytes, "application/octet-stream", doc.files);
        });
    }
}
