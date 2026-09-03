using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Perf;

// Records one rater's scores for one evaluation instance, enforces
// extreme-grade justification per rater (Block 5), and aggregates all
// raters' scores into Perf_EvaluationInstance.FinalScorePercent/FinalGrade
// once every non-Skipped rater has submitted (Block 5).
public class PerfScoringService(IDbContextFactory<HRMContext> dbFactory)
{
    private record IndicatorWeightInfo(long IndicatorId, decimal TopicWeight, decimal SubTopicWeight, decimal IndicatorWeight);

    // Weighted percent (0-100) from a rater's raw scores. Normalizes by the
    // actual sum of normalized weights present (not a hardcoded 100) so an
    // incompletely-configured Topic/SubTopic/Indicator tree degrades to "best
    // estimate from what's there" instead of silently under-scoring.
    private static decimal ComputeWeightedPercent(List<IndicatorWeightInfo> weights, Dictionary<long, decimal> scores, int maxScorePoint)
    {
        decimal weightedSum = 0, weightTotal = 0;
        foreach (var w in weights)
        {
            if (!scores.TryGetValue(w.IndicatorId, out var point)) continue;
            var normalizedWeight = (w.TopicWeight / 100m) * (w.SubTopicWeight / 100m) * (w.IndicatorWeight / 100m);
            weightedSum += normalizedWeight * (point / maxScorePoint);
            weightTotal += normalizedWeight;
        }
        return weightTotal == 0 ? 0 : Math.Round(weightedSum / weightTotal * 100m, 2);
    }

    private static async Task<List<IndicatorWeightInfo>> GetIndicatorWeightsAsync(HRMContext context, long evaluationTypeId, CancellationToken ct)
    {
        var topics = await context.Perf_Topics.Where(t => t.EvaluationTypeId == evaluationTypeId && t.IsActive).ToListAsync(ct);
        var topicIds = topics.Select(t => t.Id).ToList();
        var subTopics = await context.Perf_SubTopics.Where(st => topicIds.Contains(st.TopicId) && st.IsActive).ToListAsync(ct);
        var subTopicIds = subTopics.Select(st => st.Id).ToList();
        var indicators = await context.Perf_Indicators.Where(i => subTopicIds.Contains(i.SubTopicId) && i.IsActive).ToListAsync(ct);

        return indicators.Select(i =>
        {
            var st = subTopics.First(x => x.Id == i.SubTopicId);
            var t = topics.First(x => x.Id == st.TopicId);
            return new IndicatorWeightInfo(i.Id, t.Weight, st.Weight, i.Weight);
        }).ToList();
    }

    // Which family a direction belongs to for grouping in aggregation —
    // Superior1/2/3 average together under one WeightSuperior slice, same
    // for Subordinate1/2/3, per Perf_RaterDirectionConfig's own doc comment.
    private static string DirectionFamily(PerfRaterDirection d) => d switch
    {
        PerfRaterDirection.Self => "Self",
        PerfRaterDirection.Superior1 or PerfRaterDirection.Superior2 or PerfRaterDirection.Superior3 => "Superior",
        PerfRaterDirection.Subordinate1 or PerfRaterDirection.Subordinate2 or PerfRaterDirection.Subordinate3 => "Subordinate",
        PerfRaterDirection.Peer => "Peer",
        _ => "Other",
    };

    public record IndicatorForScoring(
        long IndicatorId, string IndicatorCode, string IndicatorName, string? TargetDescription,
        decimal IndicatorWeight, long SubTopicId, string SubTopicName, decimal SubTopicWeight,
        long TopicId, string TopicName, decimal TopicWeight, decimal? ExistingScore, string? ExistingComment,
        string? CompetencyName = null, List<Comp_ProficiencyLevel>? CompetencyLevels = null);

    public record ScoringFormData(
        Perf_RaterAssignment RaterAssignment, Perf_EvaluationInstance Instance,
        List<IndicatorForScoring> Indicators, int MaxScorePoint,
        List<Perf_RatingScaleDescription> ScaleDescriptions,
        // GradeDirect support: the type's grading method plus the period's grade
        // bands, so a GradeDirect form can offer a grade picker instead of the
        // indicator grid. MethodType defaults to ScaleWeighted for legacy types.
        PerfEvalMethod MethodType, List<Perf_GradeBand> GradeBands);

