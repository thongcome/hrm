namespace HRM.Services.Rec;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

public class RecInterviewService(IDbContextFactory<HRMContext> dbFactory)
{
    public async Task<long> ScheduleAsync(long applicationId, string round, DateTime scheduledDate, string? location,
        List<long> panelistHremployeeIds, long actorUserId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var interview = new Rec_Interview
        {
            ApplicationId = applicationId,
            Round = round,
            ScheduledDate = scheduledDate,
            Location = location,
            Status = InterviewStatus.Scheduled,
            CreatedByUserId = actorUserId,
        };
        context.Rec_Interviews.Add(interview);
        await context.SaveChangesAsync(ct);

        foreach (var (empId, index) in panelistHremployeeIds.Select((id, i) => (id, i)))
        {
            context.Rec_InterviewPanelists.Add(new Rec_InterviewPanelist
            {
                InterviewId = interview.Id,
                HremployeeId = empId,
                IsLead = index == 0,
            });
        }
        await context.SaveChangesAsync(ct);

        var app = await context.Rec_Applications.FirstOrDefaultAsync(a => a.Id == applicationId, ct);
        if (app is not null && app.Stage < ApplicationStage.Interview)
        {
            app.Stage = ApplicationStage.Interview;
            app.LastUpdatedByUserId = actorUserId;
            await context.SaveChangesAsync(ct);
        }

        return interview.Id;
    }

    public async Task<List<Rec_Interview>> GetInterviewsForApplicationAsync(long applicationId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        return await context.Rec_Interviews.Where(i => i.ApplicationId == applicationId).OrderByDescending(i => i.ScheduledDate).ToListAsync(ct);
    }

    public async Task<List<Rec_InterviewPanelist>> GetPanelistsAsync(long interviewId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        return await context.Rec_InterviewPanelists.Where(p => p.InterviewId == interviewId).ToListAsync(ct);
    }

    // Upsert by (InterviewId, HremployeeId) — mirrors
    // IdpAssessmentService.SubmitAssessmentAsync's upsert pattern.
    public async Task SubmitScorecardAsync(long interviewId, long hremployeeId, InterviewRecommendation recommendation,
        string? strengths, string? concerns, List<(long CompetencyId, int RatedLevel, string? Note)> ratings, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var scorecard = await context.Rec_InterviewScorecards
            .FirstOrDefaultAsync(s => s.InterviewId == interviewId && s.HremployeeId == hremployeeId, ct);
        if (scorecard is null)
        {
            scorecard = new Rec_InterviewScorecard { InterviewId = interviewId, HremployeeId = hremployeeId };
            context.Rec_InterviewScorecards.Add(scorecard);
            await context.SaveChangesAsync(ct);
        }

        scorecard.OverallRecommendation = recommendation;
        scorecard.Strengths = strengths;
        scorecard.Concerns = concerns;
        scorecard.SubmittedDate = DateTime.Now;

        var existingRatings = await context.Rec_ScorecardCompetencyRatings.Where(r => r.ScorecardId == scorecard.Id).ToListAsync(ct);
        context.Rec_ScorecardCompetencyRatings.RemoveRange(existingRatings);
        foreach (var (competencyId, ratedLevel, note) in ratings)
        {
            context.Rec_ScorecardCompetencyRatings.Add(new Rec_ScorecardCompetencyRating
            {
                ScorecardId = scorecard.Id,
                CompetencyId = competencyId,
                RatedLevel = ratedLevel,
                Note = note,
            });
        }

        await context.SaveChangesAsync(ct);
    }

    public async Task<Rec_InterviewScorecard?> GetScorecardAsync(long interviewId, long hremployeeId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        return await context.Rec_InterviewScorecards.FirstOrDefaultAsync(s => s.InterviewId == interviewId && s.HremployeeId == hremployeeId, ct);
    }

    public async Task<List<Rec_ScorecardCompetencyRating>> GetScorecardRatingsAsync(long scorecardId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        return await context.Rec_ScorecardCompetencyRatings.Where(r => r.ScorecardId == scorecardId).ToListAsync(ct);
    }

    // Required competencies for the position this application targets —
    // reuses the same Job_CompetencyRequirement lookup JobProfileDetail.razor
    // / IdpAssessmentService use, keyed off the requisition's Pos_ExecType.
    public async Task<List<Job_CompetencyRequirement>> GetRequiredCompetenciesAsync(long applicationId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var app = await context.Rec_Applications.FirstOrDefaultAsync(a => a.Id == applicationId, ct);
        if (app is null) return new();
        var posting = await context.Rec_JobPostings.FirstOrDefaultAsync(p => p.Id == app.JobPostingId, ct);
        if (posting is null) return new();
        var req = await context.Rec_Requisitions.FirstOrDefaultAsync(r => r.Id == posting.RequisitionId, ct);
        if (req is null) return new();

        return await context.Job_CompetencyRequirements
            .Include(r => r.Competency)
            .Where(r => r.PosExecTypeId == req.PosExecTypeId && r.IsActive)
            .OrderBy(r => r.SortOrder)
            .ToListAsync(ct);
    }
}
