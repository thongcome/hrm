using HRM.Models;
using HRM.Models.Reporting;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Reporting.Reports;

// Training pipeline health: how many course enrollments sit in each status,
// company-wide. Lms_Enrollment carries no CompanyId of its own, so scoping is
// resolved through its session's course (Lms_CourseSession.CourseId →
// Lms_Course.CompanyId). Always shows all seven statuses in a fixed order.
public class TrainingCompletionReport(IDbContextFactory<HRMContext> dbFactory) : IReportDefinition, IReportDynamicOptions
{
    public string Code => "training-completion";
    public string Category => "ฝึกอบรม (Training / LMS)";
    public string Name => "สรุปสถานะการอบรม";
    public string? Description => "จำนวนการลงทะเบียนอบรมแยกตามสถานะ";
    public IReadOnlyList<ReportParameter> Parameters => new[]
    {
        new ReportParameter("from", "ลงทะเบียนตั้งแต่วันที่", ReportParamType.Date,
            HelperText: "เว้นว่าง = ไม่จำกัดวันที่ (กรองตามวันที่ลงทะเบียน)"),
        new ReportParameter("to", "ถึงวันที่", ReportParamType.Date),
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

    private static readonly (EnrollmentStatus Status, string Label)[] StatusOrder =
    {
        (EnrollmentStatus.PendingApproval, "รออนุมัติ"),
        (EnrollmentStatus.Approved, "อนุมัติแล้ว"),
        (EnrollmentStatus.Rejected, "ถูกปฏิเสธ"),
        (EnrollmentStatus.Attended, "เข้าอบรมแล้ว"),
        (EnrollmentStatus.Completed, "จบแล้ว"),
        (EnrollmentStatus.NoShow, "ไม่มาอบรม"),
        (EnrollmentStatus.Cancelled, "ยกเลิก"),
    };

    public async Task<ReportResult> RunAsync(IReadOnlyDictionary<string, string?> args, ReportContext ctx, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        // Company scope: courses of this company → their sessions → enrollments.
        var courseQuery = context.Lms_Courses.Where(c => c.CompanyId == ctx.CompanyId);
        if (long.TryParse(ReportCriteria.Arg(args, "course"), out var courseFilter))
            courseQuery = courseQuery.Where(c => c.Id == courseFilter);
        var courseIds = await courseQuery
            .Select(c => c.Id)
            .ToListAsync(ct);

        var sessionIds = await context.Lms_CourseSessions
            .Where(s => courseIds.Contains(s.CourseId))
            .Select(s => s.Id)
            .ToListAsync(ct);

        var enrollments = context.Lms_Enrollments.Where(e => sessionIds.Contains(e.CourseSessionId));
        if (DateTime.TryParse(ReportCriteria.Arg(args, "from"), out var from))
            enrollments = enrollments.Where(e => e.EnrolledDate >= from.Date);
        if (DateTime.TryParse(ReportCriteria.Arg(args, "to"), out var to))
        {
            var toEnd = to.Date.AddDays(1);
            enrollments = enrollments.Where(e => e.EnrolledDate < toEnd);
        }
        if (ReportCriteria.Arg(args, ReportCriteria.DeptKey) is not null || ReportCriteria.Arg(args, ReportCriteria.EmpTypeKey) is not null)
        {
            var emps = await ReportCriteria.ApplyEmployeeAsync(context,
                context.Hremployee.Where(e => e.companyid == ctx.CompanyId), args, ct);
            var empIds = emps.Select(e => e.id);
            enrollments = enrollments.Where(e => empIds.Contains(e.HremployeeId));
        }
        var crit = await ReportCriteria.DescribeAsync(context, ctx.CompanyId, args, ct);
        var dateNote = ReportCriteria.Arg(args, "from") is null && ReportCriteria.Arg(args, "to") is null ? null
            : $"ลงทะเบียน {ReportCriteria.Arg(args, "from") ?? "…"} ถึง {ReportCriteria.Arg(args, "to") ?? "…"}";

        var statuses = await enrollments
            .Select(e => e.Status)
            .ToListAsync(ct);

        var total = statuses.Count;
        var countByStatus = statuses.GroupBy(s => s).ToDictionary(g => g.Key, g => g.Count());

        var rows = new List<IReadOnlyDictionary<string, object?>>();
        foreach (var (status, label) in StatusOrder)
        {
            var count = countByStatus.TryGetValue(status, out var c) ? c : 0;
            var pct = total == 0 ? 0m : Math.Round(count * 100m / total, 1);
            rows.Add(new Dictionary<string, object?>
            {
                ["status"] = label,
                ["count"] = count,
                ["pct"] = pct,
            });
        }

        var totals = new Dictionary<string, object?>
        {
            ["status"] = "รวม",
            ["count"] = total,
            ["pct"] = 100m,
        };

        return new ReportResult(
            "สรุปสถานะการอบรม",
            new[]
            {
                new ReportColumn("status", "สถานะ"),
                new ReportColumn("count", "จำนวน", ReportColumnType.Number),
                new ReportColumn("pct", "สัดส่วน", ReportColumnType.Percent),
            },
            rows, totals,
            Subtitle: $"บริษัท {ctx.CompanyId} · การลงทะเบียนทั้งหมด {total} รายการ · ณ {DateTime.Now:dd/MM/yyyy}"
                + (dateNote is null ? "" : $" · {dateNote}")
                + (crit is null ? "" : $" · {crit}"));
    }
}
