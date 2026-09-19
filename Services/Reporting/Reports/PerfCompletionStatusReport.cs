using HRM.Models;
using HRM.Models.Reporting;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Reporting.Reports;

// Progress of the evaluation cycle: how many evaluation instances sit in each
// status for a chosen period. Demonstrates a data-driven parameter (the period
// dropdown), and always shows all six statuses in a fixed order.
public class PerfCompletionStatusReport(IDbContextFactory<HRMContext> dbFactory)
    : IReportDefinition, IReportDynamicOptions
{
    public string Code => "perf-completion-status";
    public string Category => "ประเมินผล (Performance)";
    public string Name => "สถานะความคืบหน้าการประเมิน";
    public string? Description => "จำนวนแบบประเมินแยกตามสถานะ ในรอบที่เลือก";

    public IReadOnlyList<ReportParameter> Parameters => new[]
    {
        new ReportParameter("period", "รอบการประเมิน", ReportParamType.Period, Required: true,
            HelperText: "เลือกรอบที่ต้องการดูสถานะการประเมิน"),
        new ReportParameter("evaltype", "ประเภทแบบประเมิน", ReportParamType.Select,
            HelperText: "เว้นว่าง = ทุกประเภท"),
    }.Concat(ReportCriteria.Standard()).ToList();

    public async Task<IReadOnlyList<ReportParamOption>> GetOptionsAsync(string parameterKey, ReportContext ctx, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        if (parameterKey == "evaltype")
        {
            var types = await context.Perf_EvaluationTypes
                .Where(t => t.CompanyId == ctx.CompanyId)
                .OrderBy(t => t.Code)
                .Select(t => new ReportParamOption(t.Id.ToString(), t.Code + " — " + t.Name))
                .ToListAsync(ct);
            return new[] { new ReportParamOption("", "ทั้งหมด") }.Concat(types).ToList();
        }
        if (parameterKey != "period") return await ReportCriteria.OptionsAsync(context, parameterKey, ctx, ct);
        return await context.Perf_EvaluationPeriods
            .Where(p => p.CompanyId == ctx.CompanyId)
            .OrderByDescending(p => p.StartDate)
            .Select(p => new ReportParamOption(p.Id.ToString(), p.Name))
            .ToListAsync(ct);
    }

    private static readonly (PerfInstanceStatus Status, string Label)[] StatusOrder =
    {
        (PerfInstanceStatus.Draft, "ร่าง"),
        (PerfInstanceStatus.InProgress, "กำลังให้คะแนน"),
        (PerfInstanceStatus.PendingApproval, "รออนุมัติ"),
        (PerfInstanceStatus.Approved, "อนุมัติแล้ว"),
        (PerfInstanceStatus.Rejected, "ถูกปฏิเสธ"),
        (PerfInstanceStatus.Cancelled, "ยกเลิก"),
    };

    public async Task<ReportResult> RunAsync(IReadOnlyDictionary<string, string?> args, ReportContext ctx, CancellationToken ct = default)
    {
        if (!args.TryGetValue("period", out var periodStr) || !long.TryParse(periodStr, out var periodId))
            throw new InvalidOperationException("กรุณาเลือกรอบการประเมิน");

        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var period = await context.Perf_EvaluationPeriods.FirstOrDefaultAsync(p => p.Id == periodId, ct)
            ?? throw new InvalidOperationException("ไม่พบรอบการประเมินนี้");

        var instances = context.Perf_EvaluationInstances.Where(i => i.EvaluationPeriodId == periodId);
        if (long.TryParse(ReportCriteria.Arg(args, "evaltype"), out var evalTypeId))
            instances = instances.Where(i => i.EvaluationTypeId == evalTypeId);
        if (ReportCriteria.Arg(args, ReportCriteria.DeptKey) is not null || ReportCriteria.Arg(args, ReportCriteria.EmpTypeKey) is not null)
        {
            var emps = await ReportCriteria.ApplyEmployeeAsync(context,
                context.Hremployee.Where(e => e.companyid == ctx.CompanyId), args, ct);
            var empIds = emps.Select(e => e.id);
            instances = instances.Where(i => empIds.Contains(i.HremployeeId));
        }
        var crit = await ReportCriteria.DescribeAsync(context, ctx.CompanyId, args, ct);

        var statuses = await instances
            .Select(i => i.Status)
            .ToListAsync(ct);
        var total = statuses.Count;
        var countByStatus = statuses.GroupBy(s => s).ToDictionary(g => g.Key, g => g.Count());

        var rows = new List<IReadOnlyDictionary<string, object?>>();
        foreach (var (status, label) in StatusOrder)
        {
            var count = countByStatus.TryGetValue(status, out var c) ? c : 0;
            var pct = total == 0 ? 0m : Math.Round(count * 100m / total, 1);
            rows.Add(new Dictionary<string, object?>
            {
                ["status"] = label,
                ["count"] = count,
                ["pct"] = pct,
            });
        }

        var totals = new Dictionary<string, object?>
        {
            ["status"] = "รวม",
            ["count"] = total,
            ["pct"] = 100m,
        };

        return new ReportResult(
            $"สถานะการประเมิน — {period.Name}",
            new[]
            {
                new ReportColumn("status", "สถานะ"),
                new ReportColumn("count", "จำนวน", ReportColumnType.Number),
                new ReportColumn("pct", "สัดส่วน", ReportColumnType.Percent),
            },
            rows, totals,
            Subtitle: $"แบบประเมินทั้งหมด {total} รายการ · ณ {DateTime.Now:dd/MM/yyyy}"
                + (crit is null ? "" : $" · {crit}"));
    }
}
