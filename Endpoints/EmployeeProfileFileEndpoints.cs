namespace HRM.Endpoints;

using HRM.Models;
using HRM.Services.Audit;
using HRM.Services.Pay;
using Microsoft.EntityFrameworkCore;

// Download side for the "FILE UPLOAD" tab on EmployeePersonnelProfile.razor —
// doc_center rows keyed by doctypecode="EMPLOYEE_PROFILE_DOC", refid=Hremployee.id.
// Upload happens in-process inside the Razor page (Blazor Server); this is
// only the download route, mirroring WorkflowFileEndpoints.cs's
// PrivateFileStorage + IAuditLogger pattern exactly.
public static class EmployeeProfileFileEndpoints
{
    public static void MapEmployeeProfileFileEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/employee/files").RequireAuthorization("Menu:PAY_ADMIN");

        group.MapGet("/doc/{docId:long}", async (
            long docId, IDbContextFactory<HRMContext> dbFactory, PrivateFileStorage storage, IAuditLogger auditLogger) =>
        {
            await using var context = await dbFactory.CreateDbContextAsync();
            var doc = await context.doc_centers.FirstOrDefaultAsync(d => d.id == docId && d.doctypecode == "EMPLOYEE_PROFILE_DOC");
            if (doc is null || string.IsNullOrWhiteSpace(doc.path) || string.IsNullOrWhiteSpace(doc.files))
                return Results.NotFound();

            await auditLogger.LogAccessAsync("Hremployee", doc.refid?.ToString() ?? docId.ToString(), isSensitive: true,
                note: $"personnel profile document download ({doc.files})");

            var bytes = await storage.ReadAsync(doc.path);
            return Results.File(bytes, "application/octet-stream", doc.files);
        });
    }
}
