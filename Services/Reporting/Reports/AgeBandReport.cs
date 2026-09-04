using HRM.Models;
using HRM.Models.Reporting;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Reporting.Reports;

// Active headcount bucketed into age bands computed from BirthDate as of today.
// Age math is done in memory after the query, and the bands are shown in a
// fixed display order rather than by count. No parameters beyond company scope.
public class AgeBandReport(IDbContextFactory<HRMContext> dbFactory) : IReportDefinition
{
    public string Code => "headcount-by-age-band";
    public string Category => "กำลังพล (Headcount)";
    public string Name => "จำนวนพนักงานตามช่วงอายุ";
    public string? Description => "นับพนักงานที่ยังทำงานอยู่ แยกตามช่วงอายุ";
    public IReadOnlyList<ReportParameter> Parameters => Array.Empty<ReportParameter>();

    private static readonly string[] BandOrder =
    {
        "ต่ำกว่า 25 ปี", "25-34 ปี", "35-44 ปี", "45-54 ปี", "55 ปีขึ้นไป", "ไม่ระบุ",
    };

    public async Task<ReportResult> RunAsync(IReadOnlyDictionary<string, string?> args, ReportContext ctx, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var emps = await context.Hremployee
            .Where(e => e.companyid == ctx.CompanyId && e.ResignDate == null)
            .Select(e => e.BirthDate)
            .ToListAsync(ct);

        var today = DateTime.Today;

        static int AgeYears(DateTime birth, DateTime asOf)
        {
            var age = asOf.Year - birth.Year;
            if (asOf.Month < birth.Month || (asOf.Month == birth.Month && asOf.Day < birth.Day))
                age--;
            return age;
        }

        static string Band(DateTime? birth, DateTime asOf)
        {
            if (birth == null) return "ไม่ระบุ";
            var age = AgeYears(birth.Value, asOf);
            return age switch
            {
                < 25 => "ต่ำกว่า 25 ปี",
                <= 34 => "25-34 ปี",
                <= 44 => "35-44 ปี",
                <= 54 => "45-54 ปี",
                _ => "55 ปีขึ้นไป",
            };
        }

        var total = emps.Count;

        var counts = emps
            .GroupBy(b => Band(b, today))
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
            "จำนวนพนักงานตามช่วงอายุ",
            new[]
            {
                new ReportColumn("band", "ช่วงอายุ"),
                new ReportColumn("count", "จำนวน (คน)", ReportColumnType.Number),
                new ReportColumn("percent", "สัดส่วน", ReportColumnType.Percent),
            },
            rows, totals,
            Subtitle: $"บริษัท {ctx.CompanyId} · ณ {DateTime.Now:dd/MM/yyyy}");
    }
}
