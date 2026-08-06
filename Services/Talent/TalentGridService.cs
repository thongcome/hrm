namespace HRM.Services.Talent;

using HRM.Models;
using HRM.Services.Shared;
using Microsoft.EntityFrameworkCore;

// 9-box grid (Performance × Potential) + talent pool. Performance axis reads
// from Perf_EvaluationInstance.FinalScorePercent (already computed by the
// Perf_* module — Status must be Approved) split into 3 zones by
// Talent_NineBoxSettings thresholds. Potential axis is a fresh manager
// judgment call (Talent_PotentialRating) — never computed from any existing
// data, mirroring IdpAssessmentService.SubmitAssessmentAsync's upsert
// pattern exactly. GetGridAsync is deliberately transparent about employees
// who don't have both data points yet (NotYetRatedForPotential /
// NoPerformanceDataThisPeriod) rather than silently dropping them.
public class TalentGridService(IDbContextFactory<HRMContext> dbFactory)
{
    public record GridEmployeeRow(
        long HremployeeId, string EmpNo, string Name, string? PositionName,
        decimal? FinalScorePercent, string? FinalGrade, PotentialLevel? Potential);

    public record NineBoxResult(
        Dictionary<(int PerformanceTier, int PotentialTier), List<GridEmployeeRow>> Cells,
        List<GridEmployeeRow> NotYetRatedForPotential,
        List<GridEmployeeRow> NoPerformanceDataThisPeriod);

    public async Task<Talent_NineBoxSettings> GetNineBoxSettingsAsync(string companyId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var existing = await context.Talent_NineBoxSettingsList.FirstOrDefaultAsync(s => s.CompanyId == companyId, ct);
        return existing ?? new Talent_NineBoxSettings { CompanyId = companyId };
    }

    public async Task SaveNineBoxSettingsAsync(string companyId, decimal lowMax, decimal highMin, CancellationToken ct = default)
    {
        if (lowMax < 0 || highMin > 100 || lowMax >= highMin)
            throw new ArgumentException("ต้องมี 0 <= เกณฑ์ต่ำ < เกณฑ์สูง <= 100");

        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var existing = await context.Talent_NineBoxSettingsList.FirstOrDefaultAsync(s => s.CompanyId == companyId, ct);

        if (existing is not null)
        {
            existing.PerformanceLowMaxPercent = lowMax;
            existing.PerformanceHighMinPercent = highMin;
        }
        else
        {
            context.Talent_NineBoxSettingsList.Add(new Talent_NineBoxSettings
            {
                CompanyId = companyId,
                PerformanceLowMaxPercent = lowMax,
                PerformanceHighMinPercent = highMin,
            });
        }

        await context.SaveChangesAsync(ct);
    }

    private static int ResolvePerformanceTier(decimal scorePercent, Talent_NineBoxSettings settings) =>
        scorePercent < settings.PerformanceLowMaxPercent ? 1
        : scorePercent >= settings.PerformanceHighMinPercent ? 3
        : 2;

    public async Task<NineBoxResult> GetGridAsync(string companyId, long evaluationPeriodId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var settings = await GetNineBoxSettingsAsync(companyId, ct);

        var instances = await context.Perf_EvaluationInstances
            .Where(i => i.EvaluationPeriodId == evaluationPeriodId && i.Status == PerfInstanceStatus.Approved)
            .ToListAsync(ct);

        var companyEmployeeIds = await context.Hremployee
            .Where(e => e.companyid == companyId && e.ResignDate == null)
            .Select(e => e.id)
            .ToListAsync(ct);

        instances = instances.Where(i => companyEmployeeIds.Contains(i.HremployeeId)).ToList();

        var instanceHremployeeIds = instances.Select(i => i.HremployeeId).ToList();

        var ratings = await context.Talent_PotentialRatings
            .Where(r => r.EvaluationPeriodId == evaluationPeriodId
                && (instanceHremployeeIds.Contains(r.HremployeeId) || companyEmployeeIds.Contains(r.HremployeeId)))
            .ToListAsync(ct);

        var employeeIdsNeeded = instanceHremployeeIds.Union(ratings.Select(r => r.HremployeeId)).Distinct().ToList();
        var employees = await context.Hremployee.Where(e => employeeIdsNeeded.Contains(e.id)).ToListAsync(ct);
        var positionNames = await context.Pos_PositionSlots
            .Where(s => s.HremployeeId != null && employeeIdsNeeded.Contains(s.HremployeeId!.Value) && s.IsActive)
            .ToDictionaryAsync(s => s.HremployeeId!.Value, s => s.Name, ct);

        GridEmployeeRow BuildRow(long hremployeeId, decimal? score, string? grade, PotentialLevel? potential)
        {
            var emp = employees.First(e => e.id == hremployeeId);
            positionNames.TryGetValue(hremployeeId, out var positionName);
            return new GridEmployeeRow(hremployeeId, emp.EmpNo, $"{emp.EmpName} {emp.EmpSurname}", positionName, score, grade, potential);
        }

        var cells = new Dictionary<(int, int), List<GridEmployeeRow>>();
        for (var p = 1; p <= 3; p++)
            for (var q = 1; q <= 3; q++)
                cells[(p, q)] = new List<GridEmployeeRow>();

        var notYetRatedForPotential = new List<GridEmployeeRow>();

        foreach (var instance in instances)
        {
            var rating = ratings.FirstOrDefault(r => r.HremployeeId == instance.HremployeeId);
            var row = BuildRow(instance.HremployeeId, instance.FinalScorePercent, instance.FinalGrade, rating?.Level);

            if (instance.FinalScorePercent is not decimal score)
                continue; // Approved should always carry a score, but guard defensively rather than crash

            if (rating is null)
            {
                notYetRatedForPotential.Add(row);
                continue;
            }

            var perfTier = ResolvePerformanceTier(score, settings);
            var potentialTier = (int)rating.Level;
            cells[(perfTier, potentialTier)].Add(row);
        }

        var noPerformanceDataThisPeriod = ratings
            .Where(r => !instanceHremployeeIds.Contains(r.HremployeeId))
            .Select(r => BuildRow(r.HremployeeId, null, null, r.Level))
            .ToList();

        return new NineBoxResult(cells, notYetRatedForPotential, noPerformanceDataThisPeriod);
    }

