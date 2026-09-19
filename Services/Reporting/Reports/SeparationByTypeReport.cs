using HRM.Models;
using HRM.Models.Reporting;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Reporting.Reports;

// Separations (Hremployee.ResignDate) in a date range, either summarised by
// separation type or listed per person with the reason from the approved
// Hr_SeparationRequest (Hremployee itself has no reason column).
public class SeparationByTypeReport(IDbContextFactory<HRMContext> dbFactory) : IReportDefinition
{
    public string Code => "separation-by-type";
    public string Category => "กำลังพล (Headcount)";
    public string Name => "การพ้นสภาพพนักงานตามประเภท";
    public string? Description => "พนักงานที่ลาออก/พ้นสภาพในช่วงวันที่ แยกตามประเภทการพ้นสภาพ (สรุปหรือรายชื่อพร้อมเหตุผล)";

    private static readonly IReadOnlyList<ReportParamOption> ViewOptions = new[]
    {
        new ReportParamOption("summary", "สรุปตามประเภท"),
        new ReportParamOption("detail", "รายชื่อ"),
    };

    public IReadOnlyList<ReportParameter> Parameters => new[]
    {
        new ReportParameter("from", "ตั้งแต่วันที่", ReportParamType.Date, Required: true,
            DefaultValue: new DateTime(DateTime.Today.Year, 1, 1).ToString("yyyy-MM-dd")),
        new ReportParameter("to", "ถึงวันที่", ReportParamType.Date, Required: true,
            DefaultValue: DateTime.Today.ToString("yyyy-MM-dd")),
        new ReportParameter("view", "รูปแบบ", ReportParamType.Select, DefaultValue: "summary", Options: ViewOptions),
    }.Concat(ReportCriteria.Standard()).ToList();

    private static string TypeLabel(SeparationType? t) => t switch
    {
        SeparationType.VoluntaryResignation => "ลาออกเอง",
        SeparationType.TerminationOrdinary => "เลิกจ้าง (ไม่เข้ามาตรา 119)",
        SeparationType.TerminationSection119 => "เลิกจ้าง (มาตรา 119)",
        _ => "ไม่ระบุประเภท",
    };

    public async Task<ReportResult> RunAsync(IReadOnlyDictionary<string, string?> args, ReportContext ctx, CancellationToken ct = default)
    {
        var from = args.TryGetValue("from", out var f) && DateTime.TryParse(f, out var ff) ? ff.Date : new DateTime(DateTime.Today.Year, 1, 1);
        var to = args.TryGetValue("to", out var t) && DateTime.TryParse(t, out var tt) ? tt.Date : DateTime.Today;
        var toEnd = to.AddDays(1);
        var detail = ReportCriteria.Arg(args, "view") == "detail";

        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var query = context.Hremployee.Where(e => e.companyid == ctx.CompanyId
            && e.ResignDate != null && e.ResignDate >= from && e.ResignDate < toEnd);
        query = await ReportCriteria.ApplyEmployeeAsync(context, query, args, ct);
        var emps = await query
            .Select(e => new { e.id, e.EmpNo, e.EmpName, e.EmpSurname, e.ResignDate, e.SeparationType })
            .ToListAsync(ct);

        var crit = await ReportCriteria.DescribeAsync(context, ctx.CompanyId, args, ct);
        var subtitle = $"บริษัท {ctx.CompanyId} · {from:dd/MM/yyyy} – {to:dd/MM/yyyy}" + (crit is null ? "" : " · " + crit);

        if (!detail)
        {
            var groups = emps.GroupBy(e => TypeLabel(e.SeparationType))
                .Select(g => new { Label = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count).ToList();
            var total = groups.Sum(g => g.Count);
            var sumRows = groups.Select(g => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
            {
                ["type"] = g.Label,
                ["count"] = g.Count,
                ["pct"] = total == 0 ? 0m : Math.Round(g.Count * 100m / total, 1),
            }).ToList();
            return new ReportResult("การพ้นสภาพพนักงานตามประเภท (สรุป)",
                new[]
                {
                    new ReportColumn("type", "ประเภทการพ้นสภาพ"),
                    new ReportColumn("count", "จำนวน (คน)", ReportColumnType.Number),
                    new ReportColumn("pct", "สัดส่วน %", ReportColumnType.Percent),
                },
                sumRows,
                new Dictionary<string, object?> { ["type"] = "รวมทั้งหมด", ["count"] = total, ["pct"] = total == 0 ? 0m : 100m },
                subtitle);
        }

        var ids = emps.Select(e => e.id).ToList();
        var reasons = await context.Hr_SeparationRequests
            .Where(r => r.CompanyId == ctx.CompanyId && r.Status == SeparationRequestStatus.Approved && ids.Contains(r.HremployeeId))
            .Select(r => new { r.HremployeeId, r.Reason, r.EffectiveDate })
            .ToListAsync(ct);
        var reasonById = reasons.GroupBy(r => r.HremployeeId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.EffectiveDate).First().Reason);

        var rows = emps.OrderBy(e => e.ResignDate).ThenBy(e => e.EmpNo)
            .Select(e => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
            {
                ["empno"] = e.EmpNo,
                ["name"] = $"{e.EmpName} {e.EmpSurname}".Trim(),
                ["date"] = e.ResignDate,
                ["type"] = TypeLabel(e.SeparationType),
                ["reason"] = reasonById.TryGetValue(e.id, out var r) ? r : null,
            }).ToList();

        return new ReportResult("การพ้นสภาพพนักงานตามประเภท (รายชื่อ)",
            new[]
            {
                new ReportColumn("empno", "รหัสพนักงาน"),
                new ReportColumn("name", "ชื่อ-สกุล"),
                new ReportColumn("date", "วันที่พ้นสภาพ", ReportColumnType.Date),
                new ReportColumn("type", "ประเภท"),
                new ReportColumn("reason", "เหตุผล"),
            },
            rows,
            new Dictionary<string, object?> { ["empno"] = "รวม", ["name"] = $"{rows.Count:N0} คน" },
            subtitle);
    }
}
