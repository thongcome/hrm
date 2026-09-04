using HRM.Models;
using HRM.Models.Reporting;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Reporting.Reports;

// Statutory totals for a payroll run — SSO, provident fund (employee +
// company), tax, gross and net — the figures month-end filing needs in one
// place. One metric per row so the numbers read down the page.
public class PayrollStatutorySummaryReport(IDbContextFactory<HRMContext> dbFactory)
    : IReportDefinition, IReportDynamicOptions
{
    public string Code => "payroll-statutory-summary";
    public string Category => "เงินเดือน / GL (Payroll)";
    public string Name => "สรุปยอดตามกฎหมาย (SSO/PF/ภาษี)";
    public string? Description => "รวมประกันสังคม กองทุนสำรองเลี้ยงชีพ ภาษี เงินได้และเงินสุทธิ ของงวดที่เลือก";

    public IReadOnlyList<ReportParameter> Parameters => new[]
    {
        new ReportParameter("run", "งวดเงินเดือน", ReportParamType.Select, Required: true),
    };

    public async Task<IReadOnlyList<ReportParamOption>> GetOptionsAsync(string parameterKey, ReportContext ctx, CancellationToken ct = default)
    {
        if (parameterKey != "run") return Array.Empty<ReportParamOption>();
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        return await context.Pay_PayrollRuns
            .Where(r => r.CompanyId == ctx.CompanyId)
            .OrderByDescending(r => r.PeriodStart)
            .Select(r => new ReportParamOption(r.Id.ToString(), r.PayrollPeriod))
            .ToListAsync(ct);
    }

    public async Task<ReportResult> RunAsync(IReadOnlyDictionary<string, string?> args, ReportContext ctx, CancellationToken ct = default)
    {
        if (!args.TryGetValue("run", out var runStr) || !long.TryParse(runStr, out var runId))
            throw new InvalidOperationException("กรุณาเลือกงวดเงินเดือน");

        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var run = await context.Pay_PayrollRuns.FirstOrDefaultAsync(r => r.Id == runId, ct)
            ?? throw new InvalidOperationException("ไม่พบงวดเงินเดือนนี้");

        var lines = await context.Pay_PayrollEmployees
            .Where(e => e.PayrollRunId == runId)
            .Select(e => new
            {
                e.GrossEarnings, e.NetPay, e.TaxAmount,
                e.SocialSecurityAmount, e.ProvidentFundEmployeeAmount, e.ProvidentFundCompanyAmount,
            })
            .ToListAsync(ct);

        (string label, decimal value)[] metrics =
        {
            ("จำนวนพนักงาน", lines.Count),
            ("เงินได้รวม (Gross)", lines.Sum(x => x.GrossEarnings)),
            ("ประกันสังคม (พนักงาน)", lines.Sum(x => x.SocialSecurityAmount)),
            ("กองทุนสำรองฯ (พนักงาน)", lines.Sum(x => x.ProvidentFundEmployeeAmount)),
            ("กองทุนสำรองฯ (บริษัทสมทบ)", lines.Sum(x => x.ProvidentFundCompanyAmount)),
            ("ภาษีหัก ณ ที่จ่าย", lines.Sum(x => x.TaxAmount)),
            ("เงินสุทธิจ่ายจริง (Net)", lines.Sum(x => x.NetPay)),
        };

        var rows = metrics.Select(m => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
        {
            ["metric"] = m.label,
            ["amount"] = m.value,
        }).ToList();

        return new ReportResult(
            $"สรุปยอดตามกฎหมาย — {run.PayrollPeriod}",
            new[]
            {
                new ReportColumn("metric", "รายการ"),
                new ReportColumn("amount", "ยอดรวม (บาท)", ReportColumnType.Money),
            },
            rows, Totals: null,
            Subtitle: $"งวด {run.PeriodStart:dd/MM/yyyy} - {run.PeriodEnd:dd/MM/yyyy} · บริษัท {ctx.CompanyId}");
    }
}
