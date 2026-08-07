namespace HRM.Services.OrgDev;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Change Management: tracked initiatives (restructuring, system rollout,
// etc.) with a milestone checklist — same shape as LeadershipDevelopmentService,
// kept as a separate service because the two track unrelated parent entities.
public class ChangeInitiativeService(IDbContextFactory<HRMContext> dbFactory)
{
    public record InitiativeRow(long Id, string Title, ChangeInitiativeStatus Status, string? ImpactedOrganizationName, DateOnly? StartDate, DateOnly? TargetCompletionDate, int TotalMilestones, int CompletedMilestones);

    public async Task<List<InitiativeRow>> GetInitiativesAsync(string companyId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var initiatives = await context.OrgDev_ChangeInitiatives.Where(i => i.CompanyId == companyId).OrderByDescending(i => i.CreatedDate).ToListAsync(ct);
        if (initiatives.Count == 0)
            return new();

        var orgIds = initiatives.Where(i => i.ImpactedOrganizationId is not null).Select(i => i.ImpactedOrganizationId!.Value).Distinct().ToList();
        var orgNames = await context.com_organizations.Where(o => orgIds.Contains(o.id)).ToDictionaryAsync(o => o.id, o => o.name ?? $"#{o.id}", ct);

        var initiativeIds = initiatives.Select(i => i.Id).ToList();
        var milestones = await context.OrgDev_ChangeMilestones.Where(m => initiativeIds.Contains(m.InitiativeId)).ToListAsync(ct);

        return initiatives.Select(i =>
        {
            var forThis = milestones.Where(m => m.InitiativeId == i.Id).ToList();
            return new InitiativeRow(i.Id, i.Title, i.Status,
                i.ImpactedOrganizationId is long oid && orgNames.TryGetValue(oid, out var n) ? n : null,
                i.StartDate, i.TargetCompletionDate, forThis.Count, forThis.Count(m => m.Status == OrgDevMilestoneStatus.Completed));
        }).ToList();
    }

    public async Task<long> CreateAsync(string companyId, string title, string? description, long sponsorUserId, long? impactedOrganizationId, DateOnly? startDate, DateOnly? targetCompletionDate, long actorUserId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var initiative = new OrgDev_ChangeInitiative
        {
            CompanyId = companyId,
            Title = title,
            Description = description,
            SponsorUserId = sponsorUserId,
            ImpactedOrganizationId = impactedOrganizationId,
            StartDate = startDate,
            TargetCompletionDate = targetCompletionDate,
            CreatedByUserId = actorUserId,
        };
        context.OrgDev_ChangeInitiatives.Add(initiative);
        await context.SaveChangesAsync(ct);
        return initiative.Id;
    }

    public async Task UpdateStatusAsync(long initiativeId, ChangeInitiativeStatus status, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var initiative = await context.OrgDev_ChangeInitiatives.FirstOrDefaultAsync(i => i.Id == initiativeId, ct);
        if (initiative is null) return;
        initiative.Status = status;
        await context.SaveChangesAsync(ct);
    }

    public async Task<List<OrgDev_ChangeMilestone>> GetMilestonesAsync(long initiativeId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        return await context.OrgDev_ChangeMilestones.Where(m => m.InitiativeId == initiativeId).OrderBy(m => m.Id).ToListAsync(ct);
    }

    public async Task AddMilestoneAsync(long initiativeId, string title, DateOnly? targetDate, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        context.OrgDev_ChangeMilestones.Add(new OrgDev_ChangeMilestone { InitiativeId = initiativeId, Title = title, TargetDate = targetDate });
        await context.SaveChangesAsync(ct);
    }

    public async Task CompleteMilestoneAsync(long milestoneId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var milestone = await context.OrgDev_ChangeMilestones.FirstOrDefaultAsync(m => m.Id == milestoneId, ct);
        if (milestone is null) return;
        milestone.Status = OrgDevMilestoneStatus.Completed;
        milestone.CompletedDate = DateTime.Now;
        await context.SaveChangesAsync(ct);
    }
}
