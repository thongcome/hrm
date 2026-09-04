using HRM.Models;
using HRM.Models.Reporting;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Reporting.Reports;

// Salary summary (avg / min / max / total) by organization unit for active
// employees that have a salary on file. The unit name is resolved from the
// organization master (com_organizations) when available.
public class SalaryByDeptReport(IDbContextFactory<HRMContext> dbFactory) : IReportDefinition
{
    public string Code => "salary-by-dept";
    public string Category => "เงินเดือน / GL (Payroll)";
    public string Name => "สรุปเงินเดือนตามหน่วยงาน";
    public string? Description => "ค่าเฉลี่ย/ต่ำสุด/สูงสุด/รวม เงินเดือน แยกตามหน่วยงาน (เฉพาะที่มีข้อมูลเงินเดือน)";
    public IReadOnlyList<ReportParameter> Parameters => Array.Empty<ReportParameter>();

    public async Task<ReportResult> RunAsync(IReadOnlyDictionary<string, string?> args, ReportContext ctx, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var emps = await context.Hremployee
            .Where(e => e.companyid == ctx.CompanyId && e.ResignDate == null && e.SalaryAmt != null)
            .Select(e => new { e.DeptgrpCode, e.SalaryAmt })
            .ToListAsync(ct);

        var deptNames = await context.com_organizations
            .Select(o => new { o.code, o.name })
            .ToListAsync(ct);
        var nameByCode = deptNames.Where(o => o.code != null)
            .GroupBy(o => o.code!).ToDictionary(g => g.Key, g => g.First().name ?? g.Key, StringComparer.OrdinalIgnoreCase);

        var grouped = emps
            .GroupBy(e => e.DeptgrpCode ?? "(ไม่ระบุ)")
            .Select(g => new
            {
                Code = g.Key,
                Count = g.Count(),
                Sum = g.Sum(x => x.SalaryAmt!.Value),
                Avg = Math.Round(g.Average(x => x.SalaryAmt!.Value), 2),
                Min = g.Min(x => x.SalaryAmt!.Value),
                Max = g.Max(x => x.SalaryAmt!.Value),
            })
            .OrderByDescending(x => x.Sum)
            .ToList();

        var rows = grouped.Select(g => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
        {
            ["code"] = g.Code,
            ["name"] = nameByCode.TryGetValue(g.Code, out var n) ? n : "—",
            ["count"] = g.Count,
            ["sum"] = g.Sum,
            ["avg"] = g.Avg,
            ["min"] = g.Min,
            ["max"] = g.Max,
        }).ToList();

        var totalCount = emps.Count;
        var totalSum = emps.Sum(e => e.SalaryAmt!.Value);
        var totals = new Dictionary<string, object?>
        {
            ["name"] = "รวมทั้งหมด",
            ["count"] = totalCount,
            ["sum"] = totalSum,
            ["avg"] = totalCount == 0 ? 0m : Math.Round(totalSum / totalCount, 2),
        };

        return new ReportResult(
            "สรุปเงินเดือนตามหน่วยงาน",
            new[]
            {
                new ReportColumn("code", "รหัสหน่วยงาน"),
                new ReportColumn("name", "หน่วยงาน"),
                new ReportColumn("count", "จำนวน", ReportColumnType.Number),
                new ReportColumn("sum", "รวมเงินเดือน", ReportColumnType.Money),
                new ReportColumn("avg", "เฉลี่ย", ReportColumnType.Money),
                new ReportColumn("min", "ต่ำสุด", ReportColumnType.Money),
                new ReportColumn("max", "สูงสุด", ReportColumnType.Money),
            },
            rows, totals, Subtitle: $"บริษัท {ctx.CompanyId} · ณ {DateTime.Now:dd/MM/yyyy}");
    }
}