    public async Task<ScoringFormData> GetScoringFormAsync(long raterAssignmentId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var rater = await context.Perf_RaterAssignments.Include(r => r.EvaluationInstance)
            .FirstOrDefaultAsync(r => r.Id == raterAssignmentId, ct)
            ?? throw new InvalidOperationException("ไม่พบรายการที่ต้องประเมินนี้แล้ว");

        var topics = await context.Perf_Topics
            .Where(t => t.EvaluationTypeId == rater.EvaluationInstance.EvaluationTypeId && t.IsActive)
            .ToListAsync(ct);
        var topicIds = topics.Select(t => t.Id).ToList();
        var subTopics = await context.Perf_SubTopics.Where(st => topicIds.Contains(st.TopicId) && st.IsActive).ToListAsync(ct);
        var subTopicIds = subTopics.Select(st => st.Id).ToList();
        var indicators = await context.Perf_Indicators.Where(i => subTopicIds.Contains(i.SubTopicId) && i.IsActive).ToListAsync(ct);

        var existingScores = await context.Perf_Scores
            .Where(s => s.RaterAssignmentId == raterAssignmentId)
            .ToDictionaryAsync(s => s.IndicatorId, ct);

        // BARS anchors for competency-linked indicators — one batched load,
        // then attached per indicator so the scoring form can show "what a
        // level-3 vs level-5 of THIS competency actually looks like" next to
        // the score field, instead of leaving raters to interpret the
        // indicator name alone.
        var competencyIds = indicators.Where(i => i.CompetencyId is not null).Select(i => i.CompetencyId!.Value).Distinct().ToList();
        var competencyNames = await context.Comp_Competencies.Where(c => competencyIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name, ct);
        var proficiencyLevels = await context.Comp_ProficiencyLevels
            .Where(p => competencyIds.Contains(p.CompetencyId))
            .OrderBy(p => p.Level)
            .ToListAsync(ct);

        var rows = new List<IndicatorForScoring>();
        foreach (var ind in indicators)
        {
            var st = subTopics.First(x => x.Id == ind.SubTopicId);
            var t = topics.First(x => x.Id == st.TopicId);
            existingScores.TryGetValue(ind.Id, out var existing);
            var competencyName = ind.CompetencyId is long compId ? competencyNames.GetValueOrDefault(compId) : null;
            var levels = ind.CompetencyId is long compId2 ? proficiencyLevels.Where(p => p.CompetencyId == compId2).ToList() : null;
            rows.Add(new IndicatorForScoring(ind.Id, ind.Code, ind.Name, ind.TargetDescription, ind.Weight,
                st.Id, st.Name, st.Weight, t.Id, t.Name, t.Weight, existing?.ScorePoint, existing?.Comment,
                competencyName, levels));
        }

        var scaleDescriptions = await context.Perf_RatingScaleDescriptions
            .Where(d => d.EvaluationPeriodId == rater.EvaluationInstance.EvaluationPeriodId && d.IsActive)
            .OrderBy(d => d.ScorePoint)
            .ToListAsync(ct);
        var maxScorePoint = scaleDescriptions.Count > 0 ? scaleDescriptions.Max(d => d.ScorePoint) : 5;

        var evalType = await context.Perf_EvaluationTypes
            .FirstOrDefaultAsync(t => t.Id == rater.EvaluationInstance.EvaluationTypeId, ct);
        var gradeBands = await context.Perf_GradeBands
            .Where(g => g.EvaluationPeriodId == rater.EvaluationInstance.EvaluationPeriodId && g.IsActive)
            .OrderBy(g => g.SortOrder).ToListAsync(ct);

        return new ScoringFormData(rater, rater.EvaluationInstance,
            rows.OrderBy(r => r.TopicId).ThenBy(r => r.SubTopicId).ThenBy(r => r.IndicatorCode).ToList(),
            maxScorePoint, scaleDescriptions,
            evalType?.MethodType ?? PerfEvalMethod.ScaleWeighted, gradeBands);
    }

