namespace HRM.Services.Km;

using HRM.Models;
using HRM.Services.Shared;
using HRM.Services.Workflow;
using Microsoft.EntityFrameworkCore;

public class KmArticleService
{
    private readonly IDbContextFactory<HRMContext> _dbFactory;
    private readonly WorkflowEngineService _engine;

    public KmArticleService(IDbContextFactory<HRMContext> dbFactory, WorkflowEngineService engine)
    {
        _dbFactory = dbFactory;
        _engine = engine;
    }

    public async Task<List<Km_Article>> SearchAsync(string companyId, string? term, long? categoryId, bool lessonsLearnedOnly, bool sortByMostViewed = false, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var query = context.Km_Articles
            .Where(a => a.CompanyId == companyId && a.Status == ArticleStatus.Published);

        if (categoryId is long cid)
            query = query.Where(a => a.CategoryId == cid);
        if (lessonsLearnedOnly)
            query = query.Where(a => a.ProjectId != null);

        query = EntitySearchHelper.ApplyTextSearch(query, term, nameof(Km_Article.Title), nameof(Km_Article.Content), nameof(Km_Article.Tags));

        query = sortByMostViewed
            ? query.OrderByDescending(a => a.ViewCount).ThenByDescending(a => a.CreatedDate)
            : query.OrderByDescending(a => a.CreatedDate);

        return await query.ToListAsync(ct);
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

    // Publish routing through the generic Workflow Engine — mirrors
    // Services/Idp/IdpPlanService.cs's SubmitForApprovalAsync /
    // SyncStatusFromJobAsync pair exactly (no scheduler exists anywhere in
    // this codebase, so status is only ever pulled from the job on read).
    public async Task<long> SubmitForApprovalAsync(long articleId, long actorUserId, string? actorEmpNo, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var article = await context.Km_Articles.FirstOrDefaultAsync(a => a.Id == articleId, ct)
            ?? throw new InvalidOperationException("ไม่พบบทความนี้แล้ว");
        if (article.Status != ArticleStatus.Draft)
            throw new InvalidOperationException("ส่งขออนุมัติเผยแพร่ได้เฉพาะบทความที่ยังเป็นฉบับร่าง (Draft) เท่านั้น");

        var workflow = await context.wf_workflows.FirstOrDefaultAsync(w => w.workflowcode == "KM_ARTICLE_APPROVAL", ct)
            ?? throw new InvalidOperationException("ไม่พบ workflow 'KM_ARTICLE_APPROVAL' — ติดต่อแอดมินให้ตั้งค่าก่อน");
        if (workflow.isactive != true)
            throw new InvalidOperationException($"workflow '{workflow.wname}' ปิดใช้งานอยู่ ไม่สามารถส่งอนุมัติได้");

        var subject = $"ขอเผยแพร่บทความ: {article.Title}";

        var jobId = await _engine.StartJobAsync(workflow.workflowid, "Km_Article", article.Id.ToString(),
            actorUserId, actorEmpNo, subject, amount: null, ct);

        article.JobMasterId = jobId;
        article.Status = ArticleStatus.PendingApproval;
        article.SubmittedDate = DateTime.Now;
        await context.SaveChangesAsync(ct);

        return jobId;
    }

    // Lazy apply-on-read — called from the admin page on every load for
    // articles still pending. No-op unless the article is PendingApproval
    // with a job that has since closed. COMPLETED → Published; any other
    // closing status (ตีกลับ/ไม่อนุมัติ) → back to Draft so the author can
    // rework and resubmit.
    public async Task SyncStatusFromJobAsync(long articleId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var article = await context.Km_Articles.FirstOrDefaultAsync(a => a.Id == articleId, ct);
        if (article is null || article.Status != ArticleStatus.PendingApproval || article.JobMasterId is null)
            return;

        var job = await context.job_masters.FirstOrDefaultAsync(j => j.jobmasterid == article.JobMasterId, ct);
        if (job is null || job.isJobClosed != true)
            return;

        article.Status = job.status == WorkflowEngineService.StatusCompleted
            ? ArticleStatus.Published
            : ArticleStatus.Draft;
        article.ModifiedDate = DateTime.Now;
        await context.SaveChangesAsync(ct);
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
