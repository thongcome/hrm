namespace HRM.Services.Lms;

using HRM.Models;
using HRM.Services.Workflow;
using Microsoft.EntityFrameworkCore;

// Enrollment lifecycle for one course session — mirrors IdpPlanService.cs /
// PerfApprovalService.cs exactly for the approval half (StartJobAsync +
// lazy SyncStatusFromJobAsync on read, no scheduler exists anywhere in this
// codebase). Courses that don't require approval skip the workflow engine
// entirely and go straight to Approved.
public class LmsEnrollmentService(IDbContextFactory<HRMContext> dbFactory, WorkflowEngineService engine)
{
    public async Task<long> EnrollAsync(long courseSessionId, long hremployeeId, long requestedByUserId, string? requesterEmpId, long? sourceDevelopmentActionId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var session = await context.Lms_CourseSessions.FirstOrDefaultAsync(s => s.Id == courseSessionId, ct)
            ?? throw new InvalidOperationException("ไม่พบรอบอบรมนี้แล้ว");
        var course = await context.Lms_Courses.FirstOrDefaultAsync(c => c.Id == session.CourseId, ct)
            ?? throw new InvalidOperationException("ไม่พบหลักสูตรของรอบอบรมนี้");

        var alreadyEnrolled = await context.Lms_Enrollments.AnyAsync(e => e.CourseSessionId == courseSessionId
            && e.HremployeeId == hremployeeId
            && e.Status != EnrollmentStatus.Rejected && e.Status != EnrollmentStatus.Cancelled, ct);
        if (alreadyEnrolled)
            throw new InvalidOperationException("ลงทะเบียนรอบอบรมนี้ไว้แล้ว");

        if (session.MaxSeats is int maxSeats)
        {
            var takenSeats = await context.Lms_Enrollments.CountAsync(e => e.CourseSessionId == courseSessionId
                && e.Status != EnrollmentStatus.Rejected && e.Status != EnrollmentStatus.Cancelled, ct);
            if (takenSeats >= maxSeats)
                throw new InvalidOperationException("ที่นั่งเต็มแล้ว");
        }

        var enrollment = new Lms_Enrollment
        {
            CourseSessionId = courseSessionId,
            HremployeeId = hremployeeId,
            RequestedByUserId = requestedByUserId,
            SourceDevelopmentActionId = sourceDevelopmentActionId,
            Status = course.RequiresApproval ? EnrollmentStatus.PendingApproval : EnrollmentStatus.Approved,
        };
        if (!course.RequiresApproval)
            enrollment.ApprovedDate = DateTime.Now;

        context.Lms_Enrollments.Add(enrollment);
        await context.SaveChangesAsync(ct);

        if (course.RequiresApproval)
        {
            var workflow = await context.wf_workflows.FirstOrDefaultAsync(w => w.workflowcode == "LMS_TRAINING_APPROVAL", ct)
                ?? throw new InvalidOperationException("ไม่พบ workflow 'LMS_TRAINING_APPROVAL' — ติดต่อแอดมินให้ตั้งค่าก่อน");
            if (workflow.isactive != true)
                throw new InvalidOperationException($"workflow '{workflow.wname}' ปิดใช้งานอยู่ ไม่สามารถส่งอนุมัติได้");

            var employee = await context.Hremployee.FirstOrDefaultAsync(e => e.id == hremployeeId, ct);
            var subject = $"ลงทะเบียนอบรม: {course.Title} — {employee?.EmpName} {employee?.EmpSurname}";
            var jobId = await engine.StartJobAsync(workflow.workflowid, "Lms_Enrollment", enrollment.Id.ToString(),
                requestedByUserId, requesterEmpId, subject, amount: null, ct);

            enrollment.JobMasterId = jobId;
            await context.SaveChangesAsync(ct);
        }

        return enrollment.Id;
    }

