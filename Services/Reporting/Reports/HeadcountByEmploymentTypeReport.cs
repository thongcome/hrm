using HRM.Models;
using HRM.Models.Reporting;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Reporting.Reports;

// Active headcount by employment type (EMPTYPE_CODE) — permanent / contract /
// daily etc. The type name is resolved from the payroll employee-type master
// when available, otherwise the raw code is shown.
public class HeadcountByEmploymentTypeReport(IDbContextFactory<HRMContext> dbFactory) : IReportDefinition
{
    public string Code => "headcount-by-employment-type";
    public string Category => "กำลังพล (Headcount)";
    public string Name => "จำนวนพนักงานตามประเภทการจ้าง";
    public string? Description => "นับพนักงานที่ยังทำงานอยู่ แยกตามประเภทการจ้าง (EMPTYPE)";
    public IReadOnlyList<ReportParameter> Parameters => ReportCriteria.Standard(empType: false, statusDefault: ReportCriteria.StatusWorking);

    public async Task<ReportResult> RunAsync(IReadOnlyDictionary<string, string?> args, ReportContext ctx, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var query = context.Hremployee.Where(e => e.companyid == ctx.CompanyId);
        query = await ReportCriteria.ApplyEmployeeAsync(context, query, args, ct);

        // ประเภทของพนักงานผ่าน Hremployee.EmployeeTypeId (FK → Pos_EmployeeType)
        var grouped = await query
            .GroupJoin(context.Pos_EmployeeTypes, e => e.EmployeeTypeId, t => (long?)t.Id, (e, ts) => new { e, ts })
            .SelectMany(x => x.ts.DefaultIfEmpty(), (x, t) => new { Code = t != null ? t.Code : null, Name = t != null ? t.Name : null })
            .GroupBy(x => new { x.Code, x.Name })
            .Select(g => new { g.Key.Code, g.Key.Name, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync(ct);

        var rows = grouped.Select(g => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
        {
            ["code"] = g.Code ?? "(ไม่ระบุ)",
            ["name"] = g.Name ?? "—",
            ["count"] = g.Count,
        }).ToList();

        var totals = new Dictionary<string, object?> { ["name"] = "รวมทั้งหมด", ["count"] = grouped.Sum(g => g.Count) };
        var crit = await ReportCriteria.DescribeAsync(context, ctx.CompanyId, args, ct);

        return new ReportResult(
            "จำนวนพนักงานตามประเภทการจ้าง",
            new[]
            {
                new ReportColumn("code", "รหัสประเภท"),
                new ReportColumn("name", "ประเภทการจ้าง"),
                new ReportColumn("count", "จำนวน (คน)", ReportColumnType.Number),
            },
            rows, totals, Subtitle: $"บริษัท {ctx.CompanyId} · ณ {DateTime.Now:dd/MM/yyyy}" + (crit is null ? "" : " · " + crit));
    }
}
