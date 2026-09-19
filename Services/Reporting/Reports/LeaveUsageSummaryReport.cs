using HRM.Models;
using HRM.Models.Reporting;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Reporting.Reports;

// Total approved leave days by leave type for a year — the leave slice of the
// standard report set. Counts only requests that entered the workflow
// (JobMasterId != null) so drafts don't inflate the numbers.
public class LeaveUsageSummaryReport(IDbContextFactory<HRMContext> dbFactory) : IReportDefinition, IReportDynamicOptions
{
    public string Code => "leave-usage-summary";
    public string Category => "การลา (Leave)";
    public string Name => "สรุปการใช้วันลาตามประเภท";
    public string? Description => "รวมจำนวนวันลาและจำนวนคำขอ แยกตามประเภทการลา ในปีที่เลือก";

    public IReadOnlyList<ReportParameter> Parameters => new ReportParameter[]
    {
        new ReportParameter("year", "ปี (ค.ศ.)", ReportParamType.Year, Required: true,
            DefaultValue: DateTime.Now.Year.ToString(), HelperText: "อ้างอิงจากวันเริ่มลา"),
        new ReportParameter("from", "ตั้งแต่วันที่", ReportParamType.Date, HelperText: "ระบุแล้วจำกัดเฉพาะช่วงวันเริ่มลานี้ภายในปีที่เลือก"),
        new ReportParameter("to", "ถึงวันที่", ReportParamType.Date),
        new ReportParameter("leavetype", "ประเภทการลา", ReportParamType.Select, HelperText: "เว้นว่าง = ทุกประเภท"),
    }.Concat(ReportCriteria.Standard()).ToList();

    public async Task<IReadOnlyList<ReportParamOption>> GetOptionsAsync(string parameterKey, ReportContext ctx, CancellationToken ct = default)
    {
        if (parameterKey != "leavetype") return Array.Empty<ReportParamOption>();
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var types = await context.Lve_LeaveTypes.OrderBy(t => t.Code)
            .Select(t => new ReportParamOption(t.Id.ToString(), t.NameTh)).ToListAsync(ct);
        return new[] { new ReportParamOption("", "ทั้งหมด") }.Concat(types).ToList();
    }

    private static DateOnly? ParseDate(IReadOnlyDictionary<string, string?> args, string key)
        => ReportCriteria.Arg(args, key) is { } s
           && DateOnly.TryParse(s, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var d) ? d : null;

    public async Task<ReportResult> RunAsync(IReadOnlyDictionary<string, string?> args, ReportContext ctx, CancellationToken ct = default)
    {
        var year = TurnoverReport.ParseYear(args);
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var q = context.Lve_LeaveRequests
            .Where(r => r.CompanyId == ctx.CompanyId && r.JobMasterId != null && r.StartDate.Year == year);

        var dFrom = ParseDate(args, "from");
        var to = ParseDate(args, "to");
        if (dFrom is { } f) q = q.Where(r => r.StartDate >= f);
        if (to is { } t) q = q.Where(r => r.StartDate <= t);
        var typeFilter = int.TryParse(ReportCriteria.Arg(args, "leavetype"), out var typeId) ? typeId : (int?)null;
        if (typeFilter is { } tf) q = q.Where(r => r.LeaveTypeId == tf);

        if (ReportCriteria.Arg(args, ReportCriteria.DeptKey) is not null || ReportCriteria.Arg(args, ReportCriteria.EmpTypeKey) is not null)
        {
            var empIds = (await ReportCriteria.ApplyEmployeeAsync(context,
                context.Hremployee.Where(e => e.companyid == ctx.CompanyId), args, ct)).Select(e => e.id);
            q = q.Where(r => empIds.Contains(r.HremployeeId));
        }

        var reqs = await q.Select(r => new { r.LeaveTypeId, r.TotalDays }).ToListAsync(ct);

        var types = await context.Lve_LeaveTypes.ToDictionaryAsync(t => t.Id, t => t.NameTh, ct);

        var extra = new List<string>();
        if (dFrom is not null || to is not null)
            extra.Add($"วันเริ่มลา {(dFrom is { } a ? a.ToString("dd/MM/yyyy") : "…")} - {(to is { } b ? b.ToString("dd/MM/yyyy") : "…")}");
        if (typeFilter is { } tid) extra.Add($"ประเภท {(types.TryGetValue(tid, out var tn) ? tn : "#" + tid)}");
        if (await ReportCriteria.DescribeAsync(context, ctx.CompanyId, args, ct) is { } criteria) extra.Add(criteria);

        var grouped = reqs
            .GroupBy(r => r.LeaveTypeId)
            .Select(g => new { TypeId = g.Key, Days = g.Sum(x => x.TotalDays), Count = g.Count() })
            .OrderByDescending(x => x.Days)
            .ToList();

        var rows = grouped.Select(g => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
        {
            ["type"] = types.TryGetValue(g.TypeId, out var n) ? n : $"#{g.TypeId}",
            ["count"] = g.Count,
            ["days"] = g.Days,
        }).ToList();

        var totals = new Dictionary<string, object?>
        {
            ["type"] = "รวมทั้งหมด",
            ["count"] = grouped.Sum(g => g.Count),
            ["days"] = grouped.Sum(g => g.Days),
        };

        return new ReportResult(
            $"สรุปการใช้วันลาตามประเภท — ปี {year}",
            new[]
            {
                new ReportColumn("type", "ประเภทการลา"),
                new ReportColumn("count", "จำนวนคำขอ", ReportColumnType.Number),
                new ReportColumn("days", "รวมวันลา", ReportColumnType.Number),
            },
            rows, totals, Subtitle: $"บริษัท {ctx.CompanyId} · เฉพาะคำขอที่เข้าสายอนุมัติแล้ว" + (extra.Count == 0 ? "" : " · " + string.Join(" · ", extra)));
    }
}