    // GradeDirect submission: the rater chooses a grade directly. We store the
    // grade plus its band's midpoint percent (so the instance-level aggregation
    // treats this rater like any scored one) and enforce the same
    // extreme-grade-justification rule as the scored path.
    public async Task SubmitDirectGradeAsync(
        long raterAssignmentId, string grade,
        string? strengths, string? areasToImprove, PerfEthicsRating? ethicsRating,
        string? qualityJustification = null, string? quantityJustification = null, string? efficiencyJustification = null,
        CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var rater = await context.Perf_RaterAssignments.Include(r => r.EvaluationInstance)
            .FirstOrDefaultAsync(r => r.Id == raterAssignmentId, ct)
            ?? throw new InvalidOperationException("ไม่พบรายการที่ต้องประเมินนี้แล้ว");

        if (rater.Status == PerfRaterStatus.Skipped)
            throw new InvalidOperationException("รายการนี้ถูกข้ามไปแล้ว (ไม่มีผู้ประเมินจริง) ให้คะแนนไม่ได้");
        if (rater.Status == PerfRaterStatus.Submitted)
            throw new InvalidOperationException("ส่งคะแนนไปแล้ว แก้ไขซ้ำไม่ได้");
        if (string.IsNullOrWhiteSpace(grade))
            throw new InvalidOperationException("กรุณาเลือกเกรด");

        var gradeBands = await context.Perf_GradeBands
            .Where(g => g.EvaluationPeriodId == rater.EvaluationInstance.EvaluationPeriodId && g.IsActive)
            .OrderBy(g => g.SortOrder).ToListAsync(ct);
        var band = gradeBands.FirstOrDefault(g => g.Grade == grade)
            ?? throw new InvalidOperationException("เกรดที่เลือกไม่อยู่ในเกณฑ์เกรดของรอบนี้แล้ว");

        if (band.RequiresJustification &&
            (string.IsNullOrWhiteSpace(qualityJustification) || string.IsNullOrWhiteSpace(quantityJustification) || string.IsNullOrWhiteSpace(efficiencyJustification)))
        {
            throw new InvalidOperationException(
                $"เกรด {band.Grade} ต้องชี้แจงคุณภาพ/ปริมาณงาน/ประสิทธิภาพให้ครบทั้ง 3 ช่องก่อนส่ง");
        }

        rater.DirectGrade = band.Grade;
        rater.DirectScorePercent = Math.Round((band.MinPercent + band.MaxPercent) / 2m, 2);
        rater.Status = PerfRaterStatus.Submitted;
        rater.SubmittedDate = DateTime.Now;
        rater.Strengths = strengths;
        rater.AreasToImprove = areasToImprove;
        rater.EthicsRating = ethicsRating;
        rater.QualityJustification = qualityJustification;
        rater.QuantityJustification = quantityJustification;
        rater.EfficiencyJustification = efficiencyJustification;

        if (rater.EvaluationInstance.Status == PerfInstanceStatus.Draft)
            rater.EvaluationInstance.Status = PerfInstanceStatus.InProgress;

        var instanceId = rater.EvaluationInstanceId;
        await context.SaveChangesAsync(ct);

        await RecomputeInstanceScoreAsync(instanceId, ct);
    }

