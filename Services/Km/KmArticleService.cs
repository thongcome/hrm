namespace HRM.Services.Km;

using HRM.Models;
using HRM.Services.Shared;
using Microsoft.EntityFrameworkCore;

public class KmArticleService
{
    private readonly IDbContextFactory<HRMContext> _dbFactory;

    public KmArticleService(IDbContextFactory<HRMContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<Km_Article>> SearchAsync(string companyId, string? term, long? categoryId, bool lessonsLearnedOnly, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var query = context.Km_Articles
            .Where(a => a.CompanyId == companyId && a.Status == ArticleStatus.Published);

        if (categoryId is long cid)
            query = query.Where(a => a.CategoryId == cid);
        if (lessonsLearnedOnly)
            query = query.Where(a => a.ProjectId != null);

        query = EntitySearchHelper.ApplyTextSearch(query, term, nameof(Km_Article.Title), nameof(Km_Article.Content), nameof(Km_Article.Tags));

        return await query.OrderByDescending(a => a.CreatedDate).ToListAsync(ct);
    }

    public async Task<List<Km_Article>> GetAllForAdminAsync(string companyId, string? term, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var query = context.Km_Articles.Where(a => a.CompanyId == companyId);
        query = EntitySearchHelper.ApplyTextSearch(query, term, nameof(Km_Article.Title), nameof(Km_Article.Content), nameof(Km_Article.Tags));
        return await query.OrderByDescending(a => a.CreatedDate).ToListAsync(ct);
    }

    public async Task<Km_Article?> GetByIdAsync(long id, string companyId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.Km_Articles.FirstOrDefaultAsync(a => a.Id == id && a.CompanyId == companyId, ct);
    }

    public async Task IncrementViewCountAsync(long articleId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var article = await context.Km_Articles.FirstOrDefaultAsync(a => a.Id == articleId, ct);
        if (article is null) return;
        article.ViewCount++;
        await context.SaveChangesAsync(ct);
    }
}
