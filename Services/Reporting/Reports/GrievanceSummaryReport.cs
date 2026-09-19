using HRM.Models;
using HRM.Models.Reporting;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Reporting.Reports;

// Employee grievances by status for a year — how many submitted, under
// investigation, resolved, dismissed.
public class GrievanceSummaryReport(IDbContextFactory<HRMContext> dbFactory) : IReportDefinition
{
    public string Code => "grievance-summary";
    public string Category => "แรงงานสัมพันธ์ (Employee Relations)";
    public string Name => "สรุปเรื่องร้องเรียน (รายปี)";
    public string? Description => "จำนวนเรื่องร้องเรียน แยกตามสถานะการดำเนินการ ในปีที่เลือก";

    public IReadOnlyList<ReportParameter> Parameters => new[]
    {
        new ReportParameter("year", "ปี (ค.ศ.)", ReportParamType.Year, Required: true, DefaultValue: DateTime.Today.Year.ToString()),
        new ReportParameter("category", "หมวดเรื่องร้องเรียน", ReportParamType.Select,
            HelperText: "เว้นว่าง = ทุกหมวด", Options: new[]
            {
                new ReportParamOption("", "ทั้งหมด"),
                new ReportParamOption(((int)GrievanceCategory.Harassment).ToString(), "การคุกคาม"),
                new ReportParamOption(((int)GrievanceCategory.Discrimination).ToString(), "การเลือกปฏิบัติ"),
                new ReportParamOption(((int)GrievanceCategory.WorkingConditions).ToString(), "สภาพการทำงาน"),
                new ReportParamOption(((int)GrievanceCategory.Compensation).ToString(), "ค่าตอบแทน"),
                new ReportParamOption(((int)GrievanceCategory.Management).ToString(), "การบริหารจัดการ/หัวหน้างาน"),
                new ReportParamOption(((int)GrievanceCategory.Other).ToString(), "อื่น ๆ"),
            }),
        new ReportParameter("grvstatus", "สถานะการดำเนินการ", ReportParamType.Select,
            HelperText: "เว้นว่าง = ทุกสถานะ", Options: new[]
            {
                new ReportParamOption("", "ทั้งหมด"),
                new ReportParamOption(((int)GrievanceStatus.Submitted).ToString(), StatusLabel(GrievanceStatus.Submitted)),
                new ReportParamOption(((int)GrievanceStatus.UnderInvestigation).ToString(), StatusLabel(GrievanceStatus.UnderInvestigation)),
                new ReportParamOption(((int)GrievanceStatus.Resolved).ToString(), StatusLabel(GrievanceStatus.Resolved)),
                new ReportParamOption(((int)GrievanceStatus.Dismissed).ToString(), StatusLabel(GrievanceStatus.Dismissed)),
            }),
    }.Concat(ReportCriteria.Standard(empType: false)).ToList();

    private static string StatusLabel(GrievanceStatus s) => s switch
    {
        GrievanceStatus.Submitted => "ยื่นเรื่อง",
        GrievanceStatus.UnderInvestigation => "อยู่ระหว่างสอบสวน",
        GrievanceStatus.Resolved => "แก้ไขแล้ว",
        GrievanceStatus.Dismissed => "ยกคำร้อง",
        _ => s.ToString(),
    };

    public async Task<ReportResult> RunAsync(IReadOnlyDictionary<string, string?> args, ReportContext ctx, CancellationToken ct = default)
    {
        var year = args.TryGetValue("year", out var y) && int.TryParse(y, out var yy) ? yy : DateTime.Today.Year;

        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var query = context.Hr_Grievances
            .Where(g => g.CompanyId == ctx.CompanyId && g.CreatedDate.Year == year);
        if (int.TryParse(ReportCriteria.Arg(args, "category"), out var catFilter))
        {
            var cat = (GrievanceCategory)catFilter;
            query = query.Where(g => g.Category == cat);
        }
        if (int.TryParse(ReportCriteria.Arg(args, "grvstatus"), out var statusFilter))
        {
            var st = (GrievanceStatus)statusFilter;
            query = query.Where(g => g.Status == st);
        }
        // หน่วยงาน is judged by the complainant (ReporterHremployeeId); an
        // anonymous complaint has no reporter so it drops out once a unit is chosen
        if (ReportCriteria.Arg(args, ReportCriteria.DeptKey) is not null)
        {
            var emps = await ReportCriteria.ApplyEmployeeAsync(context,
                context.Hremployee.Where(e => e.companyid == ctx.CompanyId), args, ct);
            var empIds = emps.Select(e => (long?)e.id);
            query = query.Where(g => empIds.Contains(g.ReporterHremployeeId));
        }
        var crit = await ReportCriteria.DescribeAsync(context, ctx.CompanyId, args, ct);

        var items = await query
            .Select(g => g.Status)
            .ToListAsync(ct);

        var counts = items.GroupBy(s => s).ToDictionary(g => g.Key, g => g.Count());
        var order = new[] { GrievanceStatus.Submitted, GrievanceStatus.UnderInvestigation, GrievanceStatus.Resolved, GrievanceStatus.Dismissed };

        var rows = order.Select(s => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
        {
            ["status"] = StatusLabel(s),
            ["count"] = counts.TryGetValue(s, out var c) ? c : 0,
        }).ToList();

        var totals = new Dictionary<string, object?> { ["status"] = "รวมทั้งหมด", ["count"] = items.Count };

        return new ReportResult(
            $"สรุปเรื่องร้องเรียน — ปี {year}",
            new[]
            {
                new ReportColumn("status", "สถานะ"),
                new ReportColumn("count", "จำนวน (เรื่อง)", ReportColumnType.Number),
            },
            rows, totals,
            Subtitle: $"บริษัท {ctx.CompanyId}" + (crit is null ? "" : $" · {crit} (ตามผู้ร้องเรียน ไม่รวมเรื่องไม่ประสงค์ออกนาม)"));
    }
}
