using HRM.Models;
using HRM.Models.Reporting;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Reporting.Reports;

// Overtime totals per month for a year, from the payroll OT rows (HrwOt).
// Sums the OT amount and OT minutes (shown as hours) and counts the rows.
public class OvertimeSummaryReport(IDbContextFactory<HRMContext> dbFactory) : IReportDefinition
{
    private static readonly string[] ThaiMonths =
        { "มกราคม","กุมภาพันธ์","มีนาคม","เมษายน","พฤษภาคม","มิถุนายน","กรกฎาคม","สิงหาคม","กันยายน","ตุลาคม","พฤศจิกายน","ธันวาคม" };

    public string Code => "ot-summary";
    public string Category => "เวลาทำงาน & OT (Time & OT)";
    public string Name => "สรุปการทำงานล่วงเวลา (รายเดือน)";
    public string? Description => "จำนวนรายการ ชั่วโมง และเงินค่าล่วงเวลา แยกตามเดือน ในปีที่เลือก";

    public IReadOnlyList<ReportParameter> Parameters => new[]
    {
        new ReportParameter("year", "ปี (ค.ศ.)", ReportParamType.Year, Required: true,
            DefaultValue: DateTime.Today.Year.ToString(), HelperText: "ปีปฏิทินที่ต้องการสรุป"),
    };

    public async Task<ReportResult> RunAsync(IReadOnlyDictionary<string, string?> args, ReportContext ctx, CancellationToken ct = default)
    {
        var year = args.TryGetValue("year", out var y) && int.TryParse(y, out var yy) ? yy : DateTime.Today.Year;

        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var rows = await context.HrwOts
            .Where(o => o.companyid == ctx.CompanyId && o.DateWork != null && o.DateWork!.Value.Year == year)
            .Select(o => new { o.DateWork, o.OtAmt, o.OtPMinute })
            .ToListAsync(ct);

        var byMonth = rows
            .GroupBy(o => o.DateWork!.Value.Month)
            .Select(g => new
            {
                Month = g.Key,
                Count = g.Count(),
                Minutes = g.Sum(x => x.OtPMinute ?? 0m),
                Amount = g.Sum(x => x.OtAmt ?? 0m),
            })
            .OrderBy(x => x.Month)
            .ToList();

        var reportRows = byMonth.Select(m => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
        {
            ["month"] = ThaiMonths[m.Month - 1],
            ["count"] = m.Count,
            ["hours"] = Math.Round(m.Minutes / 60m, 1),
            ["amount"] = m.Amount,
        }).ToList();

        var totals = new Dictionary<string, object?>
        {
            ["month"] = "รวมทั้งปี",
            ["count"] = byMonth.Sum(m => m.Count),
            ["hours"] = Math.Round(byMonth.Sum(m => m.Minutes) / 60m, 1),
            ["amount"] = byMonth.Sum(m => m.Amount),
        };

        return new ReportResult(
            $"สรุปการทำงานล่วงเวลา — ปี {year}",
            new[]
            {
                new ReportColumn("month", "เดือน"),
                new ReportColumn("count", "จำนวนรายการ", ReportColumnType.Number),
                new ReportColumn("hours", "ชั่วโมง OT รวม", ReportColumnType.Number),
                new ReportColumn("amount", "เงิน OT รวม", ReportColumnType.Money),
            },
            reportRows, totals,
            Subtitle: $"บริษัท {ctx.CompanyId}");
    }
}
