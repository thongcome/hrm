namespace HRM.Services.Lms;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Admin CRUD for Lms_CourseRequirement rules, plus the read-side queries
// that turn Lms_MandatoryAssignment + Lms_Enrollment into a per-employee or
// per-company compliance picture. See LmsMandatoryTrainingHelper for the
// write-side sync that actually creates assignment rows.
public class LmsMandatoryTrainingService(IDbContextFactory<HRMContext> dbFactory)
{
    public enum MandatoryStatus { NotEnrolled, Enrolled, Completed }

    public record RequirementRow(Lms_CourseRequirement Requirement, string CourseTitle, string? PositionName);
    public record AssignmentStatusRow(long HremployeeId, long CourseId, string CourseTitle, MandatoryStatus Status, DateTime AssignedDate);
    public record ComplianceRow(long HremployeeId, string EmpNo, string EmployeeName, long CourseId, string CourseTitle, MandatoryStatus Status, DateTime AssignedDate);

    public async Task<List<RequirementRow>> GetRequirementsAsync(string companyId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var reqs = await context.Lms_CourseRequirements.Where(r => r.CompanyId == companyId && r.IsActive)
            .OrderByDescending(r => r.CreatedDate).ToListAsync(ct);

        var courseIds = reqs.Select(r => r.CourseId).Distinct().ToList();
        var courses = await context.Lms_Courses.Where(c => courseIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, ct);

        var posIds = reqs.Where(r => r.PosExecTypeId is not null).Select(r => r.PosExecTypeId!.Value).Distinct().ToList();
        var positions = await context.Pos_ExecTypes.Where(p => posIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);

        return reqs.Select(r => new RequirementRow(
            r,
            courses.TryGetValue(r.CourseId, out var c) ? c.Title : $"#{r.CourseId}",
            r.PosExecTypeId is long posId && positions.TryGetValue(posId, out var p) ? p.Name : null
        )).ToList();
    }

    public async Task AddRequirementAsync(string companyId, long courseId, long? posExecTypeId, long actorUserId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var duplicate = await context.Lms_CourseRequirements.AnyAsync(r => r.CompanyId == companyId && r.IsActive
            && r.CourseId == courseId && r.PosExecTypeId == posExecTypeId, ct);
        if (duplicate)
            throw new InvalidOperationException("มีกฎนี้ (หลักสูตร + ตำแหน่ง) อยู่แล้ว");

        context.Lms_CourseRequirements.Add(new Lms_CourseRequirement
        {
            CompanyId = companyId,
            CourseId = courseId,
            PosExecTypeId = posExecTypeId,
            CreatedByUserId = actorUserId,
        });
        await context.SaveChangesAsync(ct);
    }

    public async Task RemoveRequirementAsync(long requirementId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var req = await context.Lms_CourseRequirements.FirstOrDefaultAsync(r => r.Id == requirementId, ct);
        if (req is null) return;
        req.IsActive = false;
        await context.SaveChangesAsync(ct);
    }

    public async Task<List<AssignmentStatusRow>> GetStatusForEmployeeAsync(long hremployeeId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var assignments = await context.Lms_MandatoryAssignments
            .Where(a => a.HremployeeId == hremployeeId && a.IsActive)
            .ToListAsync(ct);
        if (assignments.Count == 0) return new();

        return await BuildStatusRowsAsync(context, assignments, ct);
    }

    public async Task<List<ComplianceRow>> GetComplianceReportAsync(string companyId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var empIds = await context.Hremployee.Where(e => e.companyid == companyId).Select(e => e.id).ToListAsync(ct);
        var assignments = await context.Lms_MandatoryAssignments
            .Where(a => a.IsActive && empIds.Contains(a.HremployeeId))
            .ToListAsync(ct);
        if (assignments.Count == 0) return new();

        var statusRows = await BuildStatusRowsAsync(context, assignments, ct);

        var empLookup = await context.Hremployee.Where(e => empIds.Contains(e.id))
            .ToDictionaryAsync(e => e.id, e => (e.EmpNo, Name: $"{e.EmpName} {e.EmpSurname}"), ct);

        return statusRows.Select(s =>
        {
            empLookup.TryGetValue(s.HremployeeId, out var emp);
            return new ComplianceRow(s.HremployeeId, emp.EmpNo ?? "-", emp.Name ?? "-", s.CourseId, s.CourseTitle, s.Status, s.AssignedDate);
        }).OrderBy(r => r.Status).ThenBy(r => r.EmpNo).ToList();
    }

    // Shared status computation: an assignment is Completed if the employee
    // has a Completed enrollment in ANY session of that course, Enrolled if
    // they have any non-terminal enrollment (PendingApproval/Approved/
    // Attended), else NotEnrolled. Deliberately doesn't care WHICH session —
    // this tracks "did they take the course", not "are they in session X".
    private static async Task<List<AssignmentStatusRow>> BuildStatusRowsAsync(
        HRMContext context, List<Lms_MandatoryAssignment> assignments, CancellationToken ct)
    {
        var courseIds = assignments.Select(a => a.CourseId).Distinct().ToList();
        var courses = await context.Lms_Courses.Where(c => courseIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, ct);

        var employeeIds = assignments.Select(a => a.HremployeeId).Distinct().ToList();
        var sessionsByCourse = await context.Lms_CourseSessions.Where(s => courseIds.Contains(s.CourseId))
            .ToListAsync(ct);
        var sessionIdToCourseId = sessionsByCourse.ToDictionary(s => s.Id, s => s.CourseId);
        var relevantSessionIds = sessionsByCourse.Select(s => s.Id).ToList();

        var enrollments = await context.Lms_Enrollments
            .Where(e => employeeIds.Contains(e.HremployeeId) && relevantSessionIds.Contains(e.CourseSessionId))
            .ToListAsync(ct);

        var rows = new List<AssignmentStatusRow>();
        foreach (var a in assignments)
        {
            var myEnrollments = enrollments.Where(e => e.HremployeeId == a.HremployeeId
                && sessionIdToCourseId.TryGetValue(e.CourseSessionId, out var cid) && cid == a.CourseId).ToList();

            var status = myEnrollments.Any(e => e.Status == EnrollmentStatus.Completed) ? MandatoryStatus.Completed
                : myEnrollments.Any(e => e.Status is EnrollmentStatus.PendingApproval or EnrollmentStatus.Approved or EnrollmentStatus.Attended) ? MandatoryStatus.Enrolled
                : MandatoryStatus.NotEnrolled;

            rows.Add(new AssignmentStatusRow(a.HremployeeId, a.CourseId, courses.TryGetValue(a.CourseId, out var c) ? c.Title : $"#{a.CourseId}", status, a.AssignedDate));
        }
        return rows;
    }
}
