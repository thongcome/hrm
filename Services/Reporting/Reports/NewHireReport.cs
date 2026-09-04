using HRM.Models;
using HRM.Models.Reporting;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Reporting.Reports;

// New hires per month for a year, by WORK_DATE (start date). Pairs with the
// turnover report to give a hire-vs-leave picture.
public class NewHireReport(IDbContextFactory<HRMContext> dbFactory) : IReportDefinition
{
    public string Code => "new-hires-by-month";
    public string Category => "กำลังพล (Headcount)";
    public string Name => "พนักงานเข้าใหม่รายเดือน (New Hires)";
    public string? Description => "จำนวนพนักงานที่เริ่มงาน แยกตามเดือน ในปีที่เลือก";

    public IReadOnlyList<ReportParameter> Parameters => new[]
    {
        new ReportParameter("year", "ปี (ค.ศ.)", ReportParamType.Year, Required: true,
            DefaultValue: DateTime.Now.Year.ToString(), HelperText: "เช่น 2026"),
    };

    public async Task<ReportResult> RunAsync(IReadOnlyDictionary<string, string?> args, ReportContext ctx, CancellationToken ct = default)
    {
        var year = TurnoverReport.ParseYear(args);
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var hires = await context.Hremployee
            .Where(e => e.companyid == ctx.CompanyId && e.WorkDate != null && e.WorkDate!.Value.Year == year)
            .Select(e => e.WorkDate!.Value.Month)
            .ToListAsync(ct);

        var byMonth = hires.GroupBy(m => m).ToDictionary(g => g.Key, g => g.Count());
        var rows = new List<IReadOnlyDictionary<string, object?>>();
        for (var m = 1; m <= 12; m++)
        {
            rows.Add(new Dictionary<string, object?>
            {
                ["month"] = TurnoverReport.ThaiMonth(m),
                ["count"] = byMonth.TryGetValue(m, out var c) ? c : 0,
            });
        }
        var totals = new Dictionary<string, object?> { ["month"] = "รวมทั้งปี", ["count"] = hires.Count };

        return new ReportResult(
            $"พนักงานเข้าใหม่รายเดือน — ปี {year}",
            new[] { new ReportColumn("month", "เดือน"), new ReportColumn("count", "จำนวนเข้าใหม่ (คน)", ReportColumnType.Number) },
            rows, totals, Subtitle: $"บริษัท {ctx.CompanyId}");
    }
}
