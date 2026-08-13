using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Okr;

public record OkrCompanySummary(int TotalObjectives, decimal? AverageProgress, int NotConfiguredCount, Dictionary<OkrObjectiveStatus, int> StatusBreakdown);
public record OkrOrgProgressRow(long OrganizationId, string OrganizationName, decimal? AverageProgress, int ObjectiveCount);
public record OkrCategoryProgressRow(long? CategoryId, string CategoryName, decimal? AverageProgress, int ObjectiveCount);

// Dashboard-level aggregates — computed separately from Okr_Objective's own
// progress (never written back onto the entity), mirroring
// PayrollDashboardService's separation of aggregate queries from snapshot
// data.
public class OkrDashboardService(IDbContextFactory<HRMContext> dbFactory)
{
    public async Task<OkrCompanySummary> GetCompanySummaryAsync(long cycleId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var objectives = await context.Okr_Objectives
            .Include(o => o.KeyResults)
            .Where(o => o.CycleId == cycleId)
            .ToListAsync(ct);

        var progresses = objectives.Select(o => OkrGoalService.CalculateObjectiveProgress(o)).ToList();
        var withProgress = progresses.Where(p => p is not null).Select(p => p!.Value).ToList();

        return new OkrCompanySummary(
            objectives.Count,
            withProgress.Count == 0 ? null : Math.Round(withProgress.Average(), 2),
            progresses.Count(p => p is null),
            objectives.GroupBy(o => o.Status).ToDictionary(g => g.Key, g => g.Count()));
    }

    public async Task<List<OkrOrgProgressRow>> GetProgressByOrganizationAsync(long cycleId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var objectives = await context.Okr_Objectives
            .Include(o => o.KeyResults)
            .Where(o => o.CycleId == cycleId && o.OwnerType == OkrOwnerType.Organization && o.OwnerOrganizationId != null)
            .ToListAsync(ct);

        var orgIds = objectives.Select(o => o.OwnerOrganizationId!.Value).Distinct().ToList();
        var orgNames = await context.com_organizations.Where(o => orgIds.Contains(o.id)).ToDictionaryAsync(o => o.id, o => o.name ?? o.code ?? "-", ct);

        return objectives.GroupBy(o => o.OwnerOrganizationId!.Value).Select(g =>
        {
            var progresses = g.Select(o => OkrGoalService.CalculateObjectiveProgress(o)).Where(p => p is not null).Select(p => p!.Value).ToList();
            return new OkrOrgProgressRow(g.Key, orgNames.TryGetValue(g.Key, out var n) ? n : "-", progresses.Count == 0 ? null : Math.Round(progresses.Average(), 2), g.Count());
        }).OrderByDescending(r => r.ObjectiveCount).ToList();
    }

    public async Task<List<OkrCategoryProgressRow>> GetProgressByCategoryAsync(long cycleId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var objectives = await context.Okr_Objectives
            .Include(o => o.KeyResults)
            .Include(o => o.Category)
            .Where(o => o.CycleId == cycleId)
            .ToListAsync(ct);

        return objectives.GroupBy(o => o.CategoryId).Select(g =>
        {
            var progresses = g.Select(o => OkrGoalService.CalculateObjectiveProgress(o)).Where(p => p is not null).Select(p => p!.Value).ToList();
            var categoryName = g.First().Category?.Name ?? "(ไม่ระบุหมวด)";
            return new OkrCategoryProgressRow(g.Key, categoryName, progresses.Count == 0 ? null : Math.Round(progresses.Average(), 2), g.Count());
        }).OrderByDescending(r => r.ObjectiveCount).ToList();
    }
}
