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
    public IReadOnlyList<ReportParameter> Parameters => ReportCriteria.Standard(statusDefault: ReportCriteria.StatusWorking);

    public async Task<ReportResult> RunAsync(IReadOnlyDictionary<string, string?> args, ReportContext ctx, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var query = context.Hremployee.Where(e => e.companyid == ctx.CompanyId);
        query = await ReportCriteria.ApplyEmployeeAsync(context, query, args, ct);

        // ระดับของพนักงานผ่าน Hremployee.PosExecTypeId (FK → Pos_ExecType) — เดิมหาชื่อจาก pos_positions
        // ซึ่งไม่ใช่ตารางที่ POS_CODE ชี้ จึงได้ "—" ทุกแถว (22 ก.ย. 2569)
        var grouped = await query
            .GroupJoin(context.Pos_ExecTypes, e => e.PosExecTypeId, p => (long?)p.Id, (e, ps) => new { e, ps })
            .SelectMany(x => x.ps.DefaultIfEmpty(), (x, p) => new { Code = p != null ? p.Code : null, Name = p != null ? p.Name : null })
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
            "จำนวนพนักงานตามตำแหน่ง",
            new[]
            {
                new ReportColumn("code", "รหัสตำแหน่ง"),
                new ReportColumn("name", "ตำแหน่ง"),
                new ReportColumn("count", "จำนวน (คน)", ReportColumnType.Number),
            },
            rows, totals, Subtitle: $"บริษัท {ctx.CompanyId} · ณ {DateTime.Now:dd/MM/yyyy}" + (crit is null ? "" : " · " + crit));
    }
}
