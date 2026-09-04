using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Engagement;

// The intelligence layer behind the Engagement cockpit: it turns the raw
// survey / recognition / action-plan tables into the headline numbers an HR or
// exec actually wants — eNPS, an engagement index, participation, a recognition
// leaderboard, and trend — rather than the plain row counts the old dashboard
// showed. Pure read/aggregation; no writes.
public class EngagementAnalyticsService(IDbContextFactory<HRMContext> dbFactory)
{
    public record LeaderRow(string Name, int Points, int Count);
    public record TrendPoint(string Label, double Enps, int Responses);

    public record EngagementOverview(
        // eNPS (latest ENPS campaign)
        double? Enps, int Promoters, int Passives, int Detractors, int NpsResponses, string? NpsCampaignTitle,
        // engagement index = avg rating (of latest Survey/Pulse) scaled to 0-100
        double? EngagementIndexPercent, double? EngagementIndexRaw, string? IndexCampaignTitle,
        // participation across all non-draft campaigns
        int TotalInvited, int TotalResponses, double ParticipationRatePercent,
        // recognition this month
        int KudosThisMonth, int PointsThisMonth, int DistinctGivers, int DistinctReceivers,
        double RecognitionParticipationPercent, string? TopCoreValue, int TopCoreValueCount,
        // recognition leaderboard (this month)
        List<LeaderRow> TopReceivers, List<LeaderRow> TopGivers,
        // action plans
        int ActionTotal, int ActionPlanned, int ActionInProgress, int ActionCompleted,
        // eNPS trend (recent ENPS campaigns, oldest→newest)
        List<TrendPoint> EnpsTrend,
        int ActiveHeadcount);

    public async Task<EngagementOverview> GetOverviewAsync(string companyId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

        var campaigns = await context.Eng_SurveyCampaigns
            .Where(c => c.CompanyId == companyId && c.Status != Eng_CampaignStatus.Draft)
            .Select(c => new { c.Id, c.Title, c.CampaignType, c.Status, c.InvitedCount, c.ResponseCount, c.CreatedDate })
            .ToListAsync(ct);

        var activeHeadcount = await context.Hremployee.CountAsync(e => e.companyid == companyId && e.ResignDate == null, ct);

        // ---- participation across all non-draft campaigns ----
        var totalInvited = campaigns.Sum(c => c.InvitedCount);
        var totalResponses = campaigns.Sum(c => c.ResponseCount);
        var participation = totalInvited == 0 ? 0 : Math.Round(totalResponses * 100.0 / totalInvited, 1);

        // ---- eNPS: latest ENPS campaign ----
        double? enps = null; int promoters = 0, passives = 0, detractors = 0, npsResponses = 0; string? npsTitle = null;
        var latestNps = campaigns.Where(c => c.CampaignType == Eng_CampaignType.ENPS).OrderByDescending(c => c.CreatedDate).FirstOrDefault();
        if (latestNps is not null)
        {
            npsTitle = latestNps.Title;
            var scores = await context.Eng_SurveyResponses
                .Where(r => r.CampaignId == latestNps.Id && r.NpsScore != null)
                .Select(r => r.NpsScore!.Value).ToListAsync(ct);
            (enps, promoters, passives, detractors, npsResponses) = ComputeEnps(scores);
        }

        // ---- engagement index: avg rating of the latest Survey/Pulse campaign ----
        double? indexRaw = null, indexPct = null; string? indexTitle = null;
        var latestIndexCampaign = campaigns
            .Where(c => c.CampaignType is Eng_CampaignType.Survey or Eng_CampaignType.Pulse or Eng_CampaignType.Culture)
            .OrderByDescending(c => c.CreatedDate).FirstOrDefault();
        if (latestIndexCampaign is not null)
        {
            indexTitle = latestIndexCampaign.Title;
            var ratings = await (
                from a in context.Eng_SurveyAnswers
                join r in context.Eng_SurveyResponses on a.ResponseId equals r.Id
                where r.CampaignId == latestIndexCampaign.Id && a.RatingValue != null
                select a.RatingValue!.Value).ToListAsync(ct);
            if (ratings.Count > 0)
            {
                indexRaw = Math.Round(ratings.Average(), 2);
                indexPct = Math.Round(ratings.Average() / 5.0 * 100, 1); // assumes a 1–5 scale
            }
        }

        // ---- recognition this month ----
        var recos = await context.Eng_Recognitions
            .Where(k => k.CompanyId == companyId && k.IsActive && k.CreatedDate >= monthStart)
            .Select(k => new { k.FromHremployeeId, k.ToHremployeeId, k.Points, k.CoreValueTag })
            .ToListAsync(ct);

        var kudosThisMonth = recos.Count;
        var pointsThisMonth = recos.Sum(r => r.Points);
        var givers = recos.Select(r => r.FromHremployeeId).Distinct().ToList();
        var receivers = recos.Select(r => r.ToHremployeeId).Distinct().ToList();
        var participants = givers.Concat(receivers).Distinct().Count();
        var recoParticipation = activeHeadcount == 0 ? 0 : Math.Round(participants * 100.0 / activeHeadcount, 1);
        var topValue = recos.Where(r => !string.IsNullOrWhiteSpace(r.CoreValueTag))
            .GroupBy(r => r.CoreValueTag!).OrderByDescending(g => g.Count()).FirstOrDefault();

        // ---- leaderboard (this month) ----
        var recvAgg = recos.GroupBy(r => r.ToHremployeeId)
            .Select(g => new { Id = g.Key, Points = g.Sum(x => x.Points), Count = g.Count() })
            .OrderByDescending(x => x.Points).ThenByDescending(x => x.Count).Take(5).ToList();
        var giveAgg = recos.GroupBy(r => r.FromHremployeeId)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count).Take(5).ToList();

