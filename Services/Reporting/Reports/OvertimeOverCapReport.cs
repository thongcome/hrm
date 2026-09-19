using System.Globalization;
using HRM.Models;
using HRM.Models.Reporting;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Reporting.Reports;

// Employees whose OT hours within one week (Mon–Sun) exceed a cap. The cap is a
// parameter (default 36 = the weekly OT limit under Thai labour law), not a hardcoded rule.
public class OvertimeOverCapReport(IDbContextFactory<HRMContext> dbFactory) : IReportDefinition
{
    public string Code => "ot-over-cap";
    public string Category => "เวลาทำงาน & OT (Time & OT)";
    public string Name => "พนักงานที่ OT เกินเพดานรายสัปดาห์";
    public string? Description => "พนักงานที่ชั่วโมงทำงานล่วงเวลาในสัปดาห์ใดสัปดาห์หนึ่ง (จันทร์–อาทิตย์) เกินเพดานที่กำหนด";

    public IReadOnlyList<ReportParameter> Parameters => new[]
    {
        new ReportParameter("from", "ตั้งแต่วันที่", ReportParamType.Date, Required: true,
            DefaultValue: new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).ToString("yyyy-MM-dd")),
        new ReportParameter("to", "ถึงวันที่", ReportParamType.Date, Required: true,
            DefaultValue: DateTime.Today.ToString("yyyy-MM-dd")),
        new ReportParameter("cap", "เพดาน OT (ชั่วโมง/สัปดาห์)", ReportParamType.Number, DefaultValue: "36",
            HelperText: "ค่าเริ่มต้น 36 ชม./สัปดาห์ ตามกฎหมายคุ้มครองแรงงาน — ปรับได้"),
    }.Concat(ReportCriteria.Standard()).ToList();

    private static DateTime WeekStart(DateTime d)
    {
        var diff = ((int)d.DayOfWeek + 6) % 7; // Monday = 0
        return d.Date.AddDays(-diff);
    }

    public async Task<ReportResult> RunAsync(IReadOnlyDictionary<string, string?> args, ReportContext ctx, CancellationToken ct = default)
    {
        var from = args.TryGetValue("from", out var f) && DateTime.TryParse(f, out var ff) ? ff.Date : new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var to = args.TryGetValue("to", out var t) && DateTime.TryParse(t, out var tt) ? tt.Date : DateTime.Today;
        var cap = args.TryGetValue("cap", out var c) && decimal.TryParse(c, NumberStyles.Number, CultureInfo.InvariantCulture, out var cc) && cc >= 0 ? cc : 36m;
        var toEnd = to.AddDays(1);

        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var empQuery = context.Hremployee.Where(e => e.companyid == ctx.CompanyId);
        empQuery = await ReportCriteria.ApplyEmployeeAsync(context, empQuery, args, ct);
        var emps = await empQuery.Select(e => new { e.EmpNo, e.EmpName, e.EmpSurname }).ToListAsync(ct);
        var nameByNo = emps.GroupBy(e => e.EmpNo).ToDictionary(g => g.Key, g => $"{g.First().EmpName} {g.First().EmpSurname}".Trim());
        var empNos = nameByNo.Keys.ToList();

        // whole weeks are needed to judge a week, so widen the pull to Monday..Sunday around the range
        var pullFrom = WeekStart(from);
        var pullTo = WeekStart(to).AddDays(7);

        var ot = await context.HrwOts
            .Where(o => o.companyid == ctx.CompanyId && o.DateWork != null && o.DateWork >= pullFrom && o.DateWork < pullTo
                        && empNos.Contains(o.EmpNo))
            .Select(o => new { o.EmpNo, o.DateWork, o.OtPMinute })
            .ToListAsync(ct);

        var firstWeek = WeekStart(from);
        var over = ot
            .GroupBy(o => new { o.EmpNo, Week = WeekStart(o.DateWork!.Value) })
            .Select(g => new { g.Key.EmpNo, g.Key.Week, Hours = Math.Round(g.Sum(x => x.OtPMinute ?? 0m) / 60m, 2) })
            .Where(x => x.Hours > cap && x.Week >= firstWeek && x.Week < toEnd)
            .OrderBy(x => x.EmpNo).ThenBy(x => x.Week)
            .ToList();

        var rows = over.Select(x => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
        {
            ["empno"] = x.EmpNo,
            ["name"] = nameByNo.TryGetValue(x.EmpNo, out var n) ? n : "",
            ["week"] = x.Week,
            ["hours"] = x.Hours,
            ["cap"] = cap,
            ["excess"] = Math.Round(x.Hours - cap, 2),
        }).ToList();

        var totals = new Dictionary<string, object?>
        {
            ["empno"] = "รวม",
            ["name"] = $"{over.Select(o => o.EmpNo).Distinct().Count():N0} คน / {over.Count:N0} สัปดาห์",
            ["hours"] = over.Sum(o => o.Hours),
            ["excess"] = over.Sum(o => Math.Round(o.Hours - cap, 2)),
        };

        var crit = await ReportCriteria.DescribeAsync(context, ctx.CompanyId, args, ct);
        return new ReportResult(
            "พนักงานที่ OT เกินเพดานรายสัปดาห์",
            new[]
            {
                new ReportColumn("empno", "รหัสพนักงาน"),
                new ReportColumn("name", "ชื่อ-สกุล"),
                new ReportColumn("week", "สัปดาห์เริ่ม (จันทร์)", ReportColumnType.Date),
                new ReportColumn("hours", "ชั่วโมง OT", ReportColumnType.Number),
                new ReportColumn("cap", "เพดาน (ชม.)", ReportColumnType.Number),
                new ReportColumn("excess", "เกินเพดาน (ชม.)", ReportColumnType.Number),
            },
            rows, totals,
            $"บริษัท {ctx.CompanyId} · {from:dd/MM/yyyy} – {to:dd/MM/yyyy} · เพดาน {cap:0.##} ชม./สัปดาห์" + (crit is null ? "" : " · " + crit));
    }
}
