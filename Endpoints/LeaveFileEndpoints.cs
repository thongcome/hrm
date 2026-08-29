namespace HRM.Endpoints;

using HRM.Models;
using HRM.Services.Audit;
using HRM.Services.Ess;
using HRM.Services.Pay;
using Microsoft.EntityFrameworkCore;

// Medical-certificate download for a leave request — health data, so this is
// gated stricter than most doc_center downloads: only the request's own
// employee (via EssEmployeeResolver, same "self" check LeaveRequestList.razor
// uses) or a Menu:WF_WORKFLOW_ADMIN user may read it, checked per-request
// inside the handler rather than a single fixed RequireAuthorization policy
// (same generic-refid pattern as RecFileEndpoints.cs, but ownership-scoped).
public static class LeaveFileEndpoints
{
    public static void MapLeaveFileEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/leave-requests/files").RequireAuthorization("Menu:LEAVE_ACCESS");

        group.MapGet("/medcert/{docId:long}", async (
            long docId, HttpContext http, IDbContextFactory<HRMContext> dbFactory, PrivateFileStorage storage, IAuditLogger auditLogger) =>
        {
            await using var context = await dbFactory.CreateDbContextAsync();
            var doc = await context.doc_centers.FirstOrDefaultAsync(d => d.id == docId && d.doctypecode == "LEAVE_MEDCERT");
            if (doc is null || doc.refid is null || string.IsNullOrWhiteSpace(doc.path) || string.IsNullOrWhiteSpace(doc.files))
                return Results.NotFound();

            var request = await context.Lve_LeaveRequests.FirstOrDefaultAsync(r => r.Id == doc.refid.Value);
            if (request is null) return Results.NotFound();

            var isAdmin = http.User.HasClaim("menu", "WF_WORKFLOW_ADMIN");
            if (!isAdmin)
            {
                var self = await EssEmployeeResolver.ResolveAsync(context, http.User);
                if (self is null || self.id != request.HremployeeId)
                    return Results.Forbid();
            }

            await auditLogger.LogAccessAsync("Lve_LeaveRequest", request.Id.ToString(), isSensitive: true,
                note: $"medical certificate download ({doc.files})");

            var bytes = await storage.ReadAsync(doc.path);
            return Results.File(bytes, "application/octet-stream", doc.files);
        });
    }
}
