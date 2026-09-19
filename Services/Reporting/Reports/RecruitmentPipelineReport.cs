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

    public IReadOnlyList<ReportParameter> Parameters => new[]
    {
        new ReportParameter("from", "ยื่นคำขอตั้งแต่วันที่", ReportParamType.Date,
            HelperText: "เว้นว่าง = ไม่จำกัดวันที่"),
        new ReportParameter("to", "ถึงวันที่", ReportParamType.Date),
        new ReportParameter("reqstatus", "สถานะคำขอ", ReportParamType.Select,
            HelperText: "เว้นว่าง = ทุกสถานะ", Options: new[]
            {
                new ReportParamOption("", "ทั้งหมด"),
                new ReportParamOption(((int)RequisitionStatus.Draft).ToString(), "ร่าง"),
                new ReportParamOption(((int)RequisitionStatus.PendingApproval).ToString(), "รออนุมัติ"),
                new ReportParamOption(((int)RequisitionStatus.Approved).ToString(), "อนุมัติแล้ว"),
                new ReportParamOption(((int)RequisitionStatus.Filled).ToString(), "ได้คนแล้ว"),
                new ReportParamOption(((int)RequisitionStatus.Rejected).ToString(), "ไม่อนุมัติ"),
                new ReportParamOption(((int)RequisitionStatus.Cancelled).ToString(), "ยกเลิก"),
            }),
        new ReportParameter("reqtype", "ประเภทคำขอ", ReportParamType.Select,
            HelperText: "เว้นว่าง = ทุกประเภท", Options: new[]
            {
                new ReportParamOption("", "ทั้งหมด"),
                new ReportParamOption(((int)RequisitionType.Replacement).ToString(), "ทดแทน"),
                new ReportParamOption(((int)RequisitionType.NewHeadcount).ToString(), "เพิ่มอัตราใหม่"),
            }),
    }.Concat(ReportCriteria.Standard(empType: false)).ToList();

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

        var query = context.Rec_Requisitions.Where(r => r.CompanyId == ctx.CompanyId);
        if (DateTime.TryParse(ReportCriteria.Arg(args, "from"), out var from))
            query = query.Where(r => r.RequestedDate >= from.Date);
        if (DateTime.TryParse(ReportCriteria.Arg(args, "to"), out var to))
        {
            var toEnd = to.Date.AddDays(1);
            query = query.Where(r => r.RequestedDate < toEnd);
        }
        if (int.TryParse(ReportCriteria.Arg(args, "reqstatus"), out var statusFilter))
        {
            var st = (RequisitionStatus)statusFilter;
            query = query.Where(r => r.Status == st);
        }
        if (int.TryParse(ReportCriteria.Arg(args, "reqtype"), out var typeFilter))
        {
            var rt = (RequisitionType)typeFilter;
            query = query.Where(r => r.RequisitionType == rt);
        }
        // หน่วยงาน: the requisition points at one org unit (OrganizationId =
        // com_organization.id); a chosen unit includes every unit below it.
        if (ReportCriteria.Arg(args, ReportCriteria.DeptKey) is { } dept)
        {
            var prefix = await context.com_organizations
                .Where(o => o.code == dept).Select(o => o.orgcodefull).FirstOrDefaultAsync(ct);
            var orgIds = string.IsNullOrWhiteSpace(prefix)
                ? context.com_organizations.Where(_ => false).Select(o => o.id)
                : context.com_organizations.Where(o => o.orgcodefull != null && o.orgcodefull.StartsWith(prefix)).Select(o => o.id);
            query = query.Where(r => orgIds.Contains(r.OrganizationId));
        }
        var crit = await ReportCriteria.DescribeAsync(context, ctx.CompanyId, args, ct);
        var extra = new List<string>();
        if (ReportCriteria.Arg(args, "from") is not null || ReportCriteria.Arg(args, "to") is not null)
            extra.Add($"ยื่นคำขอ {ReportCriteria.Arg(args, "from") ?? "…"} ถึง {ReportCriteria.Arg(args, "to") ?? "…"}");
        if (crit is not null) extra.Add(crit);

        var reqs = await query
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
            Subtitle: $"บริษัท {ctx.CompanyId} · ณ {DateTime.Now:dd/MM/yyyy}"
                + (extra.Count == 0 ? "" : " · " + string.Join(" · ", extra)));
    }
}
