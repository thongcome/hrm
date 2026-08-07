namespace HRM.Services.OrgDev;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Workforce Planning: target headcount per org unit per year vs actual,
// computed live from Pos_PositionSlot (never snapshotted, so it can't go
// stale). "Actual" = active slots with an employee assigned; "Vacant" =
// active slots with none — the same distinction PositionSlotAdmin.razor
// already uses.
public class WorkforcePlanService(IDbContextFactory<HRMContext> dbFactory)
{
    public record PlanRow(long PlanId, long OrganizationId, string OrganizationName, int PlanYear, int TargetHeadcount, int ActualHeadcount, int VacantSlots, int Gap, string? Note);

    public async Task<List<PlanRow>> GetPlansAsync(string companyId, int planYear, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var plans = await context.OrgDev_WorkforcePlans.Where(p => p.CompanyId == companyId && p.PlanYear == planYear).ToListAsync(ct);
        if (plans.Count == 0)
            return new();

        var orgIds = plans.Select(p => p.OrganizationId).Distinct().ToList();
        var orgNames = await context.com_organizations.Where(o => orgIds.Contains(o.id)).ToDictionaryAsync(o => o.id, o => o.name ?? $"#{o.id}", ct);

        var slots = await context.Pos_PositionSlots
            .Where(s => s.CompanyId == companyId && s.IsActive && orgIds.Contains(s.OrganizationId ?? 0))
            .ToListAsync(ct);

        return plans.Select(p =>
        {
            var slotsForOrg = slots.Where(s => s.OrganizationId == p.OrganizationId).ToList();
            var actual = slotsForOrg.Count(s => s.HremployeeId is not null);
            var vacant = slotsForOrg.Count(s => s.HremployeeId is null);
            return new PlanRow(p.Id, p.OrganizationId, orgNames.TryGetValue(p.OrganizationId, out var n) ? n : $"#{p.OrganizationId}",
                p.PlanYear, p.TargetHeadcount, actual, vacant, p.TargetHeadcount - actual, p.Note);
        }).OrderBy(r => r.OrganizationName).ToList();
    }

    public async Task<long> SaveAsync(string companyId, long organizationId, int planYear, int targetHeadcount, string? note, long actorUserId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var existing = await context.OrgDev_WorkforcePlans.FirstOrDefaultAsync(p => p.CompanyId == companyId && p.OrganizationId == organizationId && p.PlanYear == planYear, ct);
        if (existing is not null)
        {
            existing.TargetHeadcount = targetHeadcount;
            existing.Note = note;
            await context.SaveChangesAsync(ct);
            return existing.Id;
        }

        var plan = new OrgDev_WorkforcePlan
        {
            CompanyId = companyId,
            OrganizationId = organizationId,
            PlanYear = planYear,
            TargetHeadcount = targetHeadcount,
            Note = note,
            CreatedByUserId = actorUserId,
        };
        context.OrgDev_WorkforcePlans.Add(plan);
        await context.SaveChangesAsync(ct);
        return plan.Id;
    }

    public async Task RemoveAsync(long planId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var plan = await context.OrgDev_WorkforcePlans.FirstOrDefaultAsync(p => p.Id == planId, ct);
        if (plan is null) return;
        context.OrgDev_WorkforcePlans.Remove(plan);
        await context.SaveChangesAsync(ct);
    }
}