    // Lazy apply-on-read — no-op unless the enrollment is still
    // PendingApproval with a job that has since closed.
    public async Task SyncStatusFromJobAsync(long enrollmentId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var enrollment = await context.Lms_Enrollments.FirstOrDefaultAsync(e => e.Id == enrollmentId, ct);
        if (enrollment is null || enrollment.Status != EnrollmentStatus.PendingApproval || enrollment.JobMasterId is null)
            return;

        var job = await context.job_masters.FirstOrDefaultAsync(j => j.jobmasterid == enrollment.JobMasterId, ct);
        if (job is null || job.isJobClosed != true)
            return;

        enrollment.Status = job.status == WorkflowEngineService.StatusCompleted
            ? EnrollmentStatus.Approved
            : EnrollmentStatus.Rejected;
        if (enrollment.Status == EnrollmentStatus.Approved)
            enrollment.ApprovedDate = DateTime.Now;

        await context.SaveChangesAsync(ct);
    }

    public async Task MarkAttendedAsync(long enrollmentId, long actorUserId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var enrollment = await context.Lms_Enrollments.FirstOrDefaultAsync(e => e.Id == enrollmentId, ct)
            ?? throw new InvalidOperationException("ไม่พบการลงทะเบียนนี้แล้ว");
        if (enrollment.Status != EnrollmentStatus.Approved)
            throw new InvalidOperationException("ทำเครื่องหมายเข้าร่วมได้เฉพาะรายการที่อนุมัติแล้วเท่านั้น");

        enrollment.Status = EnrollmentStatus.Attended;
        enrollment.AttendedDate = DateTime.Now;
        await context.SaveChangesAsync(ct);
    }

    public async Task CancelAsync(long enrollmentId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var enrollment = await context.Lms_Enrollments.FirstOrDefaultAsync(e => e.Id == enrollmentId, ct)
            ?? throw new InvalidOperationException("ไม่พบการลงทะเบียนนี้แล้ว");
        if (enrollment.Status == EnrollmentStatus.Completed)
            throw new InvalidOperationException("ไม่สามารถยกเลิกรายการที่สำเร็จหลักสูตรแล้ว");

        enrollment.Status = EnrollmentStatus.Cancelled;
        await context.SaveChangesAsync(ct);
    }

    // Blocks marking a course "completed" when the course has a quiz gate
    // (Course.PassingScorePercent set) and no passing attempt exists yet —
    // prevents issuing a certificate to someone who failed or never took
    // the quiz.
    public async Task MarkCompletedAsync(long enrollmentId, long actorUserId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var enrollment = await context.Lms_Enrollments.FirstOrDefaultAsync(e => e.Id == enrollmentId, ct)
            ?? throw new InvalidOperationException("ไม่พบการลงทะเบียนนี้แล้ว");
        if (enrollment.Status != EnrollmentStatus.Attended)
            throw new InvalidOperationException("ทำเครื่องหมายสำเร็จหลักสูตรได้เฉพาะผู้ที่เข้าร่วมแล้วเท่านั้น");

        var session = await context.Lms_CourseSessions.FirstOrDefaultAsync(s => s.Id == enrollment.CourseSessionId, ct);
        var course = session is null ? null : await context.Lms_Courses.FirstOrDefaultAsync(c => c.Id == session.CourseId, ct);

        if (course?.PassingScorePercent is int passingScore)
        {
            var hasPassed = await context.Lms_QuizAttempts.AnyAsync(a => a.EnrollmentId == enrollmentId && a.IsPassed, ct);
            if (!hasPassed)
                throw new InvalidOperationException($"หลักสูตรนี้กำหนดเกณฑ์ผ่านแบบทดสอบ {passingScore}% — ต้องทำแบบทดสอบให้ผ่านก่อนจึงจะทำเครื่องหมายสำเร็จหลักสูตรได้");
        }

        enrollment.Status = EnrollmentStatus.Completed;
        enrollment.CompletedDate = DateTime.Now;
        await context.SaveChangesAsync(ct);
    }
}
