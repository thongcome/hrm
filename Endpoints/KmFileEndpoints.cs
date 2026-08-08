namespace HRM.Endpoints;

using HRM.Models;
using HRM.Services.Audit;
using HRM.Services.Pay;
using Microsoft.EntityFrameworkCore;

// Article attachments reuse the generic doc_center registry (same pattern as
// Workflow Block 8 / WorkflowFileEndpoints.cs), keyed by
// doctypecode="KM_ARTICLE_ATTACHMENT" + refid=Km_Article.Id. Upload happens
// in-process inside ArticleAdmin.razor (Blazor Server); this endpoint is
// only the download side.
public static class KmFileEndpoints
{
    public static void MapKmFileEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/km/files").RequireAuthorization("Menu:KM_ACCESS");

        group.MapGet("/attachment/{docId:long}", async (
            long docId, HttpContext httpContext, IDbContextFactory<HRMContext> dbFactory, PrivateFileStorage storage, IAuditLogger auditLogger) =>
        {
            await using var context = await dbFactory.CreateDbContextAsync();
            var doc = await context.doc_centers.FirstOrDefaultAsync(d => d.id == docId && d.doctypecode == "KM_ARTICLE_ATTACHMENT");
            if (doc is null || doc.refid is not long articleId || string.IsNullOrWhiteSpace(doc.path) || string.IsNullOrWhiteSpace(doc.files))
                return Results.NotFound();

            var article = await context.Km_Articles.FirstOrDefaultAsync(a => a.Id == articleId);
            if (article is null)
                return Results.NotFound();

            // Published articles are visible to anyone with KM_ACCESS; Draft/Archived
            // attachments are restricted to KM_ADMIN (the article isn't listed for
            // regular readers anyway, so this just closes the direct-URL guess path).
            var isAdmin = httpContext.User.HasClaim("menu", "KM_ADMIN");
            if (article.Status != ArticleStatus.Published && !isAdmin)
                return Results.Forbid();

            await auditLogger.LogAccessAsync("Km_Article", articleId.ToString(), isSensitive: false,
                note: $"attachment download ({doc.files})");

            var bytes = await storage.ReadAsync(doc.path);
            return Results.File(bytes, "application/octet-stream", doc.files);
        });
    }
}
