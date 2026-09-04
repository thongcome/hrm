namespace HRM.Services.Engagement;

using HRM.Models;
using HRM.Services.Shared;
using Microsoft.EntityFrameworkCore;

// Campaign lifecycle (draft -> open -> closed), targeting, and anonymous
// response collection for Survey/Pulse/eNPS campaigns. Submissions never
// carry an employee identity anywhere — see Eng_SurveyResponse's comment —
// so this service can report participation only as an aggregate count
// against ResolveTargetEmployeesAsync's invited list, never per-person.
public class SurveyService(IDbContextFactory<HRMContext> dbFactory)
{
    public async Task<List<Eng_SurveyCampaign>> GetCampaignsForAdminAsync(string companyId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        return await context.Eng_SurveyCampaigns
            .Where(c => c.CompanyId == companyId)
            .OrderByDescending(c => c.CreatedDate)
            .ToListAsync(ct);
    }

    public async Task<Eng_SurveyCampaign?> GetCampaignAsync(long campaignId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        return await context.Eng_SurveyCampaigns.FirstOrDefaultAsync(c => c.Id == campaignId, ct);
    }

    public async Task<List<Eng_CampaignQuestion>> GetQuestionsAsync(long campaignId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        return await context.Eng_CampaignQuestions
            .Where(q => q.CampaignId == campaignId)
            .OrderBy(q => q.SortOrder)
            .ToListAsync(ct);
    }

    public async Task<List<Eng_CampaignTarget>> GetTargetsAsync(long campaignId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        return await context.Eng_CampaignTargets.Where(t => t.CampaignId == campaignId).ToListAsync(ct);
    }

    public record CultureDimension(string Code, string Text);

    // The 4 standard culture dimensions the old OrgDev_CultureAssessment manual
    // form scored — now question-bank templates for Culture-type campaigns.
    public static readonly IReadOnlyList<CultureDimension> CultureDimensions = new List<CultureDimension>
    {
        new("CULTURE-COMM", "การสื่อสารในองค์กร (Communication)"),
        new("CULTURE-TRUST", "ความไว้วางใจ (Trust)"),
        new("CULTURE-COLLAB", "การทำงานร่วมกัน (Collaboration)"),
        new("CULTURE-LEAD", "ภาวะผู้นำ (Leadership)"),
    };

    // Idempotent add-missing seeding (matched by Code, falling back to exact
    // Text for rows created before codes existed) of the 4 standard culture
    // dimensions into the company's question bank, following the house
    // "idempotent catalog seeding, add-missing by code" convention. Runtime
    // seeding instead of a migration because question templates are
    // company-scoped, user-editable catalog data — not global schema — and
    // each company opts in from the Culture Assessment page. Returns how many
    // templates were newly created (0 = everything already existed).
    // actorUserId identifies who triggered the seeding; the write itself is
    // audit-logged automatically by HRMContext.Audit's SaveChangesAsync hook.
    public async Task<int> EnsureCultureQuestionTemplatesAsync(string companyId, long actorUserId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var codes = CultureDimensions.Select(d => d.Code).ToList();
        var texts = CultureDimensions.Select(d => d.Text).ToList();
        var existing = await context.Eng_QuestionTemplates
            .Where(t => t.CompanyId == companyId
                && ((t.Code != null && codes.Contains(t.Code)) || texts.Contains(t.Text)))
            .ToListAsync(ct);

        var created = 0;
        foreach (var dim in CultureDimensions)
        {
            if (existing.Any(t => t.Code == dim.Code || t.Text == dim.Text)) continue;
            context.Eng_QuestionTemplates.Add(new Eng_QuestionTemplate
            {
                Code = dim.Code,
                CompanyId = companyId,
                Text = dim.Text,
                QuestionType = Eng_QuestionType.Rating,
                IsActive = true,
            });
            created++;
        }
        if (created > 0) await context.SaveChangesAsync(ct);
        return created;
    }

    public async Task<Eng_SurveyCampaign> CreateDraftAsync(string companyId, string title, string? description,
        Eng_CampaignType campaignType, long actorUserId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var campaign = new Eng_SurveyCampaign
        {
            CompanyId = companyId,
            Title = title,
            Description = description,
            CampaignType = campaignType,
            Status = Eng_CampaignStatus.Draft,
            CreatedByUserId = actorUserId,
            CreatedDate = DateTime.Now,
        };
        context.Eng_SurveyCampaigns.Add(campaign);
        await context.SaveChangesAsync(ct);
        return campaign;
    }

