using HRM.Models;
using HRM.Models.Reporting;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Reporting.Reports;

// Active headcount by position (POS_CODE). The position name is resolved from the
// position master (pos_positions) when available, otherwise a dash is shown.
public class HeadcountByPositionReport(IDbContextFactory<HRMContext> dbFactory) : IReportDefinition
{
    public string Code => "headcount-by-position";
    public string Category => "กำลังพล (Headcount)";
    public string Name => "จำนวนพนักงานตามตำแหน่ง";
    public string? Description => "นับพนักงานที่ยังทำงานอยู่ แยกตามตำแหน่ง (POS)";
    public IReadOnlyList<ReportParameter> Parameters => Array.Empty<ReportParameter>();

    public async Task<ReportResult> RunAsync(IReadOnlyDictionary<string, string?> args, ReportContext ctx, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var emps = await context.Hremployee
            .Where(e => e.companyid == ctx.CompanyId && e.ResignDate == null)
            .Select(e => e.PosCode)
            .ToListAsync(ct);

        var posNames = await context.pos_positions
            .Select(p => new { p.pos_code, p.name })
            .ToListAsync(ct);
        var nameByCode = posNames.Where(p => p.pos_code != null)
            .GroupBy(p => p.pos_code!).ToDictionary(g => g.Key, g => g.First().name ?? g.Key, StringComparer.OrdinalIgnoreCase);

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
            "จำนวนพนักงานตามตำแหน่ง",
            new[]
            {
                new ReportColumn("code", "รหัสตำแหน่ง"),
                new ReportColumn("name", "ตำแหน่ง"),
                new ReportColumn("count", "จำนวน (คน)", ReportColumnType.Number),
            },
            rows, totals, Subtitle: $"บริษัท {ctx.CompanyId} · ณ {DateTime.Now:dd/MM/yyyy}");
    }
}
