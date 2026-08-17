namespace HRM.Services.Pos;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Counts live Pos_PositionSlot rows against Pos_HeadcountBudget scopes.
// "Used" is always a fresh count of currently active, manpower-counting
// slots matching a budget row's (Organization subtree, PosExecType) scope —
// there is no date-scoped "as of" query here (see Pos_HeadcountBudget's
// comment on why FiscalYear is a label, not a filter).
public class HeadcountBudgetService
{
    private readonly IDbContextFactory<HRMContext> _dbFactory;

    public HeadcountBudgetService(IDbContextFactory<HRMContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public record BudgetUtilization(Pos_HeadcountBudget Budget, string ScopeLabel, int UsedCount, int RemainingCount, bool IsOverBudget);

    public async Task<List<BudgetUtilization>> GetUtilizationAsync(string companyId, int fiscalYear, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var budgets = await context.Pos_HeadcountBudgets
            .Where(b => b.CompanyId == companyId && b.FiscalYear == fiscalYear && b.IsActive)
            .ToListAsync(ct);
        if (budgets.Count == 0) return new List<BudgetUtilization>();

        var orgs = await context.com_organizations.Where(o => o.orgcodefull != null).ToListAsync(ct);
        var orgById = orgs.ToDictionary(o => o.id);
        var execTypes = await context.Pos_ExecTypes.Where(t => t.CompanyId == companyId).ToDictionaryAsync(t => t.Id, ct);
        var slots = await context.Pos_PositionSlots
            .Where(s => s.CompanyId == companyId && s.IsActive && s.IsManpower)
            .ToListAsync(ct);

        var results = new List<BudgetUtilization>();
        foreach (var budget in budgets)
        {
            string? orgCodeFullPrefix = null;
            var scopeParts = new List<string>();
            if (budget.OrganizationId is long orgId)
            {
                orgCodeFullPrefix = orgById.TryGetValue(orgId, out var org) ? org.orgcodefull : null;
                scopeParts.Add(orgById.TryGetValue(orgId, out var orgLabel) ? $"{orgLabel.code} — {orgLabel.name}" : $"หน่วยงาน #{orgId} (ไม่พบ)");
            }
            if (budget.PosExecTypeId is long jobId)
                scopeParts.Add(execTypes.TryGetValue(jobId, out var job) ? $"ตำแหน่ง {job.Name}" : $"ตำแหน่ง #{jobId} (ไม่พบ)");
            if (scopeParts.Count == 0) scopeParts.Add("ทั้งบริษัท");

            var used = slots.Count(s =>
                (budget.PosExecTypeId is null || s.PosExecTypeId == budget.PosExecTypeId) &&
                (orgCodeFullPrefix is null || (s.OrganizationId is long sOrgId && orgById.TryGetValue(sOrgId, out var sOrg) && sOrg.orgcodefull is not null && sOrg.orgcodefull.StartsWith(orgCodeFullPrefix))));

            results.Add(new BudgetUtilization(budget, string.Join(" / ", scopeParts), used, budget.ApprovedCount - used, used > budget.ApprovedCount));
        }
        return results.OrderBy(r => r.Budget.OrganizationId is null).ThenBy(r => r.ScopeLabel).ToList();
    }

    // Finds the most specific active budget row matching a candidate slot's
    // scope (prefers Org+Job over Org-only over Job-only over company-wide),
    // and reports whether adding one more manpower slot there would exceed
    // it. Returns null if no budget row covers this scope at all — silence
    // in that case, not a warning, since "no budget configured" isn't the
    // same as "over budget".
    public async Task<BudgetUtilization?> CheckBeforeAddAsync(string companyId, int fiscalYear, long? organizationId, long? posExecTypeId, CancellationToken ct = default)
    {
        var all = await GetUtilizationAsync(companyId, fiscalYear, ct);
        if (all.Count == 0) return null;

        string? candidateOrgCodeFull = null;
        if (organizationId is long orgId)
        {
            await using var context = await _dbFactory.CreateDbContextAsync(ct);
            candidateOrgCodeFull = (await context.com_organizations.FirstOrDefaultAsync(o => o.id == orgId, ct))?.orgcodefull;
        }

        // Does a budget row's scope cover the candidate slot? Org scope
        // covers if the candidate org's orgcodefull starts with the budget
        // org's orgcodefull (candidate is that org or a descendant of it).
        // Job scope covers if equal or unset (wildcard).
        await using var ctx = await _dbFactory.CreateDbContextAsync(ct);
        var budgetOrgIds = all.Where(u => u.Budget.OrganizationId is not null).Select(u => u.Budget.OrganizationId!.Value).Distinct().ToList();
        var budgetOrgs = await ctx.com_organizations.Where(o => budgetOrgIds.Contains(o.id)).ToDictionaryAsync(o => o.id, ct);

        bool Covers(BudgetUtilization u)
        {
            var b = u.Budget;
            if (b.PosExecTypeId is long bJob && bJob != posExecTypeId) return false;
            if (b.OrganizationId is null) return true;
            if (candidateOrgCodeFull is null) return false;
            if (!budgetOrgs.TryGetValue(b.OrganizationId.Value, out var bOrg) || bOrg.orgcodefull is null) return false;
            return candidateOrgCodeFull.StartsWith(bOrg.orgcodefull);
        }

        var matches = all.Where(Covers).ToList();
        if (matches.Count == 0) return null;

        // Most specific = has both Org and Job set, then Org-only/Job-only, then company-wide.
        return matches
            .OrderByDescending(u => (u.Budget.OrganizationId is not null ? 2 : 0) + (u.Budget.PosExecTypeId is not null ? 1 : 0))
            .First();
    }
}
