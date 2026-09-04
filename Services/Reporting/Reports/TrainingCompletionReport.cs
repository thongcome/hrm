using HRM.Models;
using HRM.Models.Reporting;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Reporting.Reports;

// Training pipeline health: how many course enrollments sit in each status,
// company-wide. Lms_Enrollment carries no CompanyId of its own, so scoping is
// resolved through its session's course (Lms_CourseSession.CourseId →
// Lms_Course.CompanyId). Always shows all seven statuses in a fixed order.
public class TrainingCompletionReport(IDbContextFactory<HRMContext> dbFactory) : IReportDefinition
{
    public string Code => "training-completion";
    public string Category => "ฝึกอบรม (Training / LMS)";
    public string Name => "สรุปสถานะการอบรม";
    public string? Description => "จำนวนการลงทะเบียนอบรมแยกตามสถานะ";
    public IReadOnlyList<ReportParameter> Parameters => Array.Empty<ReportParameter>();

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
        var courseIds = await context.Lms_Courses
            .Where(c => c.CompanyId == ctx.CompanyId)
            .Select(c => c.Id)
            .ToListAsync(ct);

        var sessionIds = await context.Lms_CourseSessions
            .Where(s => courseIds.Contains(s.CourseId))
            .Select(s => s.Id)
            .ToListAsync(ct);

        var statuses = await context.Lms_Enrollments
            .Where(e => sessionIds.Contains(e.CourseSessionId))
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
            Subtitle: $"บริษัท {ctx.CompanyId} · การลงทะเบียนทั้งหมด {total} รายการ · ณ {DateTime.Now:dd/MM/yyyy}");
    }
}
