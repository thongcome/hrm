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
    public IReadOnlyList<ReportParameter> Parameters => Array.Empty<ReportParameter>();

    public async Task<ReportResult> RunAsync(IReadOnlyDictionary<string, string?> args, ReportContext ctx, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var emps = await context.Hremployee
            .Where(e => e.companyid == ctx.CompanyId && e.ResignDate == null)
            .Select(e => e.EmptypeCode)
            .ToListAsync(ct);

        var typeNames = await context.Pos_EmployeeTypes
            .Where(t => t.CompanyId == ctx.CompanyId)
            .Select(t => new { t.Code, t.Name })
            .ToListAsync(ct);
        var nameByCode = typeNames.Where(t => t.Code != null)
            .GroupBy(t => t.Code!).ToDictionary(g => g.Key, g => g.First().Name ?? g.Key, StringComparer.OrdinalIgnoreCase);

        var grouped = emps
            .GroupBy(c => c ?? "(ไม่ระบุ)")
            .Select(g => new { Code = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        var rows = grouped.Select(g => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
        {
            ["code"] = g.Code,
            ["name"] = nameByCode.TryGetValue(g.Code, out var n) ? n : "—",
            ["count"] = g.Count,
        }).ToList();

        var totals = new Dictionary<string, object?> { ["name"] = "รวมทั้งหมด", ["count"] = grouped.Sum(g => g.Count) };

        return new ReportResult(
            "จำนวนพนักงานตามประเภทการจ้าง",
            new[]
            {
                new ReportColumn("code", "รหัสประเภท"),
                new ReportColumn("name", "ประเภทการจ้าง"),
                new ReportColumn("count", "จำนวน (คน)", ReportColumnType.Number),
            },
            rows, totals, Subtitle: $"บริษัท {ctx.CompanyId} · ณ {DateTime.Now:dd/MM/yyyy}");
    }
}
