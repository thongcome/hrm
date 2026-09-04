using HRM.Models;
using HRM.Models.Reporting;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Reporting.Reports;

// Total approved leave days by leave type for a year — the leave slice of the
// standard report set. Counts only requests that entered the workflow
// (JobMasterId != null) so drafts don't inflate the numbers.
public class LeaveUsageSummaryReport(IDbContextFactory<HRMContext> dbFactory) : IReportDefinition
{
    public string Code => "leave-usage-summary";
    public string Category => "การลา (Leave)";
    public string Name => "สรุปการใช้วันลาตามประเภท";
    public string? Description => "รวมจำนวนวันลาและจำนวนคำขอ แยกตามประเภทการลา ในปีที่เลือก";

    public IReadOnlyList<ReportParameter> Parameters => new[]
    {
        new ReportParameter("year", "ปี (ค.ศ.)", ReportParamType.Year, Required: true,
            DefaultValue: DateTime.Now.Year.ToString(), HelperText: "อ้างอิงจากวันเริ่มลา"),
    };

    public async Task<ReportResult> RunAsync(IReadOnlyDictionary<string, string?> args, ReportContext ctx, CancellationToken ct = default)
    {
        var year = TurnoverReport.ParseYear(args);
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var reqs = await context.Lve_LeaveRequests
            .Where(r => r.CompanyId == ctx.CompanyId && r.JobMasterId != null && r.StartDate.Year == year)
            .Select(r => new { r.LeaveTypeId, r.TotalDays })
            .ToListAsync(ct);

        var types = await context.Lve_LeaveTypes.ToDictionaryAsync(t => t.Id, t => t.NameTh, ct);

        var grouped = reqs
            .GroupBy(r => r.LeaveTypeId)
            .Select(g => new { TypeId = g.Key, Days = g.Sum(x => x.TotalDays), Count = g.Count() })
            .OrderByDescending(x => x.Days)
            .ToList();

        var rows = grouped.Select(g => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
        {
            ["type"] = types.TryGetValue(g.TypeId, out var n) ? n : $"#{g.TypeId}",
            ["count"] = g.Count,
            ["days"] = g.Days,
        }).ToList();

        var totals = new Dictionary<string, object?>
        {
            ["type"] = "รวมทั้งหมด",
            ["count"] = grouped.Sum(g => g.Count),
            ["days"] = grouped.Sum(g => g.Days),
        };

        return new ReportResult(
            $"สรุปการใช้วันลาตามประเภท — ปี {year}",
            new[]
            {
                new ReportColumn("type", "ประเภทการลา"),
                new ReportColumn("count", "จำนวนคำขอ", ReportColumnType.Number),
                new ReportColumn("days", "รวมวันลา", ReportColumnType.Number),
            },
            rows, totals, Subtitle: $"บริษัท {ctx.CompanyId} · เฉพาะคำขอที่เข้าสายอนุมัติแล้ว");
    }
}
