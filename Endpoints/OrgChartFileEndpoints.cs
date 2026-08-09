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
    }
}