    private static async Task GuardDraftAsync(HRMContext context, long campaignId, CancellationToken ct)
    {
        var campaign = await context.Eng_SurveyCampaigns.FirstOrDefaultAsync(c => c.Id == campaignId, ct)
            ?? throw new InvalidOperationException("ไม่พบแคมเปญนี้แล้ว");
        if (campaign.Status != Eng_CampaignStatus.Draft)
            throw new InvalidOperationException("แก้ไขได้เฉพาะแคมเปญที่ยังเป็นฉบับร่างเท่านั้น");
    }

    public async Task AddQuestionFromTemplateAsync(long campaignId, long templateId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        await GuardDraftAsync(context, campaignId, ct);

        var template = await context.Eng_QuestionTemplates.FirstOrDefaultAsync(t => t.Id == templateId, ct)
            ?? throw new InvalidOperationException("ไม่พบคำถามนี้แล้ว");

        var nextSort = await context.Eng_CampaignQuestions.Where(q => q.CampaignId == campaignId)
            .Select(q => (int?)q.SortOrder).MaxAsync(ct) ?? 0;

        context.Eng_CampaignQuestions.Add(new Eng_CampaignQuestion
        {
            CampaignId = campaignId,
            SourceTemplateId = template.Id,
            Text = template.Text,
            QuestionType = template.QuestionType,
            Options = template.Options,
            SortOrder = nextSort + 1,
        });
        await context.SaveChangesAsync(ct);
    }

    public async Task AddAdHocQuestionAsync(long campaignId, string text, Eng_QuestionType questionType, string? options, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        await GuardDraftAsync(context, campaignId, ct);

        var nextSort = await context.Eng_CampaignQuestions.Where(q => q.CampaignId == campaignId)
            .Select(q => (int?)q.SortOrder).MaxAsync(ct) ?? 0;

        context.Eng_CampaignQuestions.Add(new Eng_CampaignQuestion
        {
            CampaignId = campaignId,
            SourceTemplateId = null,
            Text = text,
            QuestionType = questionType,
            Options = options,
            SortOrder = nextSort + 1,
        });
        await context.SaveChangesAsync(ct);
    }

    public async Task RemoveQuestionAsync(long campaignQuestionId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var question = await context.Eng_CampaignQuestions.FirstOrDefaultAsync(q => q.Id == campaignQuestionId, ct);
        if (question is null) return;
        await GuardDraftAsync(context, question.CampaignId, ct);
        context.Eng_CampaignQuestions.Remove(question);
        await context.SaveChangesAsync(ct);
    }

    // Wholesale-replace targets — same simple approach as InfoMessageService.SaveAsync.
    public async Task SaveTargetsAsync(long campaignId, List<Eng_CampaignTarget> targets, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        await GuardDraftAsync(context, campaignId, ct);

        var oldTargets = await context.Eng_CampaignTargets.Where(t => t.CampaignId == campaignId).ToListAsync(ct);
        context.Eng_CampaignTargets.RemoveRange(oldTargets);
        foreach (var t in targets)
        {
            t.Id = 0;
            t.CampaignId = campaignId;
            context.Eng_CampaignTargets.Add(t);
        }
        await context.SaveChangesAsync(ct);
    }

    public async Task OpenCampaignAsync(long campaignId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var campaign = await context.Eng_SurveyCampaigns.FirstOrDefaultAsync(c => c.Id == campaignId, ct)
            ?? throw new InvalidOperationException("ไม่พบแคมเปญนี้แล้ว");
        if (campaign.Status != Eng_CampaignStatus.Draft)
            throw new InvalidOperationException("เปิดได้เฉพาะแคมเปญที่ยังเป็นฉบับร่างเท่านั้น");

        var hasQuestions = await context.Eng_CampaignQuestions.AnyAsync(q => q.CampaignId == campaignId, ct);
        if (!hasQuestions)
            throw new InvalidOperationException("ต้องมีอย่างน้อย 1 คำถามก่อนเปิดแคมเปญ");

        var invited = await ResolveTargetEmployeesAsync(context, campaignId, campaign.CompanyId, ct);

        campaign.Status = Eng_CampaignStatus.Open;
        campaign.OpenDate ??= DateOnly.FromDateTime(DateTime.Today);
        campaign.InvitedCount = invited.Count;
        await context.SaveChangesAsync(ct);
    }

