using System.Globalization;
using HRM.Models;
using HRM.Models.Reporting;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Reporting.Reports;

// Counts of attendance exceptions (late / early-leave / absent) for a chosen
// month, from Att_DailyAttendance's boolean flags. A from/to date range, when
// supplied, replaces the year+month pick; dept / employment type restrict to
// matching employees; "kind" shows only one exception type.
public class AttendanceExceptionReport(IDbContextFactory<HRMContext> dbFactory) : IReportDefinition
{
    private static readonly string[] ThaiMonths =
        { "มกราคม","กุมภาพันธ์","มีนาคม","เมษายน","พฤษภาคม","มิถุนายน","กรกฎาคม","สิงหาคม","กันยายน","ตุลาคม","พฤศจิกายน","ธันวาคม" };

    private static readonly IReadOnlyList<ReportParamOption> KindOptions = new[]
    {
        new ReportParamOption("all", "ทุกประเภท"),
        new ReportParamOption("late", "มาสายเท่านั้น"),
        new ReportParamOption("early", "ออกก่อนเวลาเท่านั้น"),
        new ReportParamOption("absent", "ขาดงานเท่านั้น"),
    };

    public string Code => "attendance-exceptions";
    public string Category => "เวลาทำงาน & OT (Time & OT)";
    public string Name => "สรุปเวลาเข้างานผิดปกติ (รายเดือน)";
    public string? Description => "จำนวนครั้งมาสาย / ออกก่อน / ขาดงาน ในเดือนหรือช่วงวันที่ที่เลือก";

    public IReadOnlyList<ReportParameter> Parameters => new ReportParameter[]
    {
        new ReportParameter("year", "ปี (ค.ศ.)", ReportParamType.Year, Required: true, DefaultValue: DateTime.Today.Year.ToString()),
        new ReportParameter("month", "เดือน", ReportParamType.Select, Required: true,
            DefaultValue: DateTime.Today.Month.ToString(),
            Options: Enumerable.Range(1, 12).Select(i => new ReportParamOption(i.ToString(), ThaiMonths[i - 1])).ToList()),
        new ReportParameter("from", "ตั้งแต่วันที่", ReportParamType.Date,
            HelperText: "ระบุวันที่ = ใช้ช่วงวันที่แทนปี/เดือน (ใส่ช่องเดียวก็ได้)"),
        new ReportParameter("to", "ถึงวันที่", ReportParamType.Date),
        new ReportParameter("kind", "ประเภทเหตุผิดปกติ", ReportParamType.Select, DefaultValue: "all", Options: KindOptions),
    }.Concat(ReportCriteria.Standard()).ToList();

    private static DateOnly? ParseDate(IReadOnlyDictionary<string, string?> args, string key)
        => ReportCriteria.Arg(args, key) is { } s
           && DateOnly.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;

    public async Task<ReportResult> RunAsync(IReadOnlyDictionary<string, string?> args, ReportContext ctx, CancellationToken ct = default)
    {
        var year = args.TryGetValue("year", out var y) && int.TryParse(y, out var yy) ? yy : DateTime.Today.Year;
        var month = args.TryGetValue("month", out var m) && int.TryParse(m, out var mm) ? mm : DateTime.Today.Month;
        var dFrom = ParseDate(args, "from");
        var to = ParseDate(args, "to");
        var ranged = dFrom is not null || to is not null;

        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var q = context.Att_DailyAttendances.Where(a => a.CompanyId == ctx.CompanyId);
        if (ranged)
        {
            if (dFrom is { } f) q = q.Where(a => a.WorkDate >= f);
            if (to is { } t) q = q.Where(a => a.WorkDate <= t);
        }
        else
            q = q.Where(a => a.WorkDate.Year == year && a.WorkDate.Month == month);

        if (ReportCriteria.Arg(args, ReportCriteria.DeptKey) is not null || ReportCriteria.Arg(args, ReportCriteria.EmpTypeKey) is not null)
        {
            var empIds = (await ReportCriteria.ApplyEmployeeAsync(context,
                context.Hremployee.Where(e => e.companyid == ctx.CompanyId), args, ct)).Select(e => e.id);
            q = q.Where(a => empIds.Contains(a.HremployeeId));
        }

        var recs = await q.Select(a => new { a.IsLate, a.IsEarlyLeave, a.IsAbsent }).ToListAsync(ct);

        var kind = ReportCriteria.Arg(args, "kind") ?? "all";
        var showLate = kind is "all" or "late";
        var showEarly = kind is "all" or "early";
        var showAbsent = kind is "all" or "absent";
        var late = recs.Count(a => a.IsLate);
        var early = recs.Count(a => a.IsEarlyLeave);
        var absent = recs.Count(a => a.IsAbsent);

        var rows = new List<IReadOnlyDictionary<string, object?>>();
        if (showLate) rows.Add(new Dictionary<string, object?> { ["type"] = "มาสาย", ["count"] = late });
        if (showEarly) rows.Add(new Dictionary<string, object?> { ["type"] = "ออกก่อนเวลา", ["count"] = early });
        if (showAbsent) rows.Add(new Dictionary<string, object?> { ["type"] = "ขาดงาน", ["count"] = absent });

        var totals = new Dictionary<string, object?>
        {
            ["type"] = "รวมเหตุผิดปกติ",
            ["count"] = (showLate ? late : 0) + (showEarly ? early : 0) + (showAbsent ? absent : 0),
        };

        string period = !ranged ? $"{ThaiMonths[month - 1]} {year}"
            : dFrom is not null && to is not null ? $"{dFrom:dd/MM/yyyy} - {to:dd/MM/yyyy}"
            : dFrom is not null ? $"ตั้งแต่ {dFrom:dd/MM/yyyy}" : $"ถึง {to:dd/MM/yyyy}";
        var criteria = await ReportCriteria.DescribeAsync(context, ctx.CompanyId, args, ct);
        var kindLabel = kind == "all" ? null : KindOptions.FirstOrDefault(o => o.Value == kind)?.Label;

        return new ReportResult(
            $"สรุปเวลาเข้างานผิดปกติ — {period}",
            new[]
            {
                new ReportColumn("type", "ประเภท"),
                new ReportColumn("count", "จำนวน (ครั้ง)", ReportColumnType.Number),
            },
            rows, totals,
            Subtitle: $"บริษัท {ctx.CompanyId} · จากบันทึกเวลาทำงาน {recs.Count} รายการ{(ranged ? "" : "ในเดือนนี้")}"
                + (kindLabel is null ? "" : $" · {kindLabel}") + (criteria is null ? "" : $" · {criteria}"));
    }
}