    public async Task SubmitScoreAsync(
        long raterAssignmentId, Dictionary<long, decimal> scoresByIndicatorId, Dictionary<long, string?>? commentsByIndicatorId,
        string? strengths, string? areasToImprove, PerfEthicsRating? ethicsRating,
        string? qualityJustification = null, string? quantityJustification = null, string? efficiencyJustification = null,
        CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var rater = await context.Perf_RaterAssignments.Include(r => r.EvaluationInstance)
            .FirstOrDefaultAsync(r => r.Id == raterAssignmentId, ct)
            ?? throw new InvalidOperationException("ไม่พบรายการที่ต้องประเมินนี้แล้ว");

        if (rater.Status == PerfRaterStatus.Skipped)
            throw new InvalidOperationException("รายการนี้ถูกข้ามไปแล้ว (ไม่มีผู้ประเมินจริง) ให้คะแนนไม่ได้");
        if (rater.Status == PerfRaterStatus.Submitted)
            throw new InvalidOperationException("ส่งคะแนนไปแล้ว แก้ไขซ้ำไม่ได้");

        var indicatorWeights = await GetIndicatorWeightsAsync(context, rater.EvaluationInstance.EvaluationTypeId, ct);
        var requiredIndicatorIds = indicatorWeights.Select(w => w.IndicatorId).ToList();

        if (requiredIndicatorIds.Count == 0)
            throw new InvalidOperationException("แบบประเมินนี้ยังไม่มีตัวชี้วัด ให้ HR ตั้งค่า Topic/SubTopic/ตัวชี้วัดก่อน");

        var missing = requiredIndicatorIds.Except(scoresByIndicatorId.Keys).ToList();
        if (missing.Count > 0)
            throw new InvalidOperationException($"กรุณาให้คะแนนให้ครบทุกตัวชี้วัด (ขาดอีก {missing.Count} ตัว)");

        var scaleDescriptions = await context.Perf_RatingScaleDescriptions
            .Where(d => d.EvaluationPeriodId == rater.EvaluationInstance.EvaluationPeriodId && d.IsActive)
            .ToListAsync(ct);
        var maxScorePoint = scaleDescriptions.Count > 0 ? scaleDescriptions.Max(d => d.ScorePoint) : 5;

        foreach (var (indicatorId, point) in scoresByIndicatorId)
        {
            if (!requiredIndicatorIds.Contains(indicatorId))
                continue; // stale/inactive indicator submitted from a cached form — ignore rather than error
            if (point < 0 || point > maxScorePoint)
                throw new InvalidOperationException($"คะแนนต้องอยู่ในช่วง 0-{maxScorePoint}");
        }

        // Extreme-grade justification (legacy EvalSumEdit.jsp behavior): map
        // THIS rater's own weighted score to the period's grade bands — if it
        // lands in a band HR flagged RequiresJustification, all three
        // justification fields are mandatory before the score can be
        // submitted at all (prevents grade inflation/deflation without a
        // paper trail, per the plan).
        var raterPercent = ComputeWeightedPercent(indicatorWeights, scoresByIndicatorId, maxScorePoint);
        var gradeBands = await context.Perf_GradeBands
            .Where(g => g.EvaluationPeriodId == rater.EvaluationInstance.EvaluationPeriodId && g.IsActive)
            .OrderBy(g => g.SortOrder).ToListAsync(ct);
        var matchedBand = gradeBands.FirstOrDefault(g => raterPercent >= g.MinPercent && raterPercent <= g.MaxPercent);
        if (matchedBand is { RequiresJustification: true } &&
            (string.IsNullOrWhiteSpace(qualityJustification) || string.IsNullOrWhiteSpace(quantityJustification) || string.IsNullOrWhiteSpace(efficiencyJustification)))
        {
            throw new InvalidOperationException(
                $"คะแนนของคุณ ({raterPercent:0.#}%) ตกอยู่ในเกรด {matchedBand.Grade} ซึ่งต้องชี้แจงคุณภาพ/ปริมาณงาน/ประสิทธิภาพให้ครบทั้ง 3 ช่องก่อนส่ง");
        }

        var existing = await context.Perf_Scores.Where(s => s.RaterAssignmentId == raterAssignmentId).ToListAsync(ct);
        var existingByIndicator = existing.ToDictionary(s => s.IndicatorId);

        foreach (var indicatorId in requiredIndicatorIds)
        {
            var point = scoresByIndicatorId[indicatorId];
            var comment = commentsByIndicatorId is not null && commentsByIndicatorId.TryGetValue(indicatorId, out var c) ? c : null;
            if (existingByIndicator.TryGetValue(indicatorId, out var row))
            {
                row.ScorePoint = point;
                row.Comment = comment;
            }
            else
            {
                context.Perf_Scores.Add(new Perf_Score { RaterAssignmentId = raterAssignmentId, IndicatorId = indicatorId, ScorePoint = point, Comment = comment });
            }
        }

        rater.Status = PerfRaterStatus.Submitted;
        rater.SubmittedDate = DateTime.Now;
        rater.Strengths = strengths;
        rater.AreasToImprove = areasToImprove;
        rater.EthicsRating = ethicsRating;
        rater.QualityJustification = qualityJustification;
        rater.QuantityJustification = quantityJustification;
        rater.EfficiencyJustification = efficiencyJustification;

        if (rater.EvaluationInstance.Status == PerfInstanceStatus.Draft)
            rater.EvaluationInstance.Status = PerfInstanceStatus.InProgress;

        var instanceId = rater.EvaluationInstanceId;
        await context.SaveChangesAsync(ct);

        await RecomputeInstanceScoreAsync(instanceId, ct);
    }

