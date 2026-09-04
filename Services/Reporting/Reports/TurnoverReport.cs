using HRM.Models;
using HRM.Models.Reporting;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Reporting.Reports;

// Resignations per month for a year — the turnover view the AutoX list asks
// for. Counts Hremployee rows whose ResignDate falls in the selected year.
public class TurnoverReport(IDbContextFactory<HRMContext> dbFactory) : IReportDefinition
{
    public string Code => "turnover-by-month";
    public string Category => "กำลังพล (Headcount)";
    public string Name => "อัตราการลาออกรายเดือน (Turnover)";
    public string? Description => "จำนวนพนักงานที่ลาออก แยกตามเดือน ในปีที่เลือก";

    public IReadOnlyList<ReportParameter> Parameters => new[]
    {
        new ReportParameter("year", "ปี (ค.ศ.)", ReportParamType.Year, Required: true,
            DefaultValue: DateTime.Now.Year.ToString(), HelperText: "เช่น 2026"),
    };

    public async Task<ReportResult> RunAsync(IReadOnlyDictionary<string, string?> args, ReportContext ctx, CancellationToken ct = default)
    {
        var year = ParseYear(args);
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var resigns = await context.Hremployee
            .Where(e => e.companyid == ctx.CompanyId && e.ResignDate != null
                && e.ResignDate!.Value.Year == year)
            .Select(e => e.ResignDate!.Value.Month)
            .ToListAsync(ct);

        var byMonth = resigns.GroupBy(m => m).ToDictionary(g => g.Key, g => g.Count());
        var rows = new List<IReadOnlyDictionary<string, object?>>();
        for (var m = 1; m <= 12; m++)
        {
            rows.Add(new Dictionary<string, object?>
            {
                ["month"] = ThaiMonth(m),
                ["count"] = byMonth.TryGetValue(m, out var c) ? c : 0,
            });
        }
        var totals = new Dictionary<string, object?> { ["month"] = "รวมทั้งปี", ["count"] = resigns.Count };

        return new ReportResult(
            $"อัตราการลาออกรายเดือน — ปี {year}",
            new[] { new ReportColumn("month", "เดือน"), new ReportColumn("count", "จำนวนลาออก (คน)", ReportColumnType.Number) },
            rows, totals, Subtitle: $"บริษัท {ctx.CompanyId}");
    }

    internal static int ParseYear(IReadOnlyDictionary<string, string?> args)
        => args.TryGetValue("year", out var y) && int.TryParse(y, out var yr) ? yr : DateTime.Now.Year;

    internal static string ThaiMonth(int m) => new[]
    {
        "", "มกราคม", "กุมภาพันธ์", "มีนาคม", "เมษายน", "พฤษภาคม", "มิถุนายน",
        "กรกฎาคม", "สิงหาคม", "กันยายน", "ตุลาคม", "พฤศจิกายน", "ธันวาคม",
    }[m];
}
