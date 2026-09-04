using HRM.Models;
using HRM.Models.Reporting;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Reporting.Reports;

// Active employees whose work-start (hire) date anniversary falls in a selected
// month, with completed years of service — the month parameter is a static
// 1..12 dropdown so no dynamic options are needed.
public class WorkAnniversaryReport(IDbContextFactory<HRMContext> dbFactory) : IReportDefinition
{
    private static readonly string[] ThaiMonths =
    {
        "มกราคม", "กุมภาพันธ์", "มีนาคม", "เมษายน", "พฤษภาคม", "มิถุนายน",
        "กรกฎาคม", "สิงหาคม", "กันยายน", "ตุลาคม", "พฤศจิกายน", "ธันวาคม",
    };

    public string Code => "work-anniversary";
    public string Category => "การจ้างงาน (Employment)";
    public string Name => "พนักงานครบรอบวันเริ่มงานในเดือน";
    public string? Description => "รายชื่อพนักงานที่ยังทำงานอยู่และครบรอบวันเริ่มงานในเดือนที่เลือก พร้อมอายุงาน";

    public IReadOnlyList<ReportParameter> Parameters => new[]
    {
        new ReportParameter("month", "เดือน", ReportParamType.Select,
            DefaultValue: DateTime.Today.Month.ToString(),
            Options: Enumerable.Range(1, 12)
                .Select(i => new ReportParamOption(i.ToString(), ThaiMonths[i - 1]))
                .ToList()),
    };

    public async Task<ReportResult> RunAsync(IReadOnlyDictionary<string, string?> args, ReportContext ctx, CancellationToken ct = default)
    {
        var month = DateTime.Today.Month;
        if (args.TryGetValue("month", out var monthStr) && int.TryParse(monthStr, out var m) && m >= 1 && m <= 12)
            month = m;

        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var today = DateTime.Today;

        var emps = await context.Hremployee
            .Where(e => e.companyid == ctx.CompanyId && e.ResignDate == null && e.WorkDate != null)
            .Select(e => new { e.EmpNo, e.EmpName, e.EmpSurname, e.DeptgrpCode, e.WorkDate })
            .ToListAsync(ct);

        var ordered = emps
            .Where(e => e.WorkDate!.Value.Month == month)
            .OrderBy(e => e.WorkDate!.Value.Day)
            .ToList();

        var monthName = ThaiMonths[month - 1];

        var rows = ordered.Select(e =>
        {
            var start = e.WorkDate!.Value;
            var years = today.Year - start.Year;
            if (start.Date > today.AddYears(-years)) years--;
            if (years < 0) years = 0;
            return (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
            {
                ["empno"] = e.EmpNo,
                ["name"] = $"{e.EmpName} {e.EmpSurname}".Trim(),
                ["dept"] = e.DeptgrpCode ?? "—",
                ["workDate"] = start,
                ["years"] = years,
            };
        }).ToList();

        var totals = new Dictionary<string, object?> { ["empno"] = "รวม", ["name"] = $"{ordered.Count} คน" };

        return new ReportResult(
            $"พนักงานครบรอบวันเริ่มงานในเดือน{monthName}",
            new[]
            {
                new ReportColumn("empno", "รหัส"),
                new ReportColumn("name", "ชื่อ-สกุล"),
                new ReportColumn("dept", "หน่วยงาน"),
                new ReportColumn("workDate", "วันเริ่มงาน", ReportColumnType.Date),
                new ReportColumn("years", "อายุงาน (ปี)", ReportColumnType.Number),
            },
            rows, totals,
            Subtitle: $"เดือน{monthName} · {ordered.Count} คน · ณ {DateTime.Now:dd/MM/yyyy}");
    }
}
