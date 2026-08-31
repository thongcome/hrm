namespace HRM.Services.OrgDev;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Read-only access to the legacy manual culture-assessment snapshots
// (4 fixed 1-5 dimensions HR used to type in). Culture assessment now runs
// as anonymous Culture-type campaigns on the Engagement survey engine
// (Eng_CampaignType.Culture / SurveyService) — the write path here was
// removed with the entry form; existing rows are kept as read-only history
// on CultureAssessmentAdmin and as a fallback for the OrgHealthDashboard tile.
public class CultureAssessmentService(IDbContextFactory<HRMContext> dbFactory)
{
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
