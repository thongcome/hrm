using HRM.Models;
using HRM.Models.Reporting;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Reporting.Reports;

// Welfare claims (Wel_Claim) for a chosen year, grouped by benefit type
// (Wel_BenefitType) with count + total amount. The type name is resolved from
// the welfare catalog via a code→name lookup dictionary, same pattern as
// HeadcountByEmploymentTypeReport.
//
// Wel_Claim has NO status column of its own — the live approval status is read
// back from job_master (JobMasterId) via the lazy apply-on-read pattern, so
// there is no approved/paid flag to filter on here. This report therefore
// counts ALL claims whose EventDate falls in the selected year (submitted
// drafts included), which is noted in the subtitle.
public class WelfareClaimSummaryReport(IDbContextFactory<HRMContext> dbFactory) : IReportDefinition
{
    public string Code => "welfare-claim-summary";
    public string Category => "สวัสดิการ (Welfare)";
    public string Name => "สรุปการเบิกสวัสดิการตามประเภท";
    public string? Description => "จำนวนและยอดเงินการเบิกสวัสดิการ แยกตามประเภท ในปีที่เลือก";

    public IReadOnlyList<ReportParameter> Parameters => new[]
    {
        new ReportParameter("year", "ปี (ค.ศ.)", ReportParamType.Year, Required: true,
            DefaultValue: DateTime.Now.Year.ToString()),
    };

    public async Task<ReportResult> RunAsync(IReadOnlyDictionary<string, string?> args, ReportContext ctx, CancellationToken ct = default)
    {
        var year = args.TryGetValue("year", out var yStr) && int.TryParse(yStr, out var y)
            ? y : DateTime.Now.Year;

        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var claims = await context.Wel_Claims
            .Where(c => c.CompanyId == ctx.CompanyId && c.EventDate.Year == year)
            .Select(c => new { c.BenefitTypeId, c.Amount })
            .ToListAsync(ct);

        var typeNames = await context.Wel_BenefitTypes
            .Where(t => t.CompanyId == ctx.CompanyId)
            .Select(t => new { t.Id, t.NameTh })
            .ToListAsync(ct);
        var nameById = typeNames
            .GroupBy(t => t.Id).ToDictionary(g => g.Key, g => g.First().NameTh ?? "—");

        var grouped = claims
            .GroupBy(c => c.BenefitTypeId)
            .Select(g => new
            {
                Name = nameById.TryGetValue(g.Key, out var n) ? n : "(ไม่ระบุประเภท)",
                Count = g.Count(),
                Amount = g.Sum(x => x.Amount),
            })
            .OrderByDescending(x => x.Amount)
            .ToList();

        var rows = grouped.Select(g => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
        {
            ["type"] = g.Name,
            ["count"] = g.Count,
            ["amount"] = g.Amount,
        }).ToList();

        var totals = new Dictionary<string, object?>
        {
            ["type"] = "รวมทั้งหมด",
            ["count"] = grouped.Sum(g => g.Count),
            ["amount"] = grouped.Sum(g => g.Amount),
        };

        return new ReportResult(
            $"สรุปการเบิกสวัสดิการตามประเภท — ปี {year}",
            new[]
            {
                new ReportColumn("type", "ประเภทสวัสดิการ"),
                new ReportColumn("count", "จำนวนรายการ", ReportColumnType.Number),
                new ReportColumn("amount", "ยอดเบิกรวม", ReportColumnType.Money),
            },
            rows, totals,
            Subtitle: $"บริษัท {ctx.CompanyId} · นับทุกรายการ (ไม่มีคอลัมน์สถานะใน Wel_Claim — สถานะอนุมัติอ่านจาก job_master) · ณ {DateTime.Now:dd/MM/yyyy}");
    }
}
