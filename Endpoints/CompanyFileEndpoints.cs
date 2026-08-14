namespace HRM.Endpoints;

using HRM.Models;
using HRM.Services.Pay;
using Microsoft.EntityFrameworkCore;

// Serves com_company logos (logp_path) for inline <img> rendering on the
// company admin page — mirrors OrgChartFileEndpoints.cs (no
// fileDownloadName, so it renders inline instead of forcing a download).
public static class CompanyFileEndpoints
{
    public static void MapCompanyFileEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/company/files").RequireAuthorization("Menu:SYS_ADMIN");

        group.MapGet("/logo/{companyId:long}", async (
            long companyId, IDbContextFactory<HRMContext> dbFactory, PrivateFileStorage storage) =>
        {
            await using var context = await dbFactory.CreateDbContextAsync();
            var path = await context.com_companies
                .Where(c => c.id == companyId)
                .Select(c => c.logp_path)
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