    public async Task CloseCampaignAsync(long campaignId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var campaign = await context.Eng_SurveyCampaigns.FirstOrDefaultAsync(c => c.Id == campaignId, ct)
            ?? throw new InvalidOperationException("ไม่พบแคมเปญนี้แล้ว");
        if (campaign.Status != Eng_CampaignStatus.Open)
            throw new InvalidOperationException("ปิดได้เฉพาะแคมเปญที่กำลังเปิดอยู่เท่านั้น");

        campaign.Status = Eng_CampaignStatus.Closed;
        campaign.CloseDate = DateOnly.FromDateTime(DateTime.Today);
        await context.SaveChangesAsync(ct);
    }

    // Recurring-pulse support: copies questions + targets into a fresh Draft
    // campaign, leaving the source campaign (and its responses) untouched.
    public async Task<Eng_SurveyCampaign> RelaunchFromTemplateAsync(long sourceCampaignId, long actorUserId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var source = await context.Eng_SurveyCampaigns.FirstOrDefaultAsync(c => c.Id == sourceCampaignId, ct)
            ?? throw new InvalidOperationException("ไม่พบแคมเปญต้นทางนี้แล้ว");

        var sourceQuestions = await context.Eng_CampaignQuestions.Where(q => q.CampaignId == sourceCampaignId).ToListAsync(ct);
        var sourceTargets = await context.Eng_CampaignTargets.Where(t => t.CampaignId == sourceCampaignId).ToListAsync(ct);

        var newCampaign = new Eng_SurveyCampaign
        {
            CompanyId = source.CompanyId,
            Title = source.Title,
            Description = source.Description,
            CampaignType = source.CampaignType,
            Status = Eng_CampaignStatus.Draft,
            RelaunchedFromCampaignId = source.Id,
            CreatedByUserId = actorUserId,
            CreatedDate = DateTime.Now,
        };
        context.Eng_SurveyCampaigns.Add(newCampaign);
        await context.SaveChangesAsync(ct);

        foreach (var q in sourceQuestions)
        {
            context.Eng_CampaignQuestions.Add(new Eng_CampaignQuestion
            {
                CampaignId = newCampaign.Id,
                SourceTemplateId = q.SourceTemplateId,
                Text = q.Text,
                QuestionType = q.QuestionType,
                Options = q.Options,
                SortOrder = q.SortOrder,
            });
        }
        foreach (var t in sourceTargets)
        {
            context.Eng_CampaignTargets.Add(new Eng_CampaignTarget
            {
                CampaignId = newCampaign.Id,
                TargetType = t.TargetType,
                TargetOrganizationId = t.TargetOrganizationId,
                TargetHremployeeId = t.TargetHremployeeId,
            });
        }
        await context.SaveChangesAsync(ct);

        return newCampaign;
    }

    // A campaign with zero target rows is visible to everyone in the
    // company — same deliberate fallback as InfoMessageService.
    public static async Task<List<Hremployee>> ResolveTargetEmployeesAsync(HRMContext context, long campaignId, string companyId, CancellationToken ct)
    {
        var targets = await context.Eng_CampaignTargets.Where(t => t.CampaignId == campaignId).ToListAsync(ct);

        if (targets.Count == 0 || targets.Any(t => t.TargetType == Eng_TargetType.All))
            return await context.Hremployee.Where(e => e.companyid == companyId && e.ResignDate == null).ToListAsync(ct);

        var result = new Dictionary<long, Hremployee>();

        var directEmpIds = targets.Where(t => t.TargetType == Eng_TargetType.Employee && t.TargetHremployeeId != null)
            .Select(t => t.TargetHremployeeId!.Value).Distinct().ToList();
        if (directEmpIds.Count > 0)
        {
            var direct = await context.Hremployee
                .Where(e => directEmpIds.Contains(e.id) && e.companyid == companyId && e.ResignDate == null)
                .ToListAsync(ct);
            foreach (var e in direct) result[e.id] = e;
        }

        var orgIds = targets.Where(t => t.TargetType == Eng_TargetType.Organization && t.TargetOrganizationId != null)
            .Select(t => t.TargetOrganizationId!.Value).Distinct().ToList();
        foreach (var orgId in orgIds)
        {
            var inOrg = await OrgEmployeeResolverHelper.ResolveOrganizationSubtreeAsync(context, companyId, orgId, ct);
            foreach (var e in inOrg) result[e.id] = e;
        }

        return result.Values.ToList();
    }

