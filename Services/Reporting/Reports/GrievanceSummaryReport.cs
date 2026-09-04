using HRM.Models;
using HRM.Models.Reporting;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Reporting.Reports;

// Employee grievances by status for a year — how many submitted, under
// investigation, resolved, dismissed.
public class GrievanceSummaryReport(IDbContextFactory<HRMContext> dbFactory) : IReportDefinition
{
    public string Code => "grievance-summary";
    public string Category => "แรงงานสัมพันธ์ (Employee Relations)";
    public string Name => "สรุปเรื่องร้องเรียน (รายปี)";
    public string? Description => "จำนวนเรื่องร้องเรียน แยกตามสถานะการดำเนินการ ในปีที่เลือก";

    public IReadOnlyList<ReportParameter> Parameters => new[]
    {
        new ReportParameter("year", "ปี (ค.ศ.)", ReportParamType.Year, Required: true, DefaultValue: DateTime.Today.Year.ToString()),
    };

    private static string StatusLabel(GrievanceStatus s) => s switch
    {
        GrievanceStatus.Submitted => "ยื่นเรื่อง",
        GrievanceStatus.UnderInvestigation => "อยู่ระหว่างสอบสวน",
        GrievanceStatus.Resolved => "แก้ไขแล้ว",
        GrievanceStatus.Dismissed => "ยกคำร้อง",
        _ => s.ToString(),
    };

    public async Task<ReportResult> RunAsync(IReadOnlyDictionary<string, string?> args, ReportContext ctx, CancellationToken ct = default)
    {
        var year = args.TryGetValue("year", out var y) && int.TryParse(y, out var yy) ? yy : DateTime.Today.Year;

        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var items = await context.Hr_Grievances
            .Where(g => g.CompanyId == ctx.CompanyId && g.CreatedDate.Year == year)
            .Select(g => g.Status)
            .ToListAsync(ct);

        var counts = items.GroupBy(s => s).ToDictionary(g => g.Key, g => g.Count());
        var order = new[] { GrievanceStatus.Submitted, GrievanceStatus.UnderInvestigation, GrievanceStatus.Resolved, GrievanceStatus.Dismissed };

        var rows = order.Select(s => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
        {
            ["status"] = StatusLabel(s),
            ["count"] = counts.TryGetValue(s, out var c) ? c : 0,
        }).ToList();

        var totals = new Dictionary<string, object?> { ["status"] = "รวมทั้งหมด", ["count"] = items.Count };

        return new ReportResult(
            $"สรุปเรื่องร้องเรียน — ปี {year}",
            new[]
            {
                new ReportColumn("status", "สถานะ"),
                new ReportColumn("count", "จำนวน (เรื่อง)", ReportColumnType.Number),
            },
            rows, totals,
            Subtitle: $"บริษัท {ctx.CompanyId}");
    }
}
