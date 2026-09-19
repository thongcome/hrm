using HRM.Models;
using HRM.Models.Reporting;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Reporting.Reports;

// Enrollment volume per course, with a completed-count column. Enrollments link
// to a course only through their session (Lms_CourseSession.CourseId), so the
// three tables are projected and grouped in memory by course. Company scope is
// applied on Lms_Course.CompanyId (Lms_Enrollment has no CompanyId of its own).
public class TrainingByCourseReport(IDbContextFactory<HRMContext> dbFactory) : IReportDefinition, IReportDynamicOptions
{
    public string Code => "training-by-course";
    public string Category => "ฝึกอบรม (Training / LMS)";
    public string Name => "จำนวนผู้เข้าอบรมตามหลักสูตร";
    public string? Description => "จำนวนผู้ลงทะเบียนและผู้ที่อบรมจบ แยกตามหลักสูตร";
    public IReadOnlyList<ReportParameter> Parameters => new[]
    {
        new ReportParameter("from", "ลงทะเบียนตั้งแต่วันที่", ReportParamType.Date,
            HelperText: "เว้นว่าง = ไม่จำกัดวันที่ (กรองตามวันที่ลงทะเบียน)"),
        new ReportParameter("to", "ถึงวันที่", ReportParamType.Date),
        new ReportParameter("delivery", "รูปแบบการอบรม", ReportParamType.Select,
            HelperText: "เว้นว่าง = ทุกรูปแบบ", Options: new[]
            {
                new ReportParamOption("", "ทั้งหมด"),
                new ReportParamOption(((int)CourseDeliveryType.Classroom).ToString(), "ห้องเรียน"),
                new ReportParamOption(((int)CourseDeliveryType.Online).ToString(), "ออนไลน์"),
                new ReportParamOption(((int)CourseDeliveryType.Hybrid).ToString(), "ผสม (Hybrid)"),
            }),
        new ReportParameter("course", "หลักสูตร", ReportParamType.Select,
            HelperText: "เว้นว่าง = ทุกหลักสูตร"),
    }.Concat(ReportCriteria.Standard()).ToList();

    public async Task<IReadOnlyList<ReportParamOption>> GetOptionsAsync(string parameterKey, ReportContext ctx, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        if (parameterKey != "course") return await ReportCriteria.OptionsAsync(context, parameterKey, ctx, ct);
        var courses = await context.Lms_Courses
            .Where(c => c.CompanyId == ctx.CompanyId)
            .OrderBy(c => c.Code)
            .Select(c => new ReportParamOption(c.Id.ToString(), c.Code + " — " + c.Title))
            .ToListAsync(ct);
        return new[] { new ReportParamOption("", "ทั้งหมด") }.Concat(courses).ToList();
    }

    public async Task<ReportResult> RunAsync(IReadOnlyDictionary<string, string?> args, ReportContext ctx, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        // Company scope: courses of this company + their title lookup.
        var courseQuery = context.Lms_Courses.Where(c => c.CompanyId == ctx.CompanyId);
        if (long.TryParse(ReportCriteria.Arg(args, "course"), out var courseFilter))
            courseQuery = courseQuery.Where(c => c.Id == courseFilter);
        if (int.TryParse(ReportCriteria.Arg(args, "delivery"), out var deliveryFilter))
        {
            var delivery = (CourseDeliveryType)deliveryFilter;
            courseQuery = courseQuery.Where(c => c.DeliveryType == delivery);
        }
        var courses = await courseQuery
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

        var enrollmentQuery = context.Lms_Enrollments.Where(e => sessionIds.Contains(e.CourseSessionId));
        if (DateTime.TryParse(ReportCriteria.Arg(args, "from"), out var from))
            enrollmentQuery = enrollmentQuery.Where(e => e.EnrolledDate >= from.Date);
        if (DateTime.TryParse(ReportCriteria.Arg(args, "to"), out var to))
        {
            var toEnd = to.Date.AddDays(1);
            enrollmentQuery = enrollmentQuery.Where(e => e.EnrolledDate < toEnd);
        }
        if (ReportCriteria.Arg(args, ReportCriteria.DeptKey) is not null || ReportCriteria.Arg(args, ReportCriteria.EmpTypeKey) is not null)
        {
            var emps = await ReportCriteria.ApplyEmployeeAsync(context,
                context.Hremployee.Where(e => e.companyid == ctx.CompanyId), args, ct);
            var empIds = emps.Select(e => e.id);
            enrollmentQuery = enrollmentQuery.Where(e => empIds.Contains(e.HremployeeId));
        }
        var crit = await ReportCriteria.DescribeAsync(context, ctx.CompanyId, args, ct);
        var dateNote = ReportCriteria.Arg(args, "from") is null && ReportCriteria.Arg(args, "to") is null ? null
            : $"ลงทะเบียน {ReportCriteria.Arg(args, "from") ?? "…"} ถึง {ReportCriteria.Arg(args, "to") ?? "…"}";

        var enrollments = await enrollmentQuery
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
            Subtitle: $"บริษัท {ctx.CompanyId} · ณ {DateTime.Now:dd/MM/yyyy}"
                + (dateNote is null ? "" : $" · {dateNote}")
                + (crit is null ? "" : $" · {crit}"));
    }
}
