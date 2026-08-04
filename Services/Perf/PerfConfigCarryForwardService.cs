using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Perf;

// Copies the period-specific parts of a rating config to a new period, so HR
// doesn't have to re-enter grade bands/scale descriptions every year.
// Topic/SubTopic/Indicator are deliberately NOT copied here — they hang off
// Perf_EvaluationType (EvaluationTypeId), not Perf_EvaluationPeriod, so the
// same Topic/SubTopic/Indicator tree already applies to every period that
// uses that evaluation type without any copying. Only Perf_GradeBand and
// Perf_RatingScaleDescription are actually scoped per-period (see their own
// doc comments: grade bands and scale wording can change year to year).
public class PerfConfigCarryForwardService(IDbContextFactory<HRMContext> dbFactory)
{
    public async Task CopyPeriodConfigAsync(long sourcePeriodId, long newPeriodId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var newPeriod = await context.Perf_EvaluationPeriods.FirstOrDefaultAsync(p => p.Id == newPeriodId, ct)
            ?? throw new InvalidOperationException("ไม่พบรอบการประเมินปลายทาง");

        var alreadyHasConfig = await context.Perf_GradeBands.AnyAsync(g => g.EvaluationPeriodId == newPeriodId, ct)
            || await context.Perf_RatingScaleDescriptions.AnyAsync(d => d.EvaluationPeriodId == newPeriodId, ct);
        if (alreadyHasConfig)
            throw new InvalidOperationException("รอบปลายทางมีเกณฑ์เกรด/มาตรวัดคะแนนอยู่แล้ว — ก็อปปี้ทับไม่ได้ (ลบของเดิมก่อนถ้าต้องการก็อปปี้ใหม่)");

        var sourceGradeBands = await context.Perf_GradeBands
            .Where(g => g.EvaluationPeriodId == sourcePeriodId).OrderBy(g => g.SortOrder).ToListAsync(ct);
        var sourceScaleDescriptions = await context.Perf_RatingScaleDescriptions
            .Where(d => d.EvaluationPeriodId == sourcePeriodId).OrderBy(d => d.ScorePoint).ToListAsync(ct);

        if (sourceGradeBands.Count == 0 && sourceScaleDescriptions.Count == 0)
            throw new InvalidOperationException("รอบต้นทางยังไม่มีเกณฑ์เกรดหรือมาตรวัดคะแนนให้ก็อปปี้");

        foreach (var band in sourceGradeBands)
        {
            context.Perf_GradeBands.Add(new Perf_GradeBand
            {
                EvaluationPeriodId = newPeriodId,
                Grade = band.Grade,
                MinPercent = band.MinPercent,
                MaxPercent = band.MaxPercent,
                SalaryIncreasePercent = band.SalaryIncreasePercent,
                BonusPercent = band.BonusPercent,
                RequiresJustification = band.RequiresJustification,
                SortOrder = band.SortOrder,
            });
        }

        foreach (var desc in sourceScaleDescriptions)
        {
            context.Perf_RatingScaleDescriptions.Add(new Perf_RatingScaleDescription
            {
                EvaluationPeriodId = newPeriodId,
                ScorePoint = desc.ScorePoint,
                Description = desc.Description,
            });
        }

        await context.SaveChangesAsync(ct);
    }
}