    private static bool IsEmployeeTargeted(Hremployee employee, List<Eng_CampaignTarget> targets, Dictionary<long, com_organization> orgById)
    {
        if (targets.Count == 0) return true;
        foreach (var t in targets)
        {
            if (t.TargetType == Eng_TargetType.All) return true;
            if (t.TargetType == Eng_TargetType.Employee && t.TargetHremployeeId == employee.id) return true;
            if (t.TargetType == Eng_TargetType.Organization && t.TargetOrganizationId is long orgId
                && orgById.TryGetValue(orgId, out var org) && !string.IsNullOrWhiteSpace(org.orgcodefull)
                && !string.IsNullOrWhiteSpace(employee.orgcodefull) && employee.orgcodefull.StartsWith(org.orgcodefull))
                return true;
        }
        return false;
    }

    // Campaigns an employee should see in their "surveys to answer" list —
    // Open status + targeted at them (or untargeted = everyone).
    public async Task<List<Eng_SurveyCampaign>> GetVisibleOpenCampaignsAsync(Hremployee employee, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var campaigns = await context.Eng_SurveyCampaigns
            .Where(c => c.CompanyId == employee.companyid && c.Status == Eng_CampaignStatus.Open)
            .ToListAsync(ct);
        if (campaigns.Count == 0) return new();

        var ids = campaigns.Select(c => c.Id).ToList();
        var targets = await context.Eng_CampaignTargets.Where(t => ids.Contains(t.CampaignId)).ToListAsync(ct);
        var targetsByCampaign = targets.GroupBy(t => t.CampaignId).ToDictionary(g => g.Key, g => g.ToList());

        var orgIds = targets.Where(t => t.TargetOrganizationId != null).Select(t => t.TargetOrganizationId!.Value).Distinct().ToList();
        var orgById = orgIds.Count == 0
            ? new Dictionary<long, com_organization>()
            : (await context.com_organizations.Where(o => orgIds.Contains(o.id)).ToListAsync(ct)).ToDictionary(o => o.id);

        return campaigns.Where(c =>
            IsEmployeeTargeted(employee, targetsByCampaign.GetValueOrDefault(c.Id, new()), orgById))
            .ToList();
    }

    public record AnswerInput(long CampaignQuestionId, int? RatingValue, string? TextValue, string? ChoiceValue, bool? YesNoValue);

    // Fully anonymous submission — no employee identity is recorded anywhere
    // in this write, per the confirmed anonymity requirement. Only the
    // campaign-level ResponseCount aggregate advances.
    public async Task SubmitResponseAsync(long campaignId, List<AnswerInput> answers, int? npsScore, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var campaign = await context.Eng_SurveyCampaigns.FirstOrDefaultAsync(c => c.Id == campaignId, ct)
            ?? throw new InvalidOperationException("ไม่พบแคมเปญนี้แล้ว");
        if (campaign.Status != Eng_CampaignStatus.Open)
            throw new InvalidOperationException("แคมเปญนี้ไม่ได้เปิดรับคำตอบอยู่");

        var response = new Eng_SurveyResponse
        {
            CampaignId = campaignId,
            SubmittedDate = DateTime.Now,
            NpsScore = campaign.CampaignType == Eng_CampaignType.ENPS ? npsScore : null,
        };
        context.Eng_SurveyResponses.Add(response);
        await context.SaveChangesAsync(ct);

        foreach (var a in answers)
        {
            context.Eng_SurveyAnswers.Add(new Eng_SurveyAnswer
            {
                ResponseId = response.Id,
                CampaignQuestionId = a.CampaignQuestionId,
                RatingValue = a.RatingValue,
                TextValue = a.TextValue,
                ChoiceValue = a.ChoiceValue,
                YesNoValue = a.YesNoValue,
            });
        }

        campaign.ResponseCount += 1;
        await context.SaveChangesAsync(ct);
    }

    public record QuestionResult(long QuestionId, string Text, Eng_QuestionType QuestionType,
        double? AverageRating, int? YesCount, int? NoCount, List<TextAnswerRow> TextAnswers, Dictionary<string, int> ChoiceDistribution,
        // Rating questions only: how many answers fell on each 1–5 point, and the
        // total rated, so the UI can show favourable-% (4–5) and a distribution.
        Dictionary<int, int> RatingDistribution, int RatingCount);

