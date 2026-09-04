using HRM.Models;
using HRM.Models.Reporting;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Reporting.Reports;

// Active headcount grouped by gender (Sex) — the demographic slice of the
// report set. No parameters beyond the implicit company scope.
public class HeadcountByGenderReport(IDbContextFactory<HRMContext> dbFactory) : IReportDefinition
{
    public string Code => "headcount-by-gender";
    public string Category => "กำลังพล (Headcount)";
    public string Name => "จำนวนพนักงานตามเพศ";
    public string? Description => "นับพนักงานที่ยังทำงานอยู่ แยกตามเพศ";
    public IReadOnlyList<ReportParameter> Parameters => Array.Empty<ReportParameter>();

    public async Task<ReportResult> RunAsync(IReadOnlyDictionary<string, string?> args, ReportContext ctx, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var emps = await context.Hremployee
            .Where(e => e.companyid == ctx.CompanyId && e.ResignDate == null)
            .Select(e => e.Sex)
            .ToListAsync(ct);

        static string Label(string? sex) => sex switch
        {
            "M" => "ชาย",
            "F" => "หญิง",
            _ => "ไม่ระบุ",
        };

        var total = emps.Count;

        var grouped = emps
            .GroupBy(Label)
            .Select(g => new { Label = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        var rows = grouped.Select(g => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
        {
            ["sex"] = g.Label,
            ["count"] = g.Count,
            ["percent"] = total == 0 ? 0m : Math.Round(g.Count * 100m / total, 1),
        }).ToList();

        var totals = new Dictionary<string, object?>
        {
            ["sex"] = "รวมทั้งหมด",
            ["count"] = total,
            ["percent"] = 100m,
        };

        return new ReportResult(
            "จำนวนพนักงานตามเพศ",
            new[]
            {
                new ReportColumn("sex", "เพศ"),
                new ReportColumn("count", "จำนวน (คน)", ReportColumnType.Number),
                new ReportColumn("percent", "สัดส่วน", ReportColumnType.Percent),
            },
            rows, totals,
            Subtitle: $"บริษัท {ctx.CompanyId} · ณ {DateTime.Now:dd/MM/yyyy}");
    }
}
