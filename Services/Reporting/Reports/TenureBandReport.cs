using HRM.Models;
using HRM.Models.Reporting;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Reporting.Reports;

// Active headcount bucketed into length-of-service bands computed from WorkDate
// (hire date) as of today. Tenure math is done in memory after the query, and
// the bands are shown in a fixed display order. No parameters beyond company scope.
public class TenureBandReport(IDbContextFactory<HRMContext> dbFactory) : IReportDefinition
{
    public string Code => "headcount-by-tenure";
    public string Category => "กำลังพล (Headcount)";
    public string Name => "จำนวนพนักงานตามอายุงาน";
    public string? Description => "นับพนักงานที่ยังทำงานอยู่ แยกตามอายุงาน (ปี)";
    public IReadOnlyList<ReportParameter> Parameters => Array.Empty<ReportParameter>();

    private static readonly string[] BandOrder =
    {
        "น้อยกว่า 1 ปี", "1-3 ปี", "3-5 ปี", "5-10 ปี", "10 ปีขึ้นไป", "ไม่ระบุ",
    };

    public async Task<ReportResult> RunAsync(IReadOnlyDictionary<string, string?> args, ReportContext ctx, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var emps = await context.Hremployee
            .Where(e => e.companyid == ctx.CompanyId && e.ResignDate == null)
            .Select(e => e.WorkDate)
            .ToListAsync(ct);

        var today = DateTime.Today;

        static string Band(DateTime? workDate, DateTime asOf)
        {
            if (workDate == null) return "ไม่ระบุ";
            var years = (asOf - workDate.Value).TotalDays / 365.25;
            return years switch
            {
                < 1 => "น้อยกว่า 1 ปี",
                < 3 => "1-3 ปี",
                < 5 => "3-5 ปี",
                < 10 => "5-10 ปี",
                _ => "10 ปีขึ้นไป",
            };
        }

        var total = emps.Count;

        var counts = emps
            .GroupBy(w => Band(w, today))
            .ToDictionary(g => g.Key, g => g.Count());

        var rows = BandOrder
            .Where(band => counts.ContainsKey(band))
            .Select(band =>
            {
                var count = counts[band];
                return (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
                {
                    ["band"] = band,
                    ["count"] = count,
                    ["percent"] = total == 0 ? 0m : Math.Round(count * 100m / total, 1),
                };
            })
            .ToList();

        var totals = new Dictionary<string, object?>
        {
            ["band"] = "รวมทั้งหมด",
            ["count"] = total,
            ["percent"] = 100m,
        };

        return new ReportResult(
            "จำนวนพนักงานตามอายุงาน",
            new[]
            {
                new ReportColumn("band", "อายุงาน"),
                new ReportColumn("count", "จำนวน (คน)", ReportColumnType.Number),
                new ReportColumn("percent", "สัดส่วน", ReportColumnType.Percent),
            },
            rows, totals,
            Subtitle: $"บริษัท {ctx.CompanyId} · ณ {DateTime.Now:dd/MM/yyyy}");
    }
}
