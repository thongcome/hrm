using HRM.Models;
using HRM.Models.Reporting;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Reporting.Reports;

// Recruitment requisitions by status — the hiring pipeline at a glance, with
// the number of open positions (openings) behind each status.
public class RecruitmentPipelineReport(IDbContextFactory<HRMContext> dbFactory) : IReportDefinition
{
    public string Code => "recruitment-pipeline";
    public string Category => "สรรหาบุคลากร (Recruitment)";
    public string Name => "สถานะคำขออัตรากำลัง (Requisition Pipeline)";
    public string? Description => "จำนวนคำขออัตรากำลังและจำนวนอัตราที่เปิด แยกตามสถานะ";

    public IReadOnlyList<ReportParameter> Parameters => Array.Empty<ReportParameter>();

    private static string StatusLabel(RequisitionStatus s) => s switch
    {
        RequisitionStatus.Draft => "ร่าง",
        RequisitionStatus.PendingApproval => "รออนุมัติ",
        RequisitionStatus.Approved => "อนุมัติแล้ว",
        RequisitionStatus.Rejected => "ไม่อนุมัติ",
        RequisitionStatus.Filled => "ได้คนแล้ว",
        RequisitionStatus.Cancelled => "ยกเลิก",
        _ => s.ToString(),
    };

    public async Task<ReportResult> RunAsync(IReadOnlyDictionary<string, string?> args, ReportContext ctx, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var reqs = await context.Rec_Requisitions
            .Where(r => r.CompanyId == ctx.CompanyId)
            .Select(r => new { r.Status, r.OpeningsCount })
            .ToListAsync(ct);

        var byStatus = reqs.GroupBy(r => r.Status)
            .ToDictionary(g => g.Key, g => (Count: g.Count(), Openings: g.Sum(x => x.OpeningsCount)));

        var order = new[] { RequisitionStatus.Draft, RequisitionStatus.PendingApproval, RequisitionStatus.Approved,
            RequisitionStatus.Filled, RequisitionStatus.Rejected, RequisitionStatus.Cancelled };

        var rows = order.Select(s =>
        {
            byStatus.TryGetValue(s, out var v);
            return (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
            {
                ["status"] = StatusLabel(s),
                ["count"] = v.Count,
                ["openings"] = v.Openings,
            };
        }).ToList();

        var totals = new Dictionary<string, object?>
        {
            ["status"] = "รวมทั้งหมด",
            ["count"] = reqs.Count,
            ["openings"] = reqs.Sum(r => r.OpeningsCount),
        };

        return new ReportResult(
            "สถานะคำขออัตรากำลัง (Requisition Pipeline)",
            new[]
            {
                new ReportColumn("status", "สถานะ"),
                new ReportColumn("count", "จำนวนคำขอ", ReportColumnType.Number),
                new ReportColumn("openings", "จำนวนอัตราที่เปิด", ReportColumnType.Number),
            },
            rows, totals,
            Subtitle: $"บริษัท {ctx.CompanyId} · ณ {DateTime.Now:dd/MM/yyyy}");
    }
}
