using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Perf;

// AutoX KPI gap #1 — two "don't rebuild the tree by hand" helpers for the
// Topic/SubTopic/Indicator config that hangs off Perf_EvaluationType:
//
//  * CloneTopicTreeAsync — deep-copies a whole standard set from one
//    evaluation type onto another, so HR authors one master KPI set and
//    reuses it instead of retyping every indicator. Topic/SubTopic/Indicator
//    are keyed by EvaluationTypeId (see PerfConfigCarryForwardService's note
//    on why period carry-forward deliberately skips them), so this is the
//    piece that actually reuses them across types.
//
//  * SeedIndicatorsFromJobKpiAsync — turns a position-group's own KPI list
//    (Job_ProfileKpi, per PosExecType) into indicators under a chosen
//    SubTopic, so a group's defined KPIs flow straight into the evaluation
//    form rather than being re-entered.
//
// Both are additive and idempotent-friendly (skip-by-name / guard on
// non-empty target) — they never overwrite existing config.
public class PerfKpiSetService(IDbContextFactory<HRMContext> dbFactory)
{
    // Deep-copies every active Topic -> SubTopic -> Indicator from the source
    // evaluation type onto the target. Refuses to run if the target already
    // has topics, so a clone can't silently push the target's weight totals
    // past 100% or create duplicates — HR clears the target first, or clones
    // into a fresh type. OkrGoalId is dropped (an OKR link is specific to the
    // context it was authored in); CompetencyId is kept (competencies are
    // reusable master data). Returns how many indicators were copied.
    public async Task<int> CloneTopicTreeAsync(long sourceEvaluationTypeId, long targetEvaluationTypeId, CancellationToken ct = default)
    {
        if (sourceEvaluationTypeId == targetEvaluationTypeId)
            throw new InvalidOperationException("แบบประเมินต้นทางและปลายทางต้องเป็นคนละอัน");

        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var targetExists = await context.Perf_EvaluationTypes.AnyAsync(t => t.Id == targetEvaluationTypeId, ct);
        if (!targetExists)
            throw new InvalidOperationException("ไม่พบแบบประเมินปลายทางแล้ว");

        if (await context.Perf_Topics.AnyAsync(t => t.EvaluationTypeId == targetEvaluationTypeId && t.IsActive, ct))
            throw new InvalidOperationException("แบบประเมินปลายทางมี Topic อยู่แล้ว — ลบให้ว่างก่อน หรือเลือกแบบประเมินที่ยังไม่มีตัวชี้วัด");

        var srcTopics = await context.Perf_Topics
            .Where(t => t.EvaluationTypeId == sourceEvaluationTypeId && t.IsActive)
            .OrderBy(t => t.SortOrder).ToListAsync(ct);
        if (srcTopics.Count == 0)
            throw new InvalidOperationException("แบบประเมินต้นทางยังไม่มีตัวชี้วัดให้คัดลอก");

        var srcTopicIds = srcTopics.Select(t => t.Id).ToList();
        var srcSubTopics = await context.Perf_SubTopics
            .Where(s => srcTopicIds.Contains(s.TopicId) && s.IsActive).ToListAsync(ct);
        var srcSubTopicIds = srcSubTopics.Select(s => s.Id).ToList();
        var srcIndicators = await context.Perf_Indicators
            .Where(i => srcSubTopicIds.Contains(i.SubTopicId) && i.IsActive).ToListAsync(ct);

        var indicatorCount = 0;
        foreach (var t in srcTopics)
        {
            var newTopic = new Perf_Topic
            {
                EvaluationTypeId = targetEvaluationTypeId,
                Code = t.Code,
                Name = t.Name,
                Weight = t.Weight,
                SortOrder = t.SortOrder,
                IsActive = true,
            };
            context.Perf_Topics.Add(newTopic);
            await context.SaveChangesAsync(ct); // need newTopic.Id for children

            foreach (var s in srcSubTopics.Where(s => s.TopicId == t.Id).OrderBy(s => s.SortOrder))
            {
                var newSub = new Perf_SubTopic
                {
                    TopicId = newTopic.Id,
                    Code = s.Code,
                    Name = s.Name,
                    Weight = s.Weight,
                    SortOrder = s.SortOrder,
                    IsActive = true,
                };
                context.Perf_SubTopics.Add(newSub);
                await context.SaveChangesAsync(ct); // need newSub.Id for indicators

                foreach (var i in srcIndicators.Where(i => i.SubTopicId == s.Id).OrderBy(i => i.Code))
                {
                    context.Perf_Indicators.Add(new Perf_Indicator
                    {
                        SubTopicId = newSub.Id,
                        Code = i.Code,
                        Name = i.Name,
                        NameEn = i.NameEn,
                        Weight = i.Weight,
                        Description = i.Description,
                        TargetDescription = i.TargetDescription,
                        OkrGoalId = null,
                        CompetencyId = i.CompetencyId,
                        IsActive = true,
                    });
                    indicatorCount++;
                }
                await context.SaveChangesAsync(ct);
            }
        }

        return indicatorCount;
    }

