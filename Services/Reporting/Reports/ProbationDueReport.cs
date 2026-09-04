using HRM.Models;
using HRM.Models.Reporting;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Reporting.Reports;

// Active employees whose probation period ends within a configurable window and
// who have not yet been confirmed as passing probation — the HR follow-up list.
public class ProbationDueReport(IDbContextFactory<HRMContext> dbFactory) : IReportDefinition
{
    public string Code => "probation-due";
    public string Category => "การจ้างงาน (Employment)";
    public string Name => "พนักงานที่ครบกำหนดทดลองงาน";
    public string? Description => "พนักงานที่ ProbationEndDate ใกล้ถึงและยังไม่ยืนยันผ่านทดลองงาน";

    public IReadOnlyList<ReportParameter> Parameters => new[]
    {
        new ReportParameter("days", "ภายในกี่วันข้างหน้า", ReportParamType.Number,
            DefaultValue: "30", HelperText: "ค่าเริ่มต้น 30 วัน"),
    };

    public async Task<ReportResult> RunAsync(IReadOnlyDictionary<string, string?> args, ReportContext ctx, CancellationToken ct = default)
    {
        var days = 30;
        if (args.TryGetValue("days", out var daysStr) && int.TryParse(daysStr, out var d) && d > 0)
            days = d;

        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var today = DateTime.Today;
        var until = today.AddDays(days);

        var emps = await context.Hremployee
            .Where(e => e.companyid == ctx.CompanyId && e.ResignDate == null
                && e.ProbationEndDate != null && e.ProbationConfirmedDate == null
                && e.ProbationEndDate >= today && e.ProbationEndDate <= until)
            .Select(e => new { e.EmpNo, e.EmpName, e.EmpSurname, e.DeptgrpCode, e.ProbationEndDate })
            .ToListAsync(ct);

        var ordered = emps.OrderBy(e => e.ProbationEndDate).ToList();

        var rows = ordered.Select(e => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
        {
            ["empno"] = e.EmpNo,
            ["name"] = $"{e.EmpName} {e.EmpSurname}".Trim(),
            ["dept"] = e.DeptgrpCode ?? "—",
            ["probationEnd"] = e.ProbationEndDate,
        }).ToList();

        var totals = new Dictionary<string, object?> { ["empno"] = "รวม", ["name"] = $"{ordered.Count} คน" };

        return new ReportResult(
            "พนักงานที่ครบกำหนดทดลองงาน",
            new[]
            {
                new ReportColumn("empno", "รหัส"),
                new ReportColumn("name", "ชื่อ-สกุล"),
                new ReportColumn("dept", "หน่วยงาน"),
                new ReportColumn("probationEnd", "วันครบทดลองงาน", ReportColumnType.Date),
            },
            rows, totals,
            Subtitle: $"ครบกำหนดภายใน {days} วัน · {today:dd/MM/yyyy} – {until:dd/MM/yyyy}");
    }
}
