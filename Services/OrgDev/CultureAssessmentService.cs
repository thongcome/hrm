namespace HRM.Services.OrgDev;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Culture Assessment: a manual, periodic HR-entered org-health snapshot
// (4 fixed 1-5 dimensions) — deliberately not a survey/response engine, see
// the comment on OrgDev_CultureAssessment.cs for why.
public class CultureAssessmentService(IDbContextFactory<HRMContext> dbFactory)
{
    public async Task<long> RecordAsync(string companyId, long? organizationId, DateOnly assessmentDate,
        int communicationScore, int trustScore, int collaborationScore, int leadershipScore, string? note, long actorUserId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var assessment = new OrgDev_CultureAssessment
        {
            CompanyId = companyId,
            OrganizationId = organizationId,
            AssessmentDate = assessmentDate,
            CommunicationScore = communicationScore,
            TrustScore = trustScore,
            CollaborationScore = collaborationScore,
            LeadershipScore = leadershipScore,
            Note = note,
            ConductedByUserId = actorUserId,
        };
        context.OrgDev_CultureAssessments.Add(assessment);
        await context.SaveChangesAsync(ct);
        return assessment.Id;
    }

    public async Task<List<OrgDev_CultureAssessment>> GetHistoryAsync(string companyId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        return await context.OrgDev_CultureAssessments.Where(a => a.CompanyId == companyId).OrderByDescending(a => a.AssessmentDate).ToListAsync(ct);
    }

    public async Task<OrgDev_CultureAssessment?> GetLatestAsync(string companyId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        return await context.OrgDev_CultureAssessments.Where(a => a.CompanyId == companyId).OrderByDescending(a => a.AssessmentDate).FirstOrDefaultAsync(ct);
    }
}
