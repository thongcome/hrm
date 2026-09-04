using HRM.Models;
using HRM.Models.Reporting;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Reporting.Reports;

// Counts of attendance exceptions (late / early-leave / absent) for a chosen
// month, from Att_DailyAttendance's boolean flags.
public class AttendanceExceptionReport(IDbContextFactory<HRMContext> dbFactory) : IReportDefinition
{
    private static readonly string[] ThaiMonths =
        { "มกราคม","กุมภาพันธ์","มีนาคม","เมษายน","พฤษภาคม","มิถุนายน","กรกฎาคม","สิงหาคม","กันยายน","ตุลาคม","พฤศจิกายน","ธันวาคม" };

    public string Code => "attendance-exceptions";
    public string Category => "เวลาทำงาน & OT (Time & OT)";
    public string Name => "สรุปเวลาเข้างานผิดปกติ (รายเดือน)";
    public string? Description => "จำนวนครั้งมาสาย / ออกก่อน / ขาดงาน ในเดือนที่เลือก";

    public IReadOnlyList<ReportParameter> Parameters => new[]
    {
        new ReportParameter("year", "ปี (ค.ศ.)", ReportParamType.Year, Required: true, DefaultValue: DateTime.Today.Year.ToString()),
        new ReportParameter("month", "เดือน", ReportParamType.Select, Required: true,
            DefaultValue: DateTime.Today.Month.ToString(),
            Options: Enumerable.Range(1, 12).Select(i => new ReportParamOption(i.ToString(), ThaiMonths[i - 1])).ToList()),
    };

    public async Task<ReportResult> RunAsync(IReadOnlyDictionary<string, string?> args, ReportContext ctx, CancellationToken ct = default)
    {
        var year = args.TryGetValue("year", out var y) && int.TryParse(y, out var yy) ? yy : DateTime.Today.Year;
        var month = args.TryGetValue("month", out var m) && int.TryParse(m, out var mm) ? mm : DateTime.Today.Month;

        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var recs = await context.Att_DailyAttendances
            .Where(a => a.CompanyId == ctx.CompanyId && a.WorkDate.Year == year && a.WorkDate.Month == month)
            .Select(a => new { a.IsLate, a.IsEarlyLeave, a.IsAbsent })
            .ToListAsync(ct);

        var late = recs.Count(a => a.IsLate);
        var early = recs.Count(a => a.IsEarlyLeave);
        var absent = recs.Count(a => a.IsAbsent);

        var rows = new List<IReadOnlyDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["type"] = "มาสาย", ["count"] = late },
            new Dictionary<string, object?> { ["type"] = "ออกก่อนเวลา", ["count"] = early },
            new Dictionary<string, object?> { ["type"] = "ขาดงาน", ["count"] = absent },
        };

        var totals = new Dictionary<string, object?> { ["type"] = "รวมเหตุผิดปกติ", ["count"] = late + early + absent };

        return new ReportResult(
            $"สรุปเวลาเข้างานผิดปกติ — {ThaiMonths[month - 1]} {year}",
            new[]
            {
                new ReportColumn("type", "ประเภท"),
                new ReportColumn("count", "จำนวน (ครั้ง)", ReportColumnType.Number),
            },
            rows, totals,
            Subtitle: $"บริษัท {ctx.CompanyId} · จากบันทึกเวลาทำงาน {recs.Count} รายการในเดือนนี้");
    }
}
