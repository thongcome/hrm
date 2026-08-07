namespace HRM.Services.Rec;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

public class RecJobPostingService(IDbContextFactory<HRMContext> dbFactory)
{
    public async Task<long> CreateAsync(Rec_JobPosting posting, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        posting.Id = 0;
        context.Rec_JobPostings.Add(posting);
        await context.SaveChangesAsync(ct);
        return posting.Id;
    }

    public async Task UpdateAsync(Rec_JobPosting posting, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var existing = await context.Rec_JobPostings.FirstOrDefaultAsync(p => p.Id == posting.Id, ct)
            ?? throw new InvalidOperationException("ไม่พบประกาศรับสมัครนี้");
        existing.Title = posting.Title;
        existing.Description = posting.Description;
        existing.Status = posting.Status;
        existing.OpenDate = posting.OpenDate;
        existing.CloseDate = posting.CloseDate;
        await context.SaveChangesAsync(ct);
    }

    public async Task<List<Rec_JobPosting>> GetAllForCompanyAsync(string companyId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var requisitionIds = await context.Rec_Requisitions.Where(r => r.CompanyId == companyId).Select(r => r.Id).ToListAsync(ct);
        return await context.Rec_JobPostings
            .Where(p => requisitionIds.Contains(p.RequisitionId))
            .OrderByDescending(p => p.Id)
            .ToListAsync(ct);
    }

    public async Task<Rec_JobPosting?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        return await context.Rec_JobPostings.FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    // Public career-site query — deliberately joins nothing beyond Open
    // postings, so it can never leak internal requisition/candidate data to
    // an anonymous visitor.
    public async Task<List<Rec_JobPosting>> GetOpenPostingsPublicAsync(CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var today = DateOnly.FromDateTime(DateTime.Today);
        return await context.Rec_JobPostings
            .Where(p => p.Status == PostingStatus.Open
                && (p.OpenDate == null || p.OpenDate <= today)
                && (p.CloseDate == null || p.CloseDate >= today))
            .OrderByDescending(p => p.Id)
            .ToListAsync(ct);
    }

    public async Task<Rec_JobPosting?> GetOpenPostingPublicAsync(long id, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var today = DateOnly.FromDateTime(DateTime.Today);
        return await context.Rec_JobPostings
            .FirstOrDefaultAsync(p => p.Id == id && p.Status == PostingStatus.Open
                && (p.OpenDate == null || p.OpenDate <= today)
                && (p.CloseDate == null || p.CloseDate >= today), ct);
    }
}
