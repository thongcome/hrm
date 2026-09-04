using HRM.Models;
using HRM.Models.Reporting;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Reporting.Reports;

// Active headcount grouped by department/organization unit — the org-chart
// slice of the report set. No parameters beyond the implicit company scope.
public class HeadcountByDeptReport(IDbContextFactory<HRMContext> dbFactory) : IReportDefinition
{
    public string Code => "headcount-by-dept";
    public string Category => "กำลังพล (Headcount)";
    public string Name => "จำนวนพนักงานตามหน่วยงาน";
    public string? Description => "นับพนักงานที่ยังทำงานอยู่ แยกตามหน่วยงาน (DEPTGRP)";
    public IReadOnlyList<ReportParameter> Parameters => Array.Empty<ReportParameter>();

    public async Task<ReportResult> RunAsync(IReadOnlyDictionary<string, string?> args, ReportContext ctx, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var emps = await context.Hremployee
            .Where(e => e.companyid == ctx.CompanyId && e.ResignDate == null)
            .Select(e => new { e.DeptgrpCode })
            .ToListAsync(ct);

        var orgs = await context.com_organizations
            .Select(o => new { o.code, o.name })
            .ToListAsync(ct);
        var orgName = orgs.Where(o => o.code != null)
            .GroupBy(o => o.code!).ToDictionary(g => g.Key, g => g.First().name ?? g.Key, StringComparer.OrdinalIgnoreCase);

        var grouped = emps
            .GroupBy(e => e.DeptgrpCode ?? "(ไม่ระบุ)")
            .Select(g => new { Code = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        var rows = grouped.Select(g => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
        {
            ["code"] = g.Code,
            ["name"] = orgName.TryGetValue(g.Code, out var n) ? n : "—",
            ["count"] = g.Count,
        }).ToList();

        var totals = new Dictionary<string, object?> { ["name"] = "รวมทั้งหมด", ["count"] = grouped.Sum(g => g.Count) };

        return new ReportResult(
            "จำนวนพนักงานตามหน่วยงาน",
            new[]
            {
                new ReportColumn("code", "รหัสหน่วยงาน"),
                new ReportColumn("name", "หน่วยงาน"),
                new ReportColumn("count", "จำนวน (คน)", ReportColumnType.Number),
            },
            rows, totals,
            Subtitle: $"บริษัท {ctx.CompanyId} · ณ {DateTime.Now:dd/MM/yyyy}");
    }
}