    public async Task SubmitPotentialRatingAsync(long hremployeeId, long evaluationPeriodId, PotentialLevel level, long ratedByUserId, string? note, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var existing = await context.Talent_PotentialRatings.FirstOrDefaultAsync(
            r => r.HremployeeId == hremployeeId && r.EvaluationPeriodId == evaluationPeriodId, ct);

        if (existing is not null)
        {
            existing.Level = level;
            existing.RatedByUserId = ratedByUserId;
            existing.RatedDate = DateTime.Now;
            existing.Note = note;
        }
        else
        {
            context.Talent_PotentialRatings.Add(new Talent_PotentialRating
            {
                HremployeeId = hremployeeId,
                EvaluationPeriodId = evaluationPeriodId,
                Level = level,
                RatedByUserId = ratedByUserId,
                Note = note,
            });
        }

        await context.SaveChangesAsync(ct);
    }

    public async Task<List<GridEmployeeRow>> GetDirectReportsWithScoresAsync(long managerHremployeeId, long evaluationPeriodId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var manager = await context.Hremployee.FirstOrDefaultAsync(e => e.id == managerHremployeeId, ct);
        if (manager is null)
            return new();

        var reports = await DirectReportResolverHelper.ResolveDirectReportsAsync(context, manager, ct);
        if (reports.Count == 0)
            return new();

        var reportIds = reports.Select(r => r.id).ToList();

        var instances = await context.Perf_EvaluationInstances
            .Where(i => i.EvaluationPeriodId == evaluationPeriodId && i.Status == PerfInstanceStatus.Approved && reportIds.Contains(i.HremployeeId))
            .ToListAsync(ct);

        var ratings = await context.Talent_PotentialRatings
            .Where(r => r.EvaluationPeriodId == evaluationPeriodId && reportIds.Contains(r.HremployeeId))
            .ToListAsync(ct);

        var positionNames = await context.Pos_PositionSlots
            .Where(s => s.HremployeeId != null && reportIds.Contains(s.HremployeeId!.Value) && s.IsActive)
            .ToDictionaryAsync(s => s.HremployeeId!.Value, s => s.Name, ct);

        return reports.Select(r =>
        {
            var instance = instances.FirstOrDefault(i => i.HremployeeId == r.id);
            var rating = ratings.FirstOrDefault(rt => rt.HremployeeId == r.id);
            positionNames.TryGetValue(r.id, out var positionName);
            return new GridEmployeeRow(r.id, r.EmpNo, $"{r.EmpName} {r.EmpSurname}", positionName, instance?.FinalScorePercent, instance?.FinalGrade, rating?.Level);
        }).ToList();
    }

    public async Task<List<Talent_PoolEntry>> GetPoolEntriesAsync(string companyId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var companyEmployeeIds = await context.Hremployee
            .Where(e => e.companyid == companyId)
            .Select(e => e.id)
            .ToListAsync(ct);

        return await context.Talent_PoolEntries
            .Where(p => p.IsActive && companyEmployeeIds.Contains(p.HremployeeId))
            .OrderByDescending(p => p.AddedDate)
            .ToListAsync(ct);
    }

    public async Task AddToPoolAsync(long hremployeeId, long actorUserId, string? reason, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var alreadyActive = await context.Talent_PoolEntries.AnyAsync(p => p.HremployeeId == hremployeeId && p.IsActive, ct);
        if (alreadyActive)
            return; // idempotent — don't create a duplicate active entry

        context.Talent_PoolEntries.Add(new Talent_PoolEntry
        {
            HremployeeId = hremployeeId,
            AddedByUserId = actorUserId,
            Reason = reason,
        });
        await context.SaveChangesAsync(ct);
    }

    public async Task RemoveFromPoolAsync(long entryId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var entry = await context.Talent_PoolEntries.FirstOrDefaultAsync(p => p.Id == entryId, ct);
        if (entry is null)
            return;

        entry.IsActive = false;
        await context.SaveChangesAsync(ct);
    }
}
