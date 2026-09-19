using HRM.Models;
using HRM.Models.Reporting;
using HRM.Services.Audit;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Reporting.Reports;

// Employees the employer must report to the Social Security Office in the period:
// new hires (สปส.1-03 ขึ้นทะเบียนผู้ประกันตน, by WORK_DATE) and leavers (สปส.6-09 แจ้งออก,
// by RESIGN_DATE). Hremployee has no SSO-number column, so this lists what exists:
// SS_APPDATE (application date) and SS_STATUS (raw flag) beside the hire/resign date.
public class SocialSecurityRegistrationReport(IDbContextFactory<HRMContext> dbFactory, IAuditLogger audit) : IReportDefinition
{
    public string Code => "sso-registration";
    public string Category => "เงินเดือน / GL (Payroll)";
    public string Name => "ผู้ประกันตนเข้าใหม่/ออก (สปส.1-03 / สปส.6-09)";
    public string? Description => "รายชื่อพนักงานที่ต้องแจ้งขึ้นทะเบียนผู้ประกันตน (เข้าใหม่) หรือแจ้งออกจากงาน ในช่วงวันที่ที่เลือก";

    private static readonly IReadOnlyList<ReportParamOption> KindOptions = new[]
    {
        new ReportParamOption("both", "เข้าใหม่และออก"),
        new ReportParamOption("new", "เข้าใหม่ (สปส.1-03)"),
        new ReportParamOption("exit", "ออกจากงาน (สปส.6-09)"),
    };

    public IReadOnlyList<ReportParameter> Parameters => new[]
    {
        new ReportParameter("from", "ตั้งแต่วันที่", ReportParamType.Date, Required: true,
            DefaultValue: new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).ToString("yyyy-MM-dd")),
        new ReportParameter("to", "ถึงวันที่", ReportParamType.Date, Required: true,
            DefaultValue: DateTime.Today.ToString("yyyy-MM-dd")),
        new ReportParameter("kind", "ประเภทรายการ", ReportParamType.Select, DefaultValue: "both", Options: KindOptions),
    }.Concat(ReportCriteria.Standard()).ToList();

    public async Task<ReportResult> RunAsync(IReadOnlyDictionary<string, string?> args, ReportContext ctx, CancellationToken ct = default)
    {
        var from = args.TryGetValue("from", out var f) && DateTime.TryParse(f, out var ff) ? ff.Date : new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var to = args.TryGetValue("to", out var t) && DateTime.TryParse(t, out var tt) ? tt.Date : DateTime.Today;
        var toEnd = to.AddDays(1);
        var kind = ReportCriteria.Arg(args, "kind") ?? "both";
        var wantNew = kind is "both" or "new";
        var wantExit = kind is "both" or "exit";

        await using var context = await dbFactory.CreateDbContextAsync(ct);
        await audit.LogAccessAsync("Report:sso-registration", ctx.CompanyId, isSensitive: true, note: "employee list for SSO filing", ct: ct);

        var query = context.Hremployee.Where(e => e.companyid == ctx.CompanyId);
        query = await ReportCriteria.ApplyEmployeeAsync(context, query, args, ct);
        var emps = await query
            .Where(e => (e.WorkDate != null && e.WorkDate >= from && e.WorkDate < toEnd)
                     || (e.ResignDate != null && e.ResignDate >= from && e.ResignDate < toEnd))
            .Select(e => new { e.EmpNo, e.EmpName, e.EmpSurname, e.WorkDate, e.ResignDate, e.SsAppdate, e.SsStatus })
            .ToListAsync(ct);

        var items = new List<(string Kind, DateTime Date, string EmpNo, string Name, DateTime? SsApp, decimal? SsStatus)>();
        foreach (var e in emps)
        {
            var name = $"{e.EmpName} {e.EmpSurname}".Trim();
            if (wantNew && e.WorkDate is { } w && w >= from && w < toEnd)
                items.Add(("เข้าใหม่ (สปส.1-03)", w, e.EmpNo, name, e.SsAppdate, e.SsStatus));
            if (wantExit && e.ResignDate is { } r && r >= from && r < toEnd)
                items.Add(("ออกจากงาน (สปส.6-09)", r, e.EmpNo, name, e.SsAppdate, e.SsStatus));
        }

        var rows = items.OrderBy(i => i.Kind).ThenBy(i => i.Date).ThenBy(i => i.EmpNo)
            .Select(i => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
            {
                ["kind"] = i.Kind,
                ["date"] = i.Date,
                ["empno"] = i.EmpNo,
                ["name"] = i.Name,
                ["ssapp"] = i.SsApp,
                ["ssstatus"] = i.SsStatus,
            }).ToList();

        var newCount = items.Count(i => i.Kind.StartsWith("เข้า"));
        var exitCount = items.Count - newCount;
        var totals = new Dictionary<string, object?>
        {
            ["kind"] = "รวม",
            ["date"] = null,
            ["empno"] = $"เข้าใหม่ {newCount:N0}",
            ["name"] = $"ออก {exitCount:N0}",
        };

        var crit = await ReportCriteria.DescribeAsync(context, ctx.CompanyId, args, ct);
        return new ReportResult(
            "ผู้ประกันตนเข้าใหม่/ออก (สปส.1-03 / สปส.6-09)",
            new[]
            {
                new ReportColumn("kind", "รายการ"),
                new ReportColumn("date", "วันที่เข้า/ออก", ReportColumnType.Date),
                new ReportColumn("empno", "รหัสพนักงาน"),
                new ReportColumn("name", "ชื่อ-สกุล"),
                new ReportColumn("ssapp", "วันที่สมัครประกันสังคม (SS_APPDATE)", ReportColumnType.Date),
                new ReportColumn("ssstatus", "สถานะประกันสังคม (SS_STATUS)", ReportColumnType.Number),
            },
            rows, totals,
            $"บริษัท {ctx.CompanyId} · {from:dd/MM/yyyy} – {to:dd/MM/yyyy}" + (crit is null ? "" : " · " + crit));
    }
}
