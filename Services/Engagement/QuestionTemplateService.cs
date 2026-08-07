namespace HRM.Services.Engagement;

using HRM.Models;
using HRM.Services.Shared;
using Microsoft.EntityFrameworkCore;

// Plain CRUD over the shared question bank a campaign snapshots from at
// launch time (see SurveyService.AddQuestionFromTemplateAsync).
public class QuestionTemplateService(IDbContextFactory<HRMContext> dbFactory)
{
    public async Task<List<Eng_QuestionTemplate>> GetAllAsync(string companyId, string? searchTerm = null, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var query = context.Eng_QuestionTemplates.Where(q => q.CompanyId == companyId);
        query = EntitySearchHelper.ApplyTextSearch(query, searchTerm, nameof(Eng_QuestionTemplate.Text));
        return await query.OrderBy(q => q.Id).ToListAsync(ct);
    }

    public async Task<Eng_QuestionTemplate> SaveAsync(Eng_QuestionTemplate entity, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        if (entity.Id == 0)
        {
            context.Eng_QuestionTemplates.Add(entity);
        }
        else
        {
            var existing = await context.Eng_QuestionTemplates.FirstOrDefaultAsync(q => q.Id == entity.Id, ct)
                ?? throw new InvalidOperationException("ไม่พบคำถามนี้แล้ว");
            existing.Text = entity.Text;
            existing.QuestionType = entity.QuestionType;
            existing.Options = entity.Options;
            existing.IsActive = entity.IsActive;
            entity = existing;
        }

        await context.SaveChangesAsync(ct);
        return entity;
    }

    public async Task ToggleActiveAsync(long id, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var existing = await context.Eng_QuestionTemplates.FirstOrDefaultAsync(q => q.Id == id, ct);
        if (existing is null) return;
        existing.IsActive = !existing.IsActive;
        await context.SaveChangesAsync(ct);
    }
}
