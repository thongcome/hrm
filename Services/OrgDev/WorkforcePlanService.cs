namespace HRM.Services.OrgDev;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Workforce Planning: target headcount per org unit per year vs actual,
// computed live from Pos_PositionSlot (never snapshotted, so it can't go
// stale). "Actual" = active slots with an employee assigned; "Vacant" =
// active slots with none — the same distinction PositionSlotAdmin.razor
// already uses.
//
// Targets are stored in Pos_HeadcountBudget — the same SAP-OM-style table
// the slot-creation check (Services/Pos/HeadcountBudgetService.cs) reads —
// NOT a separate OrgDev table. There used to be one (OrgDev_WorkforcePlan,
// retired 2026-08-31): two HR teams maintaining two "target headcount"
// numbers per org/year that never referenced each other was a
// reconciliation bug waiting to happen, flagged in the HRD maturity audit.
// This service only ever touches the org-level slice of that table
// (OrganizationId set, PosExecTypeId null); company-wide caps and per-job
// caps remain the HeadcountBudgetAdmin page's business.
public class WorkforcePlanService(IDbContextFactory<HRMContext> dbFactory)
{
    public record PlanRow(long PlanId, long OrganizationId, string OrganizationName, int PlanYear, int TargetHeadcount, int ActualHeadcount, int VacantSlots, int Gap, string? Note);

    public async Task<List<PlanRow>> GetPlansAsync(string companyId, int planYear, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var plans = await context.Pos_HeadcountBudgets
            .Where(b => b.CompanyId == companyId && b.FiscalYear == planYear && b.IsActive
                && b.OrganizationId != null && b.PosExecTypeId == null)
            .ToListAsync(ct);
        if (plans.Count == 0)
            return new();

        var orgIds = plans.Select(p => p.OrganizationId!.Value).Distinct().ToList();
        var orgs = await context.com_organizations.Where(o => orgIds.Contains(o.id)).ToDictionaryAsync(o => o.id, ct);

        // Subtree semantics (orgcodefull prefix), matching how
        // HeadcountBudgetService counts "used" against the same rows — the
        // retired OrgDev table counted exact-org only, which silently
        // undercounted whenever a unit had children with their own slots.
        var slots = await context.Pos_PositionSlots
            .Where(s => s.CompanyId == companyId && s.IsActive && s.OrganizationId != null)
            .ToListAsync(ct);
        var slotOrgIds = slots.Select(s => s.OrganizationId!.Value).Distinct().ToList();
        var slotOrgCodes = await context.com_organizations.Where(o => slotOrgIds.Contains(o.id))
            .ToDictionaryAsync(o => o.id, o => o.orgcodefull, ct);

        return plans.Select(p =>
        {
            var org = orgs.GetValueOrDefault(p.OrganizationId!.Value);
            var prefix = org?.orgcodefull;
            var slotsForOrg = prefix is null
                ? new List<Pos_PositionSlot>()
                : slots.Where(s => slotOrgCodes.GetValueOrDefault(s.OrganizationId!.Value)?.StartsWith(prefix) == true).ToList();
            var actual = slotsForOrg.Count(s => s.HremployeeId is not null);
            var vacant = slotsForOrg.Count(s => s.HremployeeId is null);
            return new PlanRow(p.Id, p.OrganizationId.Value, org?.name ?? $"#{p.OrganizationId}",
                p.FiscalYear, p.ApprovedCount, actual, vacant, p.ApprovedCount - actual, p.Note);
        }).OrderBy(r => r.OrganizationName).ToList();
    }

    public async Task<long> SaveAsync(string companyId, long organizationId, int planYear, int targetHeadcount, string? note, long actorUserId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var existing = await context.Pos_HeadcountBudgets.FirstOrDefaultAsync(b => b.CompanyId == companyId
            && b.OrganizationId == organizationId && b.FiscalYear == planYear && b.PosExecTypeId == null && b.IsActive, ct);
        if (existing is not null)
        {
            existing.ApprovedCount = targetHeadcount;
            existing.Note = note;
            await context.SaveChangesAsync(ct);
            return existing.Id;
        }

        var plan = new Pos_HeadcountBudget
        {
            CompanyId = companyId,
            OrganizationId = organizationId,
            FiscalYear = planYear,
            ApprovedCount = targetHeadcount,
            Note = note,
            CreatedByUserId = actorUserId,
        };
        context.Pos_HeadcountBudgets.Add(plan);
        await context.SaveChangesAsync(ct);
        return plan.Id;
    }

    public async Task RemoveAsync(long planId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var plan = await context.Pos_HeadcountBudgets.FirstOrDefaultAsync(b => b.Id == planId, ct);
        if (plan is null) return;
        // Soft delete — the retired OrgDev table hard-deleted, but budgets
        // follow the codebase-wide IsActive convention.
        plan.IsActive = false;
        await context.SaveChangesAsync(ct);
    }
}
