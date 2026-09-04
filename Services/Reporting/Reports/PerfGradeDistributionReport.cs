using HRM.Models;
using HRM.Models.Reporting;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Reporting.Reports;

// Final grade distribution for an evaluation period vs the configured bell-curve
// target — the reporting view of the same numbers the calibration/HR-dashboard
// panels show. Demonstrates a data-driven parameter (the period dropdown).
public class PerfGradeDistributionReport(IDbContextFactory<HRMContext> dbFactory)
    : IReportDefinition, IReportDynamicOptions
{
    public string Code => "perf-grade-distribution";
    public string Category => "ประเมินผล (Performance)";
    public string Name => "การกระจายเกรดตามรอบประเมิน";
    public string? Description => "สัดส่วนจริงของแต่ละเกรดเทียบกับเป้าหมาย (bell curve) ในรอบที่เลือก";

    public IReadOnlyList<ReportParameter> Parameters => new[]
    {
        new ReportParameter("period", "รอบการประเมิน", ReportParamType.Period, Required: true,
            HelperText: "เลือกรอบที่ต้องการดูการกระจายเกรด"),
    };

    public async Task<IReadOnlyList<ReportParamOption>> GetOptionsAsync(string parameterKey, ReportContext ctx, CancellationToken ct = default)
    {
        if (parameterKey != "period") return Array.Empty<ReportParamOption>();
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        return await context.Perf_EvaluationPeriods
            .Where(p => p.CompanyId == ctx.CompanyId)
            .OrderByDescending(p => p.StartDate)
            .Select(p => new ReportParamOption(p.Id.ToString(), p.Name))
            .ToListAsync(ct);
    }

    public async Task<ReportResult> RunAsync(IReadOnlyDictionary<string, string?> args, ReportContext ctx, CancellationToken ct = default)
    {
        if (!args.TryGetValue("period", out var periodStr) || !long.TryParse(periodStr, out var periodId))
            throw new InvalidOperationException("กรุณาเลือกรอบการประเมิน");

        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var period = await context.Perf_EvaluationPeriods.FirstOrDefaultAsync(p => p.Id == periodId, ct)
            ?? throw new InvalidOperationException("ไม่พบรอบการประเมินนี้");

        var graded = await context.Perf_EvaluationInstances
            .Where(i => i.EvaluationPeriodId == periodId && i.FinalGrade != null)
            .Select(i => i.FinalGrade!)
            .ToListAsync(ct);
        var total = graded.Count;
        var countByGrade = graded.GroupBy(g => g).ToDictionary(g => g.Key, g => g.Count());

        var bands = await context.Perf_GradeBands
            .Where(b => b.EvaluationPeriodId == periodId && b.IsActive)
            .OrderBy(b => b.SortOrder).ToListAsync(ct);

        var rows = new List<IReadOnlyDictionary<string, object?>>();
        foreach (var b in bands)
        {
            var actual = countByGrade.TryGetValue(b.Grade, out var c) ? c : 0;
            var actualPct = total == 0 ? 0m : Math.Round(actual * 100m / total, 1);
            var variance = b.TargetDistributionPercent is decimal t ? Math.Round(actualPct - t, 1) : (decimal?)null;
            rows.Add(new Dictionary<string, object?>
            {
                ["grade"] = b.Grade,
                ["count"] = actual,
                ["actual"] = actualPct,
                ["target"] = b.TargetDistributionPercent,
                ["variance"] = variance is null ? "—" : (variance > 0 ? $"เกิน +{variance:0.#}" : variance < 0 ? $"ขาด {variance:0.#}" : "ตรงเป้า"),
            });
        }

        var totals = new Dictionary<string, object?> { ["grade"] = "รวม", ["count"] = total, ["actual"] = total == 0 ? 0m : 100m };

        return new ReportResult(
            $"การกระจายเกรด — {period.Name}",
            new[]
            {
                new ReportColumn("grade", "เกรด"),
                new ReportColumn("count", "จำนวน (คน)", ReportColumnType.Number),
                new ReportColumn("actual", "สัดส่วนจริง", ReportColumnType.Percent),
                new ReportColumn("target", "เป้าหมาย", ReportColumnType.Percent),
                new ReportColumn("variance", "ผลต่าง"),
            },
            rows, totals,
            Subtitle: $"ผู้ได้รับเกรดทั้งหมด {total} คน · ณ {DateTime.Now:dd/MM/yyyy}");
    }
}
