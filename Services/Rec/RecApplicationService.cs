namespace HRM.Services.Rec;

using HRM.Models;
using HRM.Services.Pay;
using Microsoft.EntityFrameworkCore;

// Application lifecycle, including the public (unauthenticated) apply path.
// ApplyPublicAsync is the one method in this codebase's Rec_* module that
// can be called with no logged-in user at all — see
// Endpoints/CareerEndpoints.cs, which is the only caller.
public class RecApplicationService(IDbContextFactory<HRMContext> dbFactory, PrivateFileStorage fileStorage)
{
    private const string ResumeDocTypeCode = "CANDIDATE_RESUME";
    private static readonly string[] AllowedResumeExtensions = [".pdf", ".doc", ".docx"];
    private const long MaxResumeBytes = 5 * 1024 * 1024;

    public record ApplyResult(bool Success, string? Error, long? ApplicationId);

    public async Task<ApplyResult> ApplyPublicAsync(
        long jobPostingId, string firstName, string lastName, string email, string? phone,
        bool consentGiven, string? resumeOriginalFileName, byte[]? resumeBytes, CancellationToken ct = default)
    {
        if (!consentGiven)
            return new(false, "กรุณายืนยันความยินยอมให้เก็บและใช้ข้อมูลส่วนบุคคลก่อนส่งใบสมัคร", null);
        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            return new(false, "กรุณากรอกชื่อและนามสกุล", null);
        if (!HRM.Services.Shared.EmployeeEmailResolver.IsValidFormat(email))
            return new(false, "รูปแบบอีเมลไม่ถูกต้อง", null);

        string? resumeExt = null;
        if (resumeBytes is not null && !string.IsNullOrWhiteSpace(resumeOriginalFileName))
        {
            resumeExt = Path.GetExtension(resumeOriginalFileName).ToLowerInvariant();
            if (!AllowedResumeExtensions.Contains(resumeExt))
                return new(false, "ไฟล์เรซูเม่ต้องเป็น PDF, DOC หรือ DOCX เท่านั้น", null);
            if (resumeBytes.Length > MaxResumeBytes)
                return new(false, "ไฟล์เรซูเม่ต้องมีขนาดไม่เกิน 5MB", null);
        }

        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var posting = await context.Rec_JobPostings.FirstOrDefaultAsync(p => p.Id == jobPostingId && p.Status == PostingStatus.Open, ct);
        if (posting is null)
            return new(false, "ไม่พบประกาศรับสมัครนี้ หรือปิดรับสมัครแล้ว", null);

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var candidate = await context.Rec_Candidates.FirstOrDefaultAsync(c => c.Email.ToLower() == normalizedEmail, ct);
        if (candidate is null)
        {
            candidate = new Rec_Candidate
            {
                FirstName = firstName.Trim(),
                LastName = lastName.Trim(),
                Email = email.Trim(),
                Phone = phone,
                Source = "CareerSite",
                ConsentGiven = true,
                ConsentDate = DateTime.Now,
            };
            context.Rec_Candidates.Add(candidate);
            await context.SaveChangesAsync(ct);
        }
        else if (!candidate.ConsentGiven)
        {
            candidate.ConsentGiven = true;
            candidate.ConsentDate = DateTime.Now;
        }

        if (resumeBytes is not null && resumeExt is not null)
        {
            // Server-generated filename only — never trust the uploaded
            // filename for the stored path (path traversal / collision risk).
            var storedFileName = $"{candidate.Id}_{DateTime.Now:yyyyMMddHHmmssfff}{resumeExt}";
            var (relativePath, _) = await fileStorage.SaveAsync("candidate-resumes", storedFileName, resumeBytes, ct);

            var doc = new doc_center
            {
                doctypecode = ResumeDocTypeCode,
                refid = candidate.Id,
                files = resumeOriginalFileName,
                path = relativePath,
                isActive = true,
                moddate = DateTime.Now,
            };
            context.doc_centers.Add(doc);
            await context.SaveChangesAsync(ct);
            candidate.ResumeDocCenterId = doc.id;
        }

        var alreadyApplied = await context.Rec_Applications.AnyAsync(a => a.CandidateId == candidate.Id && a.JobPostingId == jobPostingId, ct);
        if (alreadyApplied)
            return new(false, "คุณได้สมัครตำแหน่งนี้ไปแล้ว", null);

        var application = new Rec_Application
        {
            CandidateId = candidate.Id,
            JobPostingId = jobPostingId,
            Stage = ApplicationStage.Applied,
        };
        context.Rec_Applications.Add(application);
        await context.SaveChangesAsync(ct);

        return new(true, null, application.Id);
    }

    public async Task<List<Rec_Application>> GetApplicationsForPostingAsync(long jobPostingId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        return await context.Rec_Applications.Where(a => a.JobPostingId == jobPostingId).OrderByDescending(a => a.Id).ToListAsync(ct);
    }

    public async Task<Rec_Application?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        return await context.Rec_Applications.FirstOrDefaultAsync(a => a.Id == id, ct);
    }

    public async Task<Rec_Candidate?> GetCandidateAsync(long candidateId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        return await context.Rec_Candidates.FirstOrDefaultAsync(c => c.Id == candidateId, ct);
    }

    public async Task<List<Rec_Application>> GetApplicationHistoryForCandidateAsync(long candidateId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        return await context.Rec_Applications.Where(a => a.CandidateId == candidateId).OrderByDescending(a => a.Id).ToListAsync(ct);
    }

    public async Task UpdateStageAsync(long applicationId, ApplicationStage stage, long actorUserId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var app = await context.Rec_Applications.FirstOrDefaultAsync(a => a.Id == applicationId, ct)
            ?? throw new InvalidOperationException("ไม่พบใบสมัครนี้");
        app.Stage = stage;
        app.LastUpdatedByUserId = actorUserId;
        await context.SaveChangesAsync(ct);
    }

    public async Task RejectAsync(long applicationId, string reason, long actorUserId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var app = await context.Rec_Applications.FirstOrDefaultAsync(a => a.Id == applicationId, ct)
            ?? throw new InvalidOperationException("ไม่พบใบสมัครนี้");
        app.Stage = ApplicationStage.Rejected;
        app.RejectedReason = reason;
        app.RejectedDate = DateTime.Now;
        app.LastUpdatedByUserId = actorUserId;
        await context.SaveChangesAsync(ct);
    }
}
