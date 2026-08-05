using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Hrd;

// Onboarding/Offboarding checklist engine. No workflow-engine integration by
// design (confirmed with user) — this is a plain HR-driven checklist, not a
// gated approval flow like Org_OrganizationChangeRequest. Templates are
// copied into per-employee task instances at case-start time; editing a
// template afterwards never retroactively changes already-assigned tasks
// because Hrd_LifecycleTaskInstance.Title is a snapshot.
public class LifecycleTaskService
{
    private readonly IDbContextFactory<HRMContext> _dbFactory;

    public LifecycleTaskService(IDbContextFactory<HRMContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public record LifecycleProgress(int Total, int Completed, int Skipped, int Pending);

    public async Task StartOnboardingAsync(long hremployeeId, long actorUserId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var emp = await context.Hremployee.FirstOrDefaultAsync(e => e.id == hremployeeId, ct)
            ?? throw new InvalidOperationException("ไม่พบพนักงาน");

        await StartCaseAsync(context, emp, LifecycleTaskDirection.Onboarding, actorUserId, ct);
    }

    public async Task StartOffboardingAsync(long hremployeeId, long actorUserId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var emp = await context.Hremployee.FirstOrDefaultAsync(e => e.id == hremployeeId, ct)
            ?? throw new InvalidOperationException("ไม่พบพนักงาน");

        if (emp.ResignDate is null)
            throw new InvalidOperationException("ต้องลงวันที่ลาออกก่อนจึงจะเริ่ม Offboarding ได้");

        await StartCaseAsync(context, emp, LifecycleTaskDirection.Offboarding, actorUserId, ct);
    }

    private static async Task StartCaseAsync(HRMContext context, Hremployee emp, LifecycleTaskDirection direction, long actorUserId, CancellationToken ct)
    {
        // Idempotent — starting twice must not duplicate tasks.
        var alreadyStarted = await context.Hrd_LifecycleTaskInstances
            .AnyAsync(t => t.HremployeeId == emp.id && t.Direction == direction, ct);
        if (alreadyStarted) return;

        var templates = await context.Hrd_LifecycleTaskTemplates
            .Where(t => t.IsActive && t.CompanyId == emp.companyid && t.Direction == direction)
            .OrderBy(t => t.SortOrder)
            .ToListAsync(ct);

        foreach (var template in templates)
        {
            context.Hrd_LifecycleTaskInstances.Add(new Hrd_LifecycleTaskInstance
            {
                HremployeeId = emp.id,
                Direction = direction,
                TemplateId = template.Id,
                Title = template.Title, // snapshot — later template edits don't retroactively change this
                Status = LifecycleTaskStatus.Pending,
                CreatedByUserId = actorUserId,
            });
        }

        await context.SaveChangesAsync(ct);
    }

    public async Task AddAdHocTaskAsync(long hremployeeId, LifecycleTaskDirection direction, string title, DateTime? dueDate, long actorUserId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new InvalidOperationException("กรุณาระบุชื่องาน");

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        context.Hrd_LifecycleTaskInstances.Add(new Hrd_LifecycleTaskInstance
        {
            HremployeeId = hremployeeId,
            Direction = direction,
            TemplateId = null,
            Title = title,
            Status = LifecycleTaskStatus.Pending,
            DueDate = dueDate,
            CreatedByUserId = actorUserId,
        });
        await context.SaveChangesAsync(ct);
    }

    public async Task CompleteTaskAsync(long taskId, long actorUserId, string? note, CancellationToken ct = default)
        => await SetTaskStatusAsync(taskId, LifecycleTaskStatus.Completed, actorUserId, note, ct);

    public async Task SkipTaskAsync(long taskId, long actorUserId, string? note, CancellationToken ct = default)
        => await SetTaskStatusAsync(taskId, LifecycleTaskStatus.Skipped, actorUserId, note, ct);

    public async Task ReopenTaskAsync(long taskId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var task = await context.Hrd_LifecycleTaskInstances.FirstOrDefaultAsync(t => t.Id == taskId, ct)
            ?? throw new InvalidOperationException("ไม่พบงานนี้");
        task.Status = LifecycleTaskStatus.Pending;
        task.CompletedDate = null;
        task.CompletedByUserId = null;
        await context.SaveChangesAsync(ct);
    }

    private async Task SetTaskStatusAsync(long taskId, LifecycleTaskStatus status, long actorUserId, string? note, CancellationToken ct)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var task = await context.Hrd_LifecycleTaskInstances.FirstOrDefaultAsync(t => t.Id == taskId, ct)
            ?? throw new InvalidOperationException("ไม่พบงานนี้");
        task.Status = status;
        task.CompletedDate = DateTime.Now;
        task.CompletedByUserId = actorUserId;
        if (!string.IsNullOrWhiteSpace(note)) task.Note = note;
        await context.SaveChangesAsync(ct);
    }

    public async Task<List<Hrd_LifecycleTaskInstance>> GetTasksAsync(long hremployeeId, LifecycleTaskDirection direction, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.Hrd_LifecycleTaskInstances
            .Where(t => t.HremployeeId == hremployeeId && t.Direction == direction)
            .OrderBy(t => t.Id)
            .ToListAsync(ct);
    }

    public async Task<LifecycleProgress> GetProgressAsync(long hremployeeId, LifecycleTaskDirection direction, CancellationToken ct = default)
    {
        var tasks = await GetTasksAsync(hremployeeId, direction, ct);
        return new LifecycleProgress(
            tasks.Count,
            tasks.Count(t => t.Status == LifecycleTaskStatus.Completed),
            tasks.Count(t => t.Status == LifecycleTaskStatus.Skipped),
            tasks.Count(t => t.Status == LifecycleTaskStatus.Pending));
    }

    // Pure suggestion, not persisted until ConfirmProbationAsync is called —
    // lets the UI show a default the HR user can still edit before saving.
    public static DateTime ResolveSuggestedProbationEndDate(DateTime workDate, int probationDays)
        => workDate.AddDays(probationDays);

    public async Task ConfirmProbationAsync(long hremployeeId, long actorUserId, DateTime endDate, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var emp = await context.Hremployee.FirstOrDefaultAsync(e => e.id == hremployeeId, ct)
            ?? throw new InvalidOperationException("ไม่พบพนักงาน");
        emp.ProbationEndDate = endDate;
        emp.ProbationConfirmedDate = DateTime.Now;
        await context.SaveChangesAsync(ct);
    }

    public async Task RecordExitInterviewAsync(long hremployeeId, long actorUserId, DateTime interviewDate,
        ExitReasonCode reasonCode, string? reasonNote, bool? wouldRecommend, string? feedback, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        context.Hrd_ExitInterviews.Add(new Hrd_ExitInterview
        {
            HremployeeId = hremployeeId,
            InterviewDate = interviewDate,
            ConductedByUserId = actorUserId,
            ReasonCode = reasonCode,
            ReasonNote = reasonNote,
            WouldRecommendCompany = wouldRecommend,
            Feedback = feedback,
            CreatedByUserId = actorUserId,
        });
        await context.SaveChangesAsync(ct);
    }

    public async Task<Hrd_ExitInterview?> GetLatestExitInterviewAsync(long hremployeeId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.Hrd_ExitInterviews
            .Where(i => i.HremployeeId == hremployeeId)
            .OrderByDescending(i => i.CreatedDate)
            .FirstOrDefaultAsync(ct);
    }
}
