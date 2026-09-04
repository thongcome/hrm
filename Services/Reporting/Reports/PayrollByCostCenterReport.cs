using HRM.Models;
using HRM.Models.Reporting;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Reporting.Reports;

// Payroll totals grouped by cost center — the GL slice of the report set (the
// client's "reports split by GL / org structure" ask). Parameter is the
// payroll run, resolved dynamically from the company's runs.
public class PayrollByCostCenterReport(IDbContextFactory<HRMContext> dbFactory)
    : IReportDefinition, IReportDynamicOptions
{
    public string Code => "payroll-by-cost-center";
    public string Category => "เงินเดือน / GL (Payroll)";
    public string Name => "สรุปเงินเดือนตามศูนย์ต้นทุน (Cost Center)";
    public string? Description => "รวมเงินได้ / ภาษี / เงินสุทธิ แยกตามศูนย์ต้นทุน สำหรับงวดที่เลือก";

    public IReadOnlyList<ReportParameter> Parameters => new[]
    {
        new ReportParameter("run", "งวดเงินเดือน", ReportParamType.Select, Required: true,
            HelperText: "เลือกงวดที่คำนวณเงินเดือนแล้ว"),
    };

    public async Task<IReadOnlyList<ReportParamOption>> GetOptionsAsync(string parameterKey, ReportContext ctx, CancellationToken ct = default)
    {
        if (parameterKey != "run") return Array.Empty<ReportParamOption>();
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        return await context.Pay_PayrollRuns
            .Where(r => r.CompanyId == ctx.CompanyId)
            .OrderByDescending(r => r.PeriodStart)
            .Select(r => new ReportParamOption(r.Id.ToString(), r.PayrollPeriod + " (" + r.PeriodStart + " - " + r.PeriodEnd + ")"))
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
            .Select(e => new { e.CostCenterCode, e.GrossEarnings, e.TaxAmount, e.NetPay })
            .ToListAsync(ct);

        var grouped = lines
            .GroupBy(e => string.IsNullOrWhiteSpace(e.CostCenterCode) ? "(ไม่ระบุศูนย์ต้นทุน)" : e.CostCenterCode!)
            .Select(g => new
            {
                CostCenter = g.Key,
                Emp = g.Count(),
                Gross = g.Sum(x => x.GrossEarnings),
                Tax = g.Sum(x => x.TaxAmount),
                Net = g.Sum(x => x.NetPay),
            })
            .OrderByDescending(x => x.Net)
            .ToList();

        var rows = grouped.Select(g => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
        {
            ["cc"] = g.CostCenter,
            ["emp"] = g.Emp,
            ["gross"] = g.Gross,
            ["tax"] = g.Tax,
            ["net"] = g.Net,
        }).ToList();

        var totals = new Dictionary<string, object?>
        {
            ["cc"] = "รวมทั้งหมด",
            ["emp"] = grouped.Sum(g => g.Emp),
            ["gross"] = grouped.Sum(g => g.Gross),
            ["tax"] = grouped.Sum(g => g.Tax),
            ["net"] = grouped.Sum(g => g.Net),
        };

        return new ReportResult(
            $"สรุปเงินเดือนตามศูนย์ต้นทุน — {run.PayrollPeriod}",
            new[]
            {
                new ReportColumn("cc", "ศูนย์ต้นทุน (Cost Center)"),
                new ReportColumn("emp", "จำนวนพนักงาน", ReportColumnType.Number),
                new ReportColumn("gross", "เงินได้รวม", ReportColumnType.Money),
                new ReportColumn("tax", "ภาษีหัก ณ ที่จ่าย", ReportColumnType.Money),
                new ReportColumn("net", "เงินสุทธิ", ReportColumnType.Money),
            },
            rows, totals,
            Subtitle: $"งวด {run.PeriodStart:dd/MM/yyyy} - {run.PeriodEnd:dd/MM/yyyy} · บริษัท {ctx.CompanyId}");
    }
}
