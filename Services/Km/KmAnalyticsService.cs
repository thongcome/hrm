using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Km;

// Knowledge-base intelligence the KM module lacked (it had article CRUD +
// approval only). Aggregates Km_Article for a company: how many are in each
// status, total reads, the most-read articles, the most active contributors,
// and coverage by category — so KM owners see what's actually being used and
// who's sharing knowledge. Pure read.
public class KmAnalyticsService(IDbContextFactory<HRMContext> dbFactory)
{
    public record ArticleStat(long Id, string Title, string Category, int ViewCount);
    public record AuthorStat(string Name, int Published, int TotalViews);
    public record CategoryStat(string Category, int Published, int TotalViews);

    public record KmOverview(
        int Total, int Draft, int Pending, int Published, int Archived, int TotalViews,
        List<ArticleStat> TopArticles, List<AuthorStat> TopAuthors, List<CategoryStat> Categories);

    public async Task<KmOverview> GetOverviewAsync(string companyId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var arts = await context.Km_Articles
            .Where(a => a.CompanyId == companyId)
            .Select(a => new { a.Id, a.Title, a.Status, a.CategoryId, a.AuthorHremployeeId, a.ViewCount })
            .ToListAsync(ct);
        if (arts.Count == 0)
            return new(0, 0, 0, 0, 0, 0, new(), new(), new());

        var catNames = await context.Km_ArticleCategories.Select(c => new { c.Id, c.Name })
            .ToDictionaryAsync(c => c.Id, c => c.Name, ct);
        string Cat(long? id) => id is long i && catNames.TryGetValue(i, out var n) ? n : "ไม่ระบุหมวด";

        var published = arts.Where(a => a.Status == ArticleStatus.Published).ToList();

        var authorIds = published.Select(a => a.AuthorHremployeeId).Distinct().ToList();
        var authorNames = await context.Hremployee.Where(e => authorIds.Contains(e.id))
            .Select(e => new { e.id, Name = (e.EmpName ?? "") + " " + (e.EmpSurname ?? "") })
            .ToDictionaryAsync(e => e.id, e => e.Name.Trim(), ct);

        var topArticles = published.OrderByDescending(a => a.ViewCount).Take(8)
            .Select(a => new ArticleStat(a.Id, a.Title, Cat(a.CategoryId), a.ViewCount)).ToList();

        var topAuthors = published.GroupBy(a => a.AuthorHremployeeId)
            .Select(g => new AuthorStat(authorNames.GetValueOrDefault(g.Key, $"#{g.Key}"), g.Count(), g.Sum(x => x.ViewCount)))
            .OrderByDescending(a => a.Published).ThenByDescending(a => a.TotalViews).Take(8).ToList();

        var categories = published.GroupBy(a => Cat(a.CategoryId))
            .Select(g => new CategoryStat(g.Key, g.Count(), g.Sum(x => x.ViewCount)))
            .OrderByDescending(c => c.Published).ToList();

        return new KmOverview(
            arts.Count,
            arts.Count(a => a.Status == ArticleStatus.Draft),
            arts.Count(a => a.Status == ArticleStatus.PendingApproval),
            published.Count,
            arts.Count(a => a.Status == ArticleStatus.Archived),
            published.Sum(a => a.ViewCount),
            topArticles, topAuthors, categories);
    }
}
