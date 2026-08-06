namespace HRM.Services.Idp;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Lightweight self/manager competency-proficiency assessment + gap analysis
// against the employee's current Job Profile (Pos_ExecType via the active
// Pos_PositionSlot). Deliberately NOT a multi-rater engine like Perf_* — see
// the plan's "จงใจไม่ทำในรอบนี้" section. GetDirectReportsAsync delegates to
// Services/Shared/DirectReportResolverHelper.cs (extracted from here once
// Talent Management needed the identical level-1 direct-report lookup).
public class IdpAssessmentService(IDbContextFactory<HRMContext> dbFactory)
{
    public record CompetencyGapRow(
        long CompetencyId, string CompetencyName, int RequiredLevel, bool IsCritical,
        int? SelfLevel, int? ManagerLevel, int? EffectiveLevel, int? Gap);

    public async Task<List<Job_CompetencyRequirement>> GetRequiredCompetenciesAsync(long hremployeeId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        return await GetRequiredCompetenciesAsync(context, hremployeeId, ct);
    }

    private static async Task<List<Job_CompetencyRequirement>> GetRequiredCompetenciesAsync(HRMContext context, long hremployeeId, CancellationToken ct)
    {
        var slot = await context.Pos_PositionSlots.FirstOrDefaultAsync(s => s.HremployeeId == hremployeeId && s.IsActive, ct);
        if (slot?.PosExecTypeId is not long posExecTypeId)
            return new();

        return await context.Job_CompetencyRequirements
            .Include(r => r.Competency)
            .Where(r => r.PosExecTypeId == posExecTypeId)
            .OrderBy(r => r.SortOrder)
            .ToListAsync(ct);
    }

    public async Task SubmitAssessmentAsync(long hremployeeId, long competencyId, IdpAssessmentSource source, int ratedLevel, long ratedByUserId, string? note, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var existing = await context.Idp_CompetencyAssessments.FirstOrDefaultAsync(
            a => a.HremployeeId == hremployeeId && a.CompetencyId == competencyId && a.Source == source, ct);

        if (existing is not null)
        {
            existing.RatedLevel = ratedLevel;
            existing.RatedByUserId = ratedByUserId;
            existing.RatedDate = DateTime.Now;
            existing.Note = note;
        }
        else
        {
            context.Idp_CompetencyAssessments.Add(new Idp_CompetencyAssessment
            {
                HremployeeId = hremployeeId,
                CompetencyId = competencyId,
                Source = source,
                RatedLevel = ratedLevel,
                RatedByUserId = ratedByUserId,
                Note = note,
            });
        }

        await context.SaveChangesAsync(ct);
    }

    public async Task<List<Idp_CompetencyAssessment>> GetAssessmentsAsync(long hremployeeId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        return await context.Idp_CompetencyAssessments.Where(a => a.HremployeeId == hremployeeId).ToListAsync(ct);
    }

    public async Task<List<CompetencyGapRow>> GetGapAnalysisAsync(long hremployeeId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var requirements = await GetRequiredCompetenciesAsync(context, hremployeeId, ct);
        if (requirements.Count == 0)
            return new();

        var assessments = await context.Idp_CompetencyAssessments.Where(a => a.HremployeeId == hremployeeId).ToListAsync(ct);

        var rows = new List<CompetencyGapRow>();
        foreach (var req in requirements)
        {
            var selfLevel = assessments.FirstOrDefault(a => a.CompetencyId == req.CompetencyId && a.Source == IdpAssessmentSource.Self)?.RatedLevel;
            var managerLevel = assessments.FirstOrDefault(a => a.CompetencyId == req.CompetencyId && a.Source == IdpAssessmentSource.Manager)?.RatedLevel;
            var effectiveLevel = managerLevel ?? selfLevel;
            var gap = effectiveLevel is int eff ? req.RequiredLevel - eff : (int?)null;

            rows.Add(new CompetencyGapRow(req.CompetencyId, req.Competency.Name, req.RequiredLevel, req.IsCritical, selfLevel, managerLevel, effectiveLevel, gap));
        }

        return rows;
    }

    public async Task<List<Hremployee>> GetDirectReportsAsync(long managerHremployeeId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var manager = await context.Hremployee.FirstOrDefaultAsync(e => e.id == managerHremployeeId, ct);
        if (manager is null)
            return new();

        return await HRM.Services.Shared.DirectReportResolverHelper.ResolveDirectReportsAsync(context, manager, ct);
    }
}
