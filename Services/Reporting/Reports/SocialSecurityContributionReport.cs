using HRM.Models;
using HRM.Models.Reporting;
using HRM.Services.Pay;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Reporting.Reports;

// สปส.1-10 (ส่วนที่ 2) — the monthly employer filing to the Social Security
// Office: one row per employee with the contribution wage base, employee
// contribution and employer contribution, with totals. Customer spec REQ-125
// / REQ-218. Lives in the Report Center so it renders online and exports to
// Excel/PDF/CSV/Word without a dedicated page.
//
// Wage base is reconstructed from the SSO deduction and the company's rate
// (Hrucfsecurity code 01): the engine already capped it (15,000 by default),
// so amount ÷ rate gives the capped base the SSO form expects. The employer
// contribution uses the employer rate when configured, else mirrors the
// employee rate (Thai law: equal 5% / 5% today).
public class SocialSecurityContributionReport(IDbContextFactory<HRMContext> dbFactory)
    : IReportDefinition, IReportDynamicOptions
{
    public string Code => "sso-contribution";
    public string Category => "เงินเดือน / GL (Payroll)";
    public string Name => "รายงานเงินสมทบประกันสังคม (สปส.1-10)";
    public string? Description => "รายชื่อผู้ประกันตน ค่าจ้างที่นำส่ง เงินสมทบผู้ประกันตนและนายจ้าง ของงวดที่เลือก";

    public IReadOnlyList<ReportParameter> Parameters => new[]
    {
        new ReportParameter("run", "งวดเงินเดือน", ReportParamType.Select, Required: true),
    };

    public async Task<IReadOnlyList<ReportParamOption>> GetOptionsAsync(string parameterKey, ReportContext ctx, CancellationToken ct = default)
    {
        if (parameterKey != "run") return Array.Empty<ReportParamOption>();
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        return await context.Pay_PayrollRuns
            .Where(r => r.CompanyId == ctx.CompanyId && r.RunType == PayrollRunType.Regular)
            .OrderByDescending(r => r.PeriodStart)
            .Select(r => new ReportParamOption(r.Id.ToString(), r.PayrollPeriod + (r.Status >= PayrollRunStatus.Posted ? " (บันทึกบัญชีแล้ว)" : "")))
            .ToListAsync(ct);
    }

    public async Task<ReportResult> RunAsync(IReadOnlyDictionary<string, string?> args, ReportContext ctx, CancellationToken ct = default)
    {
        if (!args.TryGetValue("run", out var runStr) || !long.TryParse(runStr, out var runId))
            throw new InvalidOperationException("กรุณาเลือกงวดเงินเดือน");

        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var run = await context.Pay_PayrollRuns.FirstOrDefaultAsync(r => r.Id == runId && r.CompanyId == ctx.CompanyId, ct)
            ?? throw new InvalidOperationException("ไม่พบงวดเงินเดือนนี้");

        var rate = await context.Hrucfsecuritys
            .Where(x => x.companyid == run.CompanyId && x.SecurityCode == HrucfsecurityRateProvider.CurrentEmployeeSecurityCode)
            .Select(x => new { x.PercenSecurity, x.PercenmgSecurity })
            .FirstOrDefaultAsync(ct);
        var employeeRate = (rate?.PercenSecurity ?? 5m) / 100m;
        var employerRate = ((rate?.PercenmgSecurity is > 0 ? rate.PercenmgSecurity.Value : rate?.PercenSecurity) ?? 5m) / 100m;

        var settings = await context.Pay_PayslipSettings.FirstOrDefaultAsync(s => s.CompanyId == run.CompanyId, ct);

        var lines = await context.Pay_PayrollEmployees
            .Where(e => e.PayrollRunId == runId && !e.IsExcluded && e.SocialSecurityAmount > 0)
            .Select(e => new
            {
                e.EmpNo,
                e.Hremployee.EmpName, e.Hremployee.EmpSurname, e.Hremployee.IdCard,
                e.SocialSecurityAmount,
            })
            .OrderBy(e => e.EmpNo)
            .ToListAsync(ct);

        var rows = new List<IReadOnlyDictionary<string, object?>>();
        decimal totalBase = 0, totalEmp = 0, totalEr = 0;
        var seq = 0;
        foreach (var l in lines)
        {
            var wageBase = employeeRate > 0 ? Math.Round(l.SocialSecurityAmount / employeeRate, 2, MidpointRounding.AwayFromZero) : 0m;
            var employer = Math.Round(wageBase * employerRate, 2, MidpointRounding.AwayFromZero);
            totalBase += wageBase; totalEmp += l.SocialSecurityAmount; totalEr += employer;
            rows.Add(new Dictionary<string, object?>
            {
                ["seq"] = ++seq,
                ["idcard"] = l.IdCard,
                ["empno"] = l.EmpNo,
                ["name"] = $"{l.EmpName} {l.EmpSurname}".Trim(),
                ["wage"] = wageBase,
                ["employee"] = l.SocialSecurityAmount,
                ["employer"] = employer,
                ["total"] = l.SocialSecurityAmount + employer,
            });
        }

        var totals = new Dictionary<string, object?>
        {
            ["name"] = $"รวม {lines.Count} คน",
            ["wage"] = totalBase,
            ["employee"] = totalEmp,
            ["employer"] = totalEr,
            ["total"] = totalEmp + totalEr,
        };

        var employer_ = settings?.CompanyName ?? run.CompanyId;
        return new ReportResult(
            $"สปส.1-10 เงินสมทบประกันสังคม — งวด {run.PayrollPeriod}",
            new[]
            {
                new ReportColumn("seq", "ลำดับ", ReportColumnType.Number),
                new ReportColumn("idcard", "เลขประจำตัวประชาชน"),
                new ReportColumn("empno", "รหัสพนักงาน"),
                new ReportColumn("name", "ชื่อ - สกุล ผู้ประกันตน"),
                new ReportColumn("wage", "ค่าจ้างที่นำส่ง", ReportColumnType.Money),
                new ReportColumn("employee", "เงินสมทบผู้ประกันตน", ReportColumnType.Money),
                new ReportColumn("employer", "เงินสมทบนายจ้าง", ReportColumnType.Money),
                new ReportColumn("total", "รวมนำส่ง", ReportColumnType.Money),
            },
            rows, Totals: totals,
            Subtitle: $"นายจ้าง {employer_} · เลขประจำตัวผู้เสียภาษี {settings?.CompanyTaxId ?? "-"} · ค่าจ้างเดือน {run.PeriodStart:MM/yyyy} · อัตราผู้ประกันตน {employeeRate:P2} นายจ้าง {employerRate:P2}");
    }
}