        var nameIds = recvAgg.Select(x => x.Id).Concat(giveAgg.Select(x => x.Id)).Distinct().ToList();
        var names = await context.Hremployee.Where(e => nameIds.Contains(e.id))
            .Select(e => new { e.id, Name = (e.EmpName ?? "") + " " + (e.EmpSurname ?? "") })
            .ToDictionaryAsync(e => e.id, e => e.Name.Trim(), ct);

        var topReceivers = recvAgg.Select(x => new LeaderRow(names.GetValueOrDefault(x.Id, $"#{x.Id}"), x.Points, x.Count)).ToList();
        var topGivers = giveAgg.Select(x => new LeaderRow(names.GetValueOrDefault(x.Id, $"#{x.Id}"), 0, x.Count)).ToList();

        // ---- action plans ----
        var plans = await context.Eng_ActionPlans.Where(p => p.CompanyId == companyId)
            .Select(p => p.Status).ToListAsync(ct);
        var actionPlanned = plans.Count(s => s == Eng_ActionPlanStatus.Planned);
        var actionInProgress = plans.Count(s => s == Eng_ActionPlanStatus.InProgress);
        var actionCompleted = plans.Count(s => s == Eng_ActionPlanStatus.Completed);

        // ---- eNPS trend (recent ENPS campaigns) ----
        var trendCampaigns = campaigns.Where(c => c.CampaignType == Eng_CampaignType.ENPS)
            .OrderByDescending(c => c.CreatedDate).Take(6).OrderBy(c => c.CreatedDate).ToList();
        var trend = new List<TrendPoint>();
        foreach (var tc in trendCampaigns)
        {
            var s = await context.Eng_SurveyResponses.Where(r => r.CampaignId == tc.Id && r.NpsScore != null)
                .Select(r => r.NpsScore!.Value).ToListAsync(ct);
            var (e, _, _, _, n) = ComputeEnps(s);
            if (e is double ev) trend.Add(new TrendPoint(tc.Title.Length > 18 ? tc.Title[..18] : tc.Title, ev, n));
        }

        return new EngagementOverview(
            enps, promoters, passives, detractors, npsResponses, npsTitle,
            indexPct, indexRaw, indexTitle,
            totalInvited, totalResponses, participation,
            kudosThisMonth, pointsThisMonth, givers.Count, receivers.Count, recoParticipation,
            topValue?.Key, topValue?.Count() ?? 0,
            topReceivers, topGivers,
            plans.Count, actionPlanned, actionInProgress, actionCompleted,
            trend, activeHeadcount);
    }

    // eNPS = %promoters − %detractors (0–10 scale: 9–10 promoter, 7–8 passive, 0–6 detractor).
    private static (double? Enps, int Promoters, int Passives, int Detractors, int Total) ComputeEnps(List<int> scores)
    {
        if (scores.Count == 0) return (null, 0, 0, 0, 0);
        var p = scores.Count(s => s >= 9);
        var pa = scores.Count(s => s is 7 or 8);
        var d = scores.Count(s => s <= 6);
        var enps = Math.Round((p - d) * 100.0 / scores.Count, 0);
        return (enps, p, pa, d, scores.Count);
    }
}
