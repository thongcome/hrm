using HRM.Models;
using HRM.Models.Reporting;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Reporting.Reports;

// Enrollment volume per course, with a completed-count column. Enrollments link
// to a course only through their session (Lms_CourseSession.CourseId), so the
// three tables are projected and grouped in memory by course. Company scope is
// applied on Lms_Course.CompanyId (Lms_Enrollment has no CompanyId of its own).
public class TrainingByCourseReport(IDbContextFactory<HRMContext> dbFactory) : IReportDefinition
{
    public string Code => "training-by-course";
    public string Category => "ฝึกอบรม (Training / LMS)";
    public string Name => "จำนวนผู้เข้าอบรมตามหลักสูตร";
    public string? Description => "จำนวนผู้ลงทะเบียนและผู้ที่อบรมจบ แยกตามหลักสูตร";
    public IReadOnlyList<ReportParameter> Parameters => Array.Empty<ReportParameter>();

    public async Task<ReportResult> RunAsync(IReadOnlyDictionary<string, string?> args, ReportContext ctx, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        // Company scope: courses of this company + their title lookup.
        var courses = await context.Lms_Courses
            .Where(c => c.CompanyId == ctx.CompanyId)
            .Select(c => new { c.Id, c.Title })
            .ToListAsync(ct);
        var titleByCourse = courses.ToDictionary(c => c.Id, c => c.Title);
        var courseIds = courses.Select(c => c.Id).ToList();

        // Session → course map, scoped to this company's courses.
        var sessions = await context.Lms_CourseSessions
            .Where(s => courseIds.Contains(s.CourseId))
            .Select(s => new { s.Id, s.CourseId })
            .ToListAsync(ct);
        var courseBySession = sessions.ToDictionary(s => s.Id, s => s.CourseId);
        var sessionIds = sessions.Select(s => s.Id).ToList();

        var enrollments = await context.Lms_Enrollments
            .Where(e => sessionIds.Contains(e.CourseSessionId))
            .Select(e => new { e.CourseSessionId, e.Status })
            .ToListAsync(ct);

        var grouped = enrollments
            .Where(e => courseBySession.ContainsKey(e.CourseSessionId))
            .GroupBy(e => courseBySession[e.CourseSessionId])
            .Select(g => new
            {
                CourseId = g.Key,
                Enrolled = g.Count(),
                Completed = g.Count(e => e.Status == EnrollmentStatus.Completed),
            })
            .OrderByDescending(x => x.Enrolled)
            .ToList();

        var rows = grouped.Select(g => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
        {
            ["course"] = titleByCourse.TryGetValue(g.CourseId, out var t) ? t : "—",
            ["enrolled"] = g.Enrolled,
            ["completed"] = g.Completed,
        }).ToList();

        var totals = new Dictionary<string, object?>
        {
            ["course"] = "รวมทั้งหมด",
            ["enrolled"] = grouped.Sum(g => g.Enrolled),
            ["completed"] = grouped.Sum(g => g.Completed),
        };

        return new ReportResult(
            "จำนวนผู้เข้าอบรมตามหลักสูตร",
            new[]
            {
                new ReportColumn("course", "หลักสูตร"),
                new ReportColumn("enrolled", "ผู้ลงทะเบียน", ReportColumnType.Number),
                new ReportColumn("completed", "จบแล้ว", ReportColumnType.Number),
            },
            rows, totals,
            Subtitle: $"บริษัท {ctx.CompanyId} · ณ {DateTime.Now:dd/MM/yyyy}");
    }
}