    public record TextAnswerRow(string Text);

    public record CampaignResults(int ResponseCount, int InvitedCount, double? NpsScore,
        int? PromoterCount, int? PassiveCount, int? DetractorCount, List<QuestionResult> Questions);

    // Aggregate-only results — never joins back to a respondent identity
    // (there isn't one to join to).
    public async Task<CampaignResults> GetResultsAsync(long campaignId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var campaign = await context.Eng_SurveyCampaigns.FirstOrDefaultAsync(c => c.Id == campaignId, ct)
            ?? throw new InvalidOperationException("ไม่พบแคมเปญนี้แล้ว");

        var questions = await context.Eng_CampaignQuestions.Where(q => q.CampaignId == campaignId).OrderBy(q => q.SortOrder).ToListAsync(ct);
        var responses = await context.Eng_SurveyResponses.Where(r => r.CampaignId == campaignId).ToListAsync(ct);
        var responseIds = responses.Select(r => r.Id).ToList();
        var answers = responseIds.Count == 0
            ? new List<Eng_SurveyAnswer>()
            : await context.Eng_SurveyAnswers.Where(a => responseIds.Contains(a.ResponseId)).ToListAsync(ct);

        double? npsScore = null;
        int? promoters = null, passives = null, detractors = null;
        if (campaign.CampaignType == Eng_CampaignType.ENPS)
        {
            var scores = responses.Where(r => r.NpsScore != null).Select(r => r.NpsScore!.Value).ToList();
            if (ComputeEnps(scores) is { } enps)
            {
                promoters = enps.Promoters;
                passives = enps.Passives;
                detractors = enps.Detractors;
                npsScore = enps.Score;
            }
        }

        var questionResults = new List<QuestionResult>();
        foreach (var q in questions)
        {
            var qAnswers = answers.Where(a => a.CampaignQuestionId == q.Id).ToList();

            double? avgRating = null;
            int? yesCount = null, noCount = null;
            var textAnswers = new List<TextAnswerRow>();
            var choiceDist = new Dictionary<string, int>();
            var ratingDist = new Dictionary<int, int>();
            var ratingCount = 0;

            switch (q.QuestionType)
            {
                case Eng_QuestionType.Rating:
                    var ratings = qAnswers.Where(a => a.RatingValue != null).Select(a => a.RatingValue!.Value).ToList();
                    if (ratings.Count > 0) avgRating = Math.Round(ratings.Average(), 2);
                    ratingCount = ratings.Count;
                    foreach (var rv in ratings) ratingDist[rv] = ratingDist.GetValueOrDefault(rv) + 1;
                    break;
                case Eng_QuestionType.YesNo:
                    yesCount = qAnswers.Count(a => a.YesNoValue == true);
                    noCount = qAnswers.Count(a => a.YesNoValue == false);
                    break;
                case Eng_QuestionType.Text:
                    textAnswers = qAnswers.Where(a => !string.IsNullOrWhiteSpace(a.TextValue))
                        .Select(a => new TextAnswerRow(a.TextValue!)).ToList();
                    break;
                case Eng_QuestionType.MultipleChoice:
                    foreach (var a in qAnswers.Where(a => !string.IsNullOrWhiteSpace(a.ChoiceValue)))
                        choiceDist[a.ChoiceValue!] = choiceDist.GetValueOrDefault(a.ChoiceValue!) + 1;
                    break;
            }

            questionResults.Add(new QuestionResult(q.Id, q.Text, q.QuestionType, avgRating, yesCount, noCount, textAnswers, choiceDist, ratingDist, ratingCount));
        }

        return new CampaignResults(campaign.ResponseCount, campaign.InvitedCount, npsScore, promoters, passives, detractors, questionResults);
    }

    // The one eNPS formula, shared by GetResultsAsync and GetSeriesTrendAsync:
    // promoter = score >= 9, detractor = score <= 6, eNPS = (P - D) * 100 / total.
    private static (double Score, int Promoters, int Passives, int Detractors)? ComputeEnps(List<int> scores)
    {
        if (scores.Count == 0) return null;
        var promoters = scores.Count(s => s >= 9);
        var detractors = scores.Count(s => s <= 6);
        var passives = scores.Count - promoters - detractors;
        var score = Math.Round((promoters - detractors) * 100.0 / scores.Count, 1);
        return (score, promoters, passives, detractors);
    }

