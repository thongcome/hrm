using HRM.Models;
using HRM.Models.Reporting;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Reporting.Reports;

// Disciplinary cases by action type for a year — how many verbal/written
// warnings, suspensions, terminations were recorded.
public class DisciplinaryCaseSummaryReport(IDbContextFactory<HRMContext> dbFactory) : IReportDefinition
{
    public string Code => "disciplinary-summary";
    public string Category => "แรงงานสัมพันธ์ (Employee Relations)";
    public string Name => "สรุปกรณีทางวินัย (รายปี)";
    public string? Description => "จำนวนกรณีทางวินัย แยกตามประเภทการลงโทษ ในปีที่เลือก";

    public IReadOnlyList<ReportParameter> Parameters => new[]
    {
        new ReportParameter("year", "ปี (ค.ศ.)", ReportParamType.Year, Required: true, DefaultValue: DateTime.Today.Year.ToString()),
        new ReportParameter("actiontype", "ประเภทการลงโทษ", ReportParamType.Select,
            HelperText: "เว้นว่าง = ทุกประเภท", Options: new[]
            {
                new ReportParamOption("", "ทั้งหมด"),
                new ReportParamOption(((int)DisciplinaryActionType.VerbalWarning).ToString(), TypeLabel(DisciplinaryActionType.VerbalWarning)),
                new ReportParamOption(((int)DisciplinaryActionType.WrittenWarning).ToString(), TypeLabel(DisciplinaryActionType.WrittenWarning)),
                new ReportParamOption(((int)DisciplinaryActionType.Suspension).ToString(), TypeLabel(DisciplinaryActionType.Suspension)),
                new ReportParamOption(((int)DisciplinaryActionType.Termination).ToString(), TypeLabel(DisciplinaryActionType.Termination)),
            }),
    }.Concat(ReportCriteria.Standard(statusDefault: ReportCriteria.StatusAll)).ToList();

    private static string TypeLabel(DisciplinaryActionType t) => t switch
    {
        DisciplinaryActionType.VerbalWarning => "ตักเตือนด้วยวาจา",
        DisciplinaryActionType.WrittenWarning => "ตักเตือนเป็นลายลักษณ์อักษร",
        DisciplinaryActionType.Suspension => "พักงาน",
        DisciplinaryActionType.Termination => "เลิกจ้าง",
        _ => t.ToString(),
    };

    public async Task<ReportResult> RunAsync(IReadOnlyDictionary<string, string?> args, ReportContext ctx, CancellationToken ct = default)
    {
        var year = args.TryGetValue("year", out var y) && int.TryParse(y, out var yy) ? yy : DateTime.Today.Year;

        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var query = context.Hr_DisciplinaryCases
            .Where(c => c.CompanyId == ctx.CompanyId && c.CreatedDate.Year == year);
        if (int.TryParse(ReportCriteria.Arg(args, "actiontype"), out var typeFilter))
        {
            var at = (DisciplinaryActionType)typeFilter;
            query = query.Where(c => c.ActionType == at);
        }
        // employee criteria apply to the person the case is about (HremployeeId);
        // "all" status (the default) adds no filter, so default output is unchanged
        if (ReportCriteria.Arg(args, ReportCriteria.DeptKey) is not null
            || ReportCriteria.Arg(args, ReportCriteria.EmpTypeKey) is not null
            || (ReportCriteria.Arg(args, ReportCriteria.StatusKey) is { } st && st != ReportCriteria.StatusAll))
        {
            var emps = await ReportCriteria.ApplyEmployeeAsync(context,
                context.Hremployee.Where(e => e.companyid == ctx.CompanyId), args, ct);
            var empIds = emps.Select(e => e.id);
            query = query.Where(c => empIds.Contains(c.HremployeeId));
        }
        // "ทั้งหมด" status is the default, not a criterion worth printing
        var descArgs = args.Where(kv => !(kv.Key == ReportCriteria.StatusKey && kv.Value == ReportCriteria.StatusAll))
            .ToDictionary(kv => kv.Key, kv => kv.Value);
        var crit = await ReportCriteria.DescribeAsync(context, ctx.CompanyId, descArgs, ct);

        var cases = await query
            .Select(c => c.ActionType)
            .ToListAsync(ct);

        var counts = cases.GroupBy(a => a).ToDictionary(g => g.Key, g => g.Count());
        var order = new[] { DisciplinaryActionType.VerbalWarning, DisciplinaryActionType.WrittenWarning,
            DisciplinaryActionType.Suspension, DisciplinaryActionType.Termination };

        var rows = order.Select(a => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
        {
            ["type"] = TypeLabel(a),
            ["count"] = counts.TryGetValue(a, out var c) ? c : 0,
        }).ToList();

        var totals = new Dictionary<string, object?> { ["type"] = "รวมทั้งหมด", ["count"] = cases.Count };

        return new ReportResult(
            $"สรุปกรณีทางวินัย — ปี {year}",
            new[]
            {
                new ReportColumn("type", "ประเภทการลงโทษ"),
                new ReportColumn("count", "จำนวน (กรณี)", ReportColumnType.Number),
            },
            rows, totals,
            Subtitle: $"บริษัท {ctx.CompanyId}" + (crit is null ? "" : $" · {crit}"));
    }
}
