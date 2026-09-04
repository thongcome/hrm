using HRM.Models;
using HRM.Models.Reporting;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Reporting.Reports;

// Expense claims per month for a year — count and total amount. Claim approval
// status lives on the workflow job (read lazily), so this counts all claims
// filed; it's a volume/spend view, not an approved-only figure.
public class ExpenseClaimSummaryReport(IDbContextFactory<HRMContext> dbFactory) : IReportDefinition
{
    private static readonly string[] ThaiMonths =
        { "มกราคม","กุมภาพันธ์","มีนาคม","เมษายน","พฤษภาคม","มิถุนายน","กรกฎาคม","สิงหาคม","กันยายน","ตุลาคม","พฤศจิกายน","ธันวาคม" };

    public string Code => "expense-claim-summary";
    public string Category => "เบิกจ่าย (Expense & Claims)";
    public string Name => "สรุปการเบิกค่าใช้จ่าย (รายเดือน)";
    public string? Description => "จำนวนใบเบิกและยอดเงินรวม แยกตามเดือน ในปีที่เลือก";

    public IReadOnlyList<ReportParameter> Parameters => new[]
    {
        new ReportParameter("year", "ปี (ค.ศ.)", ReportParamType.Year, Required: true, DefaultValue: DateTime.Today.Year.ToString()),
    };

    public async Task<ReportResult> RunAsync(IReadOnlyDictionary<string, string?> args, ReportContext ctx, CancellationToken ct = default)
    {
        var year = args.TryGetValue("year", out var y) && int.TryParse(y, out var yy) ? yy : DateTime.Today.Year;

        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var claims = await context.Exp_ClaimHeaders
            .Where(c => c.CompanyId == ctx.CompanyId && c.RequestedDate.Year == year)
            .Select(c => new { c.RequestedDate, c.TotalAmount })
            .ToListAsync(ct);

        var byMonth = claims.GroupBy(c => c.RequestedDate.Month)
            .Select(g => new { Month = g.Key, Count = g.Count(), Amount = g.Sum(x => x.TotalAmount) })
            .OrderBy(x => x.Month)
            .ToList();

        var rows = byMonth.Select(m => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
        {
            ["month"] = ThaiMonths[m.Month - 1],
            ["count"] = m.Count,
            ["amount"] = m.Amount,
        }).ToList();

        var totals = new Dictionary<string, object?>
        {
            ["month"] = "รวมทั้งปี",
            ["count"] = byMonth.Sum(m => m.Count),
            ["amount"] = byMonth.Sum(m => m.Amount),
        };

        return new ReportResult(
            $"สรุปการเบิกค่าใช้จ่าย — ปี {year}",
            new[]
            {
                new ReportColumn("month", "เดือน"),
                new ReportColumn("count", "จำนวนใบเบิก", ReportColumnType.Number),
                new ReportColumn("amount", "ยอดเบิกรวม", ReportColumnType.Money),
            },
            rows, totals,
            Subtitle: $"บริษัท {ctx.CompanyId}");
    }
}
