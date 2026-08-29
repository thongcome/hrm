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

    // Optional education/experience the candidate types on the public apply
    // form. The form is a plain static HTML <form> (see CareerEndpoints.cs
    // for why), so it offers a fixed number of slots rather than a dynamic
    // add-row UI: one "highest degree" education entry and up to 3 recent
    // jobs. Unlimited rows are only editable by HR afterwards, on
    // CandidateDetail.razor (a real interactive Blazor page).
    public record EducationInput(string? Level, string? Degree, string? Major, string? Institute, DateOnly? FinishedDate);
    public record ExperienceInput(string? Position, string? Company, DateOnly? StartDate, DateOnly? EndDate);

    public async Task<ApplyResult> ApplyPublicAsync(
        long jobPostingId, string firstName, string lastName, string email, string? phone,
        bool consentGiven, string? resumeOriginalFileName, byte[]? resumeBytes,
        EducationInput? education = null, List<ExperienceInput>? experiences = null, CancellationToken ct = default)
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

        // Repeat applicants who already have education/experience on file
        // aren't re-asked to fill the form in — we just don't duplicate rows
        // for a candidate who already has at least one on record.
        var hasEducation = await context.Rec_CandidateEducations.AnyAsync(e => e.CandidateId == candidate.Id && e.IsActive, ct);
        if (!hasEducation && education is not null && !string.IsNullOrWhiteSpace(education.Level))
        {
            context.Rec_CandidateEducations.Add(new Rec_CandidateEducation
            {
                CandidateId = candidate.Id,
                Level = education.Level,
                Degree = education.Degree,
                Major = education.Major,
                Institute = education.Institute,
                FinishedDate = education.FinishedDate,
                IsHighestDegree = true,
            });
        }

        var hasExperience = await context.Rec_CandidateExperiences.AnyAsync(e => e.CandidateId == candidate.Id && e.IsActive, ct);
        if (!hasExperience && experiences is { Count: > 0 })
        {
            foreach (var exp in experiences.Where(e => !string.IsNullOrWhiteSpace(e.Company) || !string.IsNullOrWhiteSpace(e.Position)))
            {
                context.Rec_CandidateExperiences.Add(new Rec_CandidateExperience
                {
                    CandidateId = candidate.Id,
                    Position = exp.Position,
                    Company = exp.Company,
                    StartDate = exp.StartDate,
                    EndDate = exp.EndDate,
                });
            }
        }
        await context.SaveChangesAsync(ct);

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

    // Career Management's internal-mobility apply path — reuses the exact
    // same Rec_Application/interview/offer pipeline as external hiring, just
    // skips resume upload/consent (the employee is already authenticated and
    // known) and tags the Rec_Candidate with HremployeeId + Source="Internal"
    // so it's traceable back to the employee record.
    public async Task<ApplyResult> ApplyInternalAsync(long jobPostingId, long hremployeeId, long actorUserId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var posting = await context.Rec_JobPostings.FirstOrDefaultAsync(p => p.Id == jobPostingId && p.Status == PostingStatus.Open && p.IsInternal, ct);
        if (posting is null)
            return new(false, "ไม่พบประกาศรับสมัครภายในนี้ หรือปิดรับสมัครแล้ว", null);

        var emp = await context.Hremployee.FirstOrDefaultAsync(e => e.id == hremployeeId, ct);
        if (emp is null)
            return new(false, "ไม่พบข้อมูลพนักงาน", null);

        var email = await HRM.Services.Shared.EmployeeEmailResolver.ResolveAsync(context, hremployeeId, ct);
        if (string.IsNullOrWhiteSpace(email))
            return new(false, "กรุณาตั้งค่าอีเมลของคุณก่อนสมัคร (ที่หน้าโปรไฟล์)", null);

        var candidate = await context.Rec_Candidates.FirstOrDefaultAsync(c => c.HremployeeId == hremployeeId, ct);
        if (candidate is null)
        {
            candidate = new Rec_Candidate
            {
                FirstName = emp.EmpName ?? emp.EmpNo,
                LastName = emp.EmpSurname ?? string.Empty,
                Email = email,
                Source = "Internal",
                HremployeeId = hremployeeId,
                ConsentGiven = true,
                ConsentDate = DateTime.Now,
                CreatedByUserId = actorUserId,
            };
            context.Rec_Candidates.Add(candidate);
            await context.SaveChangesAsync(ct);
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

    // IdCard is optional at application time but required before
    // RecOfferService.SubmitForHireApprovalAsync will start the hire-approval
    // workflow — see CandidateDetail.razor for where HR fills this in.
    public async Task UpdateCandidateIdCardAsync(long candidateId, string? idCard, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var candidate = await context.Rec_Candidates.FirstOrDefaultAsync(c => c.Id == candidateId, ct)
            ?? throw new InvalidOperationException("ไม่พบผู้สมัครนี้");
        candidate.IdCard = idCard;
        await context.SaveChangesAsync(ct);
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

    // Unlimited add/remove for HR on CandidateDetail.razor — fills the gap
    // for candidates who skipped the apply-form's fixed education/experience
    // slots, or were entered manually by HR in the first place.
    public async Task<List<Rec_CandidateEducation>> GetCandidateEducationAsync(long candidateId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        return await context.Rec_CandidateEducations.Where(e => e.CandidateId == candidateId && e.IsActive).OrderByDescending(e => e.IsHighestDegree).ThenByDescending(e => e.FinishedDate).ToListAsync(ct);
    }

    public async Task AddCandidateEducationAsync(Rec_CandidateEducation entry, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        entry.Id = 0;
        context.Rec_CandidateEducations.Add(entry);
        await context.SaveChangesAsync(ct);
    }

    public async Task RemoveCandidateEducationAsync(long id, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var entry = await context.Rec_CandidateEducations.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (entry is not null)
        {
            entry.IsActive = false;
            await context.SaveChangesAsync(ct);
        }
    }

    public async Task<List<Rec_CandidateExperience>> GetCandidateExperienceAsync(long candidateId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        return await context.Rec_CandidateExperiences.Where(e => e.CandidateId == candidateId && e.IsActive).OrderByDescending(e => e.StartDate).ToListAsync(ct);
    }

    public async Task AddCandidateExperienceAsync(Rec_CandidateExperience entry, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        entry.Id = 0;
        context.Rec_CandidateExperiences.Add(entry);
        await context.SaveChangesAsync(ct);
    }

    public async Task RemoveCandidateExperienceAsync(long id, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var entry = await context.Rec_CandidateExperiences.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (entry is not null)
        {
            entry.IsActive = false;
            await context.SaveChangesAsync(ct);
        }
    }
}