    public record SeriesTrendRow(long CampaignId, string Title, DateOnly? OpenDate, DateOnly? CloseDate,
        int ResponseCount, double? NpsScore, double? AverageRating);

    // Trend across a recurring pulse series. A "series" is every campaign whose
    // RelaunchedFromCampaignId chain reaches the same root campaign as the one
    // given. Returns one row per Open/Closed campaign in the series, ordered
    // oldest -> newest; a standalone campaign yields a single row, which callers
    // treat as "no series". Aggregate-only, same anonymity rules as GetResultsAsync.
    public async Task<List<SeriesTrendRow>> GetSeriesTrendAsync(long campaignId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var campaign = await context.Eng_SurveyCampaigns.FirstOrDefaultAsync(c => c.Id == campaignId, ct);
        if (campaign is null) return new();

        // Series resolution: campaign counts per company are small (tens, not
        // thousands), so load them all once and walk each chain in memory
        // instead of a recursive SQL query.
        var all = await context.Eng_SurveyCampaigns
            .Where(c => c.CompanyId == campaign.CompanyId)
            .ToListAsync(ct);
        var byId = all.ToDictionary(c => c.Id);

        long RootOf(Eng_SurveyCampaign c)
        {
            var seen = new HashSet<long> { c.Id }; // cycle guard, in case of bad data
            while (c.RelaunchedFromCampaignId is long parentId
                   && byId.TryGetValue(parentId, out var parent)
                   && seen.Add(parentId))
                c = parent;
            return c.Id;
        }

        var rootId = RootOf(campaign);
        var series = all
            .Where(c => RootOf(c) == rootId)
            .Where(c => c.Status is Eng_CampaignStatus.Open or Eng_CampaignStatus.Closed)
            .OrderBy(c => c.OpenDate ?? DateOnly.FromDateTime(c.CreatedDate))
            .ThenBy(c => c.CreatedDate)
            .ToList();
        if (series.Count == 0) return new();

        var seriesIds = series.Select(c => c.Id).ToList();

        // Batched aggregate lookups for the whole series (no per-round queries).
        var responses = await context.Eng_SurveyResponses
            .Where(r => seriesIds.Contains(r.CampaignId))
            .Select(r => new { r.Id, r.CampaignId, r.NpsScore })
            .ToListAsync(ct);
        var campaignByResponseId = responses.ToDictionary(r => r.Id, r => r.CampaignId);

        var ratingQuestionIds = await context.Eng_CampaignQuestions
            .Where(q => seriesIds.Contains(q.CampaignId) && q.QuestionType == Eng_QuestionType.Rating)
            .Select(q => q.Id)
            .ToListAsync(ct);

        var ratingsByCampaign = new Dictionary<long, List<int>>();
        if (ratingQuestionIds.Count > 0)
        {
            var ratingAnswers = await context.Eng_SurveyAnswers
                .Where(a => ratingQuestionIds.Contains(a.CampaignQuestionId) && a.RatingValue != null)
                .Select(a => new { a.ResponseId, RatingValue = a.RatingValue!.Value })
                .ToListAsync(ct);
            foreach (var a in ratingAnswers)
            {
                if (!campaignByResponseId.TryGetValue(a.ResponseId, out var cid)) continue;
                if (!ratingsByCampaign.TryGetValue(cid, out var list))
                    ratingsByCampaign[cid] = list = new();
                list.Add(a.RatingValue);
            }
        }

        var rows = new List<SeriesTrendRow>();
        foreach (var c in series)
        {
            double? enps = null;
            if (c.CampaignType == Eng_CampaignType.ENPS)
            {
                var scores = responses.Where(r => r.CampaignId == c.Id && r.NpsScore != null)
                    .Select(r => r.NpsScore!.Value).ToList();
                enps = ComputeEnps(scores)?.Score;
            }

            double? avgRating = ratingsByCampaign.TryGetValue(c.Id, out var ratings) && ratings.Count > 0
                ? Math.Round(ratings.Average(), 2)
                : null;

            rows.Add(new SeriesTrendRow(c.Id, c.Title, c.OpenDate, c.CloseDate, c.ResponseCount, enps, avgRating));
        }
        return rows;
    }
}