    // Aggregates every non-Skipped rater's weighted score into the
    // instance's FinalScorePercent/FinalGrade, once all of them have
    // Submitted — a no-op (returns without writing) if anyone is still
    // Pending. Each family (Self/Superior/Subordinate/Peer) present is
    // averaged internally first (matches Perf_RaterDirectionConfig's own doc
    // comment: multiple raters in one direction "ถัวเฉลี่ยกันเองภายในสัดส่วนนี้"),
    // then combined using each rater's own WeightInFinalScore (copied from
    // config at resolve time — identical for every row in the same family).
    // Families entirely absent (e.g. Superior fully Skipped, or Peer with
    // nobody else in the org) are excluded from BOTH the numerator and
    // denominator, so the result renormalizes to what data actually exists
    // instead of silently under-scoring — the same graceful-degradation
    // stance as Block 3's rater resolution.
    public async Task RecomputeInstanceScoreAsync(long instanceId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var instance = await context.Perf_EvaluationInstances.FirstOrDefaultAsync(i => i.Id == instanceId, ct)
            ?? throw new InvalidOperationException("ไม่พบรายการประเมินนี้แล้ว");

        var raters = await context.Perf_RaterAssignments
            .Where(r => r.EvaluationInstanceId == instanceId && r.Status != PerfRaterStatus.Skipped)
            .ToListAsync(ct);

        if (raters.Count == 0 || raters.Any(r => r.Status != PerfRaterStatus.Submitted))
            return; // still waiting on someone — nothing to compute yet

        var indicatorWeights = await GetIndicatorWeightsAsync(context, instance.EvaluationTypeId, ct);
        var scoresByRater = await context.Perf_Scores
            .Where(s => raters.Select(r => r.Id).Contains(s.RaterAssignmentId))
            .ToListAsync(ct);

        var scaleDescriptions = await context.Perf_RatingScaleDescriptions
            .Where(d => d.EvaluationPeriodId == instance.EvaluationPeriodId && d.IsActive).ToListAsync(ct);
        var maxScorePoint = scaleDescriptions.Count > 0 ? scaleDescriptions.Max(d => d.ScorePoint) : 5;

        decimal weightedSum = 0, weightTotal = 0;
        foreach (var family in raters.GroupBy(r => DirectionFamily(r.Direction)))
        {
            var familyPercents = family.Select(r =>
            {
                // GradeDirect raters carry their percent directly; scored raters
                // derive it from their weighted indicator scores.
                if (r.DirectScorePercent is decimal direct) return direct;
                var scores = scoresByRater.Where(s => s.RaterAssignmentId == r.Id).ToDictionary(s => s.IndicatorId, s => s.ScorePoint);
                return ComputeWeightedPercent(indicatorWeights, scores, maxScorePoint);
            }).ToList();
            var avgPercent = familyPercents.Average();
            var familyWeight = family.First().WeightInFinalScore;

            weightedSum += avgPercent * familyWeight;
            weightTotal += familyWeight;
        }

        instance.FinalScorePercent = weightTotal == 0 ? 0 : Math.Round(weightedSum / weightTotal, 2);

        var gradeBands = await context.Perf_GradeBands
            .Where(g => g.EvaluationPeriodId == instance.EvaluationPeriodId && g.IsActive)
            .OrderBy(g => g.SortOrder).ToListAsync(ct);
        var matchedBand = gradeBands.FirstOrDefault(g => instance.FinalScorePercent >= g.MinPercent && instance.FinalScorePercent <= g.MaxPercent);
        instance.FinalGrade = matchedBand?.Grade;
        instance.Status = PerfInstanceStatus.PendingApproval;

        await context.SaveChangesAsync(ct);
    }

