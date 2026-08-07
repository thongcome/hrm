namespace HRM.Services.Att;

using HRM.Models;
using HRM.Services.Workflow;
using Microsoft.EntityFrameworkCore;

// Weekly project-timesheet CRUD + submission into the generic Workflow
// Engine, mirroring Services/Idp/IdpPlanService.cs's SubmitForApprovalAsync /
// SyncStatusFromJobAsync pair exactly (no scheduler exists anywhere in this
// codebase, so status is only ever pulled from the job on read).
public class TimesheetService(IDbContextFactory<HRMContext> dbFactory, WorkflowEngineService engine)
{
    public static DateOnly WeekStartFor(DateOnly anyDate)
    {
        // Monday-start week, matching the "รายสัปดาห์" (weekly) scope decided for this feature.
        var diff = ((int)anyDate.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return anyDate.AddDays(-diff);
    }

    public async Task<Att_TimesheetSubmission> GetOrCreateDraftAsync(long hremployeeId, string companyId, DateOnly weekStart, long actorUserId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var existing = await context.Att_TimesheetSubmissions
            .FirstOrDefaultAsync(s => s.HremployeeId == hremployeeId && s.WeekStartDate == weekStart, ct);
        if (existing is not null)
            return existing;

        var submission = new Att_TimesheetSubmission
        {
            CompanyId = companyId,
            HremployeeId = hremployeeId,
            WeekStartDate = weekStart,
            WeekEndDate = weekStart.AddDays(6),
            Status = Att_TimesheetStatus.Draft,
            CreatedByUserId = actorUserId,
            CreatedDate = DateTime.Now,
        };
        context.Att_TimesheetSubmissions.Add(submission);
        await context.SaveChangesAsync(ct);
        return submission;
    }

    public async Task AddEntryAsync(long submissionId, long projectId, DateOnly workDate, decimal hours, string? note, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var submission = await context.Att_TimesheetSubmissions.FirstOrDefaultAsync(s => s.Id == submissionId, ct)
            ?? throw new InvalidOperationException("ไม่พบ timesheet นี้แล้ว");
        if (submission.Status != Att_TimesheetStatus.Draft)
            throw new InvalidOperationException("แก้ไขรายการได้เฉพาะฉบับร่าง (Draft) เท่านั้น");
        if (workDate < submission.WeekStartDate || workDate > submission.WeekEndDate)
            throw new InvalidOperationException("วันที่ต้องอยู่ในสัปดาห์ของ timesheet ฉบับนี้");
        if (hours <= 0 || hours > 24)
            throw new InvalidOperationException("จำนวนชั่วโมงต้องมากกว่า 0 และไม่เกิน 24 ต่อวัน");

        context.Att_TimesheetEntries.Add(new Att_TimesheetEntry
        {
            SubmissionId = submissionId,
            ProjectId = projectId,
            WorkDate = workDate,
            Hours = hours,
            Note = note,
        });
        await context.SaveChangesAsync(ct);
    }

    public async Task RemoveEntryAsync(long entryId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var entry = await context.Att_TimesheetEntries.Include(e => e.Submission).FirstOrDefaultAsync(e => e.Id == entryId, ct);
        if (entry is null)
            return;
        if (entry.Submission.Status != Att_TimesheetStatus.Draft)
            throw new InvalidOperationException("แก้ไขรายการได้เฉพาะฉบับร่าง (Draft) เท่านั้น");

        context.Att_TimesheetEntries.Remove(entry);
        await context.SaveChangesAsync(ct);
    }

    public async Task<long> SubmitForApprovalAsync(long submissionId, long requesterUserId, string? requesterEmpId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var submission = await context.Att_TimesheetSubmissions.Include(s => s.Entries)
            .FirstOrDefaultAsync(s => s.Id == submissionId, ct)
            ?? throw new InvalidOperationException("ไม่พบ timesheet นี้แล้ว");
        if (submission.Status != Att_TimesheetStatus.Draft)
            throw new InvalidOperationException("timesheet ฉบับนี้ถูกส่งไปแล้ว");
        if (submission.Entries.Count == 0)
            throw new InvalidOperationException("กรุณาเพิ่มรายการอย่างน้อย 1 รายการก่อนส่งอนุมัติ");

        var workflow = await context.wf_workflows.FirstOrDefaultAsync(w => w.workflowcode == "TIMESHEET_APPROVAL", ct)
            ?? throw new InvalidOperationException("ไม่พบ workflow 'TIMESHEET_APPROVAL' — ติดต่อแอดมินให้ตั้งค่าก่อน");
        if (workflow.isactive != true)
            throw new InvalidOperationException($"workflow '{workflow.wname}' ปิดใช้งานอยู่ ไม่สามารถส่งอนุมัติได้");

        var employee = await context.Hremployee.FirstOrDefaultAsync(e => e.id == submission.HremployeeId, ct);
        var subject = $"Timesheet {submission.WeekStartDate:dd/MM/yyyy} - {submission.WeekEndDate:dd/MM/yyyy}: {employee?.EmpName} {employee?.EmpSurname}";

        submission.TotalHours = submission.Entries.Sum(e => e.Hours);

        var jobId = await engine.StartJobAsync(workflow.workflowid, "Att_TimesheetSubmission", submissionId.ToString(),
            requesterUserId, requesterEmpId, subject, amount: null, ct);

        submission.JobMasterId = jobId;
        submission.Status = Att_TimesheetStatus.PendingApproval;
        submission.SubmittedDate = DateTime.Now;
        await context.SaveChangesAsync(ct);

        return jobId;
    }

    // Lazy apply-on-read — called from the timesheet detail page on every
    // load. No-op unless the submission is still PendingApproval with a job
    // that has since closed.
    public async Task SyncStatusFromJobAsync(long submissionId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var submission = await context.Att_TimesheetSubmissions.FirstOrDefaultAsync(s => s.Id == submissionId, ct);
        if (submission is null || submission.Status != Att_TimesheetStatus.PendingApproval || submission.JobMasterId is null)
            return;

        var job = await context.job_masters.FirstOrDefaultAsync(j => j.jobmasterid == submission.JobMasterId, ct);
        if (job is null || job.isJobClosed != true)
            return;

        submission.Status = job.status == WorkflowEngineService.StatusCompleted
            ? Att_TimesheetStatus.Approved
            : Att_TimesheetStatus.Rejected;
        if (submission.Status == Att_TimesheetStatus.Approved)
            submission.ApprovedDate = DateTime.Now;

        await context.SaveChangesAsync(ct);
    }

    public async Task<List<Att_TimesheetSubmission>> GetMySubmissionsAsync(long hremployeeId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        return await context.Att_TimesheetSubmissions
            .Where(s => s.HremployeeId == hremployeeId)
            .OrderByDescending(s => s.WeekStartDate)
            .ToListAsync(ct);
    }

    public async Task<(Att_TimesheetSubmission Submission, List<Att_TimesheetEntry> Entries)?> GetDetailAsync(long submissionId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var submission = await context.Att_TimesheetSubmissions.FirstOrDefaultAsync(s => s.Id == submissionId, ct);
        if (submission is null)
            return null;

        var entries = await context.Att_TimesheetEntries
            .Include(e => e.Project)
            .Where(e => e.SubmissionId == submissionId)
            .OrderBy(e => e.WorkDate)
            .ToListAsync(ct);

        return (submission, entries);
    }

    public async Task<List<Att_Project>> GetActiveProjectsAsync(string companyId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        return await context.Att_Projects.Where(p => p.CompanyId == companyId && p.IsActive).OrderBy(p => p.Name).ToListAsync(ct);
    }
}
