namespace HRM.Services.Engagement;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Follow-up action tracking after a survey/pulse closes — same shape as
// OrgDev's ChangeInitiativeService (status + milestone checklist, no
// workflow approval; this is a tracking tool, not a document needing sign-off).
public class ActionPlanService(IDbContextFactory<HRMContext> dbFactory)
{
    public record ActionPlanRow(long Id, string Title, Eng_ActionPlanStatus Status, string? CampaignTitle,
        DateOnly? StartDate, DateOnly? TargetCompletionDate, int TotalMilestones, int CompletedMilestones);

    public async Task<List<ActionPlanRow>> GetActionPlansAsync(string companyId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var plans = await context.Eng_ActionPlans.Where(p => p.CompanyId == companyId).OrderByDescending(p => p.CreatedDate).ToListAsync(ct);
        if (plans.Count == 0) return new();

        var campaignIds = plans.Where(p => p.CampaignId is not null).Select(p => p.CampaignId!.Value).Distinct().ToList();
        var campaignTitles = campaignIds.Count == 0
            ? new Dictionary<long, string>()
            : await context.Eng_SurveyCampaigns.Where(c => campaignIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Title, ct);

        var planIds = plans.Select(p => p.Id).ToList();
        var milestones = await context.Eng_ActionPlanMilestones.Where(m => planIds.Contains(m.ActionPlanId)).ToListAsync(ct);

        return plans.Select(p =>
        {
            var forThis = milestones.Where(m => m.ActionPlanId == p.Id).ToList();
            return new ActionPlanRow(p.Id, p.Title, p.Status,
                p.CampaignId is long cid && campaignTitles.TryGetValue(cid, out var t) ? t : null,
                p.StartDate, p.TargetCompletionDate, forThis.Count, forThis.Count(m => m.Status == Eng_MilestoneStatus.Completed));
        }).ToList();
    }

    public async Task<long> CreateAsync(string companyId, long? campaignId, string title, string? description,
        long ownerUserId, long? impactedOrganizationId, DateOnly? startDate, DateOnly? targetCompletionDate,
        long actorUserId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var plan = new Eng_ActionPlan
        {
            CompanyId = companyId,
            CampaignId = campaignId,
            Title = title,
            Description = description,
            OwnerUserId = ownerUserId,
            ImpactedOrganizationId = impactedOrganizationId,
            StartDate = startDate,
            TargetCompletionDate = targetCompletionDate,
            CreatedByUserId = actorUserId,
        };
        context.Eng_ActionPlans.Add(plan);
        await context.SaveChangesAsync(ct);
        return plan.Id;
    }

    public async Task UpdateStatusAsync(long actionPlanId, Eng_ActionPlanStatus status, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var plan = await context.Eng_ActionPlans.FirstOrDefaultAsync(p => p.Id == actionPlanId, ct);
        if (plan is null) return;
        plan.Status = status;
        await context.SaveChangesAsync(ct);
    }

    public async Task<List<Eng_ActionPlanMilestone>> GetMilestonesAsync(long actionPlanId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        return await context.Eng_ActionPlanMilestones.Where(m => m.ActionPlanId == actionPlanId).OrderBy(m => m.Id).ToListAsync(ct);
    }

    public async Task AddMilestoneAsync(long actionPlanId, string title, DateOnly? targetDate, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        context.Eng_ActionPlanMilestones.Add(new Eng_ActionPlanMilestone { ActionPlanId = actionPlanId, Title = title, TargetDate = targetDate });
        await context.SaveChangesAsync(ct);
    }

    public async Task CompleteMilestoneAsync(long milestoneId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var milestone = await context.Eng_ActionPlanMilestones.FirstOrDefaultAsync(m => m.Id == milestoneId, ct);
        if (milestone is null) return;
        milestone.Status = Eng_MilestoneStatus.Completed;
        milestone.CompletedDate = DateTime.Now;
        await context.SaveChangesAsync(ct);
    }
}