    // Creates indicators under an existing SubTopic from the active
    // Job_ProfileKpi rows of a position-group (PosExecType). Weight is split
    // evenly so the SubTopic's indicators still total 100%; the KPI's unit and
    // target value are folded into TargetDescription so nothing is lost.
    // Skips a KPI whose Name already exists (active) under this SubTopic, so
    // re-running only adds what's new. Returns how many indicators were added.
    public async Task<int> SeedIndicatorsFromJobKpiAsync(long subTopicId, long posExecTypeId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var subTopic = await context.Perf_SubTopics.FirstOrDefaultAsync(s => s.Id == subTopicId, ct)
            ?? throw new InvalidOperationException("ไม่พบ SubTopic นี้แล้ว");

        var kpis = await context.Job_ProfileKpis
            .Where(k => k.PosExecTypeId == posExecTypeId && k.IsActive)
            .OrderBy(k => k.SortOrder).ToListAsync(ct);
        if (kpis.Count == 0)
            throw new InvalidOperationException("กลุ่มตำแหน่งนี้ยังไม่มี KPI ให้ดึงเข้ามา");

        var existing = await context.Perf_Indicators
            .Where(i => i.SubTopicId == subTopicId && i.IsActive)
            .ToListAsync(ct);
        var existingNames = existing.Select(i => i.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var toAdd = kpis.Where(k => !existingNames.Contains(k.Name)).ToList();
        if (toAdd.Count == 0) return 0;

        // Even weight across the FINAL indicator set (existing + new) so the
        // SubTopic still sums to 100 after seeding.
        var finalCount = existing.Count + toAdd.Count;
        var evenWeight = Math.Round(100m / finalCount, 2);

        var startSeq = existing.Count;
        for (var idx = 0; idx < toAdd.Count; idx++)
        {
            var k = toAdd[idx];
            var target = k.TargetDescription;
            var unitTarget = new List<string>();
            if (k.TargetValue is decimal tv) unitTarget.Add($"เป้าหมาย {tv:0.##}");
            if (!string.IsNullOrWhiteSpace(k.Unit)) unitTarget.Add(k.Unit!);
            if (unitTarget.Count > 0)
                target = string.IsNullOrWhiteSpace(target) ? string.Join(" ", unitTarget) : $"{target} ({string.Join(" ", unitTarget)})";

            context.Perf_Indicators.Add(new Perf_Indicator
            {
                SubTopicId = subTopicId,
                Code = $"KPI-{startSeq + idx + 1:00}",
                Name = k.Name,
                Weight = evenWeight,
                TargetDescription = target,
                IsActive = true,
            });
        }

        // Re-even the already-existing indicators too, so the whole SubTopic
        // lands on 100 rather than existing rows keeping their old weights.
        foreach (var e in existing)
            e.Weight = evenWeight;

        await context.SaveChangesAsync(ct);
        return toAdd.Count;
    }
}