    // AutoX #2 — RankByResult scoring. For an evaluation TYPE graded by result
    // (e.g. sales), the score isn't weighted indicators — it's the person's
    // standing within their cohort. Ranks every instance of this type+period by
    // the sum of its Perf_ResultMetric values (lower-is-better metrics negated),
    // turns the rank into a percentile-percent (best = 100), maps that through
    // the period's grade bands, and moves each instance to PendingApproval.
    // Returns how many instances were ranked. Idempotent — safe to re-run after
    // metrics change. Instances of the type that have NO result metric are left
    // untouched (nothing to rank them by).
    public async Task<int> FinalizeRankByResultAsync(long evaluationTypeId, long evaluationPeriodId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var instances = await context.Perf_EvaluationInstances
            .Where(i => i.EvaluationTypeId == evaluationTypeId && i.EvaluationPeriodId == evaluationPeriodId)
            .ToListAsync(ct);
        if (instances.Count == 0) return 0;

        var instanceIds = instances.Select(i => i.Id).ToList();
        var metrics = await context.Perf_ResultMetrics
            .Where(m => instanceIds.Contains(m.EvaluationInstanceId) && m.IsActive)
            .ToListAsync(ct);

        // One score per instance = sum of its metrics (a lower-is-better metric
        // subtracts, so higher combined score is always better).
        var scoreByInstance = metrics
            .GroupBy(m => m.EvaluationInstanceId)
            .ToDictionary(g => g.Key, g => g.Sum(m => m.HigherIsBetter ? m.MetricValue : -m.MetricValue));

        var ranked = instances
            .Where(i => scoreByInstance.ContainsKey(i.Id))
            .OrderByDescending(i => scoreByInstance[i.Id])
            .ToList();
        var n = ranked.Count;
        if (n == 0) return 0;

        var gradeBands = await context.Perf_GradeBands
            .Where(g => g.EvaluationPeriodId == evaluationPeriodId && g.IsActive)
            .OrderBy(g => g.SortOrder)
            .ToListAsync(ct);

        for (var idx = 0; idx < n; idx++)
        {
            // Percentile-percent: best (idx 0) -> 100, worst -> (1/n)*100.
            var percent = n == 1 ? 100m : Math.Round((decimal)(n - idx) / n * 100m, 2);
            var inst = ranked[idx];
            inst.FinalScorePercent = percent;
            inst.FinalGrade = gradeBands.FirstOrDefault(g => percent >= g.MinPercent && percent <= g.MaxPercent)?.Grade;
            if (inst.Status is PerfInstanceStatus.Draft or PerfInstanceStatus.InProgress)
                inst.Status = PerfInstanceStatus.PendingApproval;
        }

        await context.SaveChangesAsync(ct);
        return n;
    }

    public record MyEvaluationItem(long RaterAssignmentId, long InstanceId, string SubjectEmpName, PerfRaterDirection Direction, PerfRaterStatus Status, string PeriodName);

    public async Task<List<MyEvaluationItem>> GetMyEvaluationsAsync(long myHremployeeId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var raters = await context.Perf_RaterAssignments
            .Where(r => r.RaterHremployeeId == myHremployeeId)
            .Include(r => r.EvaluationInstance)
            .ToListAsync(ct);

        var periodIds = raters.Select(r => r.EvaluationInstance.EvaluationPeriodId).Distinct().ToList();
        var periods = await context.Perf_EvaluationPeriods.Where(p => periodIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);

        return raters.Select(r => new MyEvaluationItem(
            r.Id, r.EvaluationInstanceId, r.EvaluationInstance.SnapshotEmpName ?? $"#{r.EvaluationInstance.HremployeeId}",
            r.Direction, r.Status, periods.TryGetValue(r.EvaluationInstance.EvaluationPeriodId, out var p) ? p.Name : "-"))
            .OrderBy(i => i.Status).ThenBy(i => i.SubjectEmpName)
            .ToList();
    }
}
