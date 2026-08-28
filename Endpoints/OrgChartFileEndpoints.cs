namespace HRM.Endpoints;

using HRM.Models;
using HRM.Services.Pay;
using Microsoft.EntityFrameworkCore;

// Serves employee photos (Hremployee.PhotoStoragePath) for inline <img>
// rendering — the org chart and the employee admin detail page both embed
// this URL directly in an <img src>, so unlike every other file endpoint in
// this app it must NOT set a fileDownloadName (that forces
// Content-Disposition: attachment and breaks inline rendering). Any
// authenticated user can view a photo — lower sensitivity than the
// documents under /pay/files, which stay behind Menu:PAY_ADMIN.
public static class OrgChartFileEndpoints
{
    public static void MapOrgChartFileEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/org/files").RequireAuthorization();

        group.MapGet("/employee-photo/{hremployeeId:long}", async (
            long hremployeeId, IDbContextFactory<HRMContext> dbFactory, PrivateFileStorage storage) =>
        {
            await using var context = await dbFactory.CreateDbContextAsync();
            var path = await context.Hremployee
                .Where(e => e.id == hremployeeId)
                .Select(e => e.PhotoStoragePath)
                .FirstOrDefaultAsync();
            if (string.IsNullOrWhiteSpace(path))
                return Results.NotFound();

            byte[] bytes;
            try
            {
                bytes = await storage.ReadAsync(path);
            }
            catch (FileNotFoundException)
            {
                return Results.NotFound();
            }

            var contentType = path.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? "image/png" : "image/jpeg";
            return Results.File(bytes, contentType);
        });

        // Supporting evidence (email/document) HR attached to a temporary
        // approver-delegation row (Model/toa.cs) — gated behind Menu:ORG_ADMIN
        // (organization admins only, unlike the photo route above) and
        // audit-logged on every download, same discipline as PayrollFileEndpoints.
        group.MapGet("/approver-delegation/{toaId:long}", async (
            long toaId, IDbContextFactory<HRMContext> dbFactory, PrivateFileStorage storage,
            HRM.Services.Audit.IAuditLogger auditLogger, HttpContext httpContext) =>
        {
            if (!httpContext.User.HasClaim("menu", "ORG_ADMIN"))
                return Results.Forbid();

            await using var context = await dbFactory.CreateDbContextAsync();
            var delegation = await context.toas.FirstOrDefaultAsync(d => d.toaid == toaId);
            if (delegation is null || string.IsNullOrWhiteSpace(delegation.AttachmentStoragePath))
                return Results.NotFound();

            byte[] bytes;
            try
            {
                bytes = await storage.ReadAsync(delegation.AttachmentStoragePath);
            }
            catch (FileNotFoundException)
            {
                return Results.NotFound();
            }

            await auditLogger.LogAccessAsync("toa", toaId.ToString(), isSensitive: true,
                note: $"downloaded approver-delegation attachment for org id={delegation.OrganizationId}");

            return Results.File(bytes, "application/octet-stream", delegation.AttachmentFileName);
        });
    }
}
