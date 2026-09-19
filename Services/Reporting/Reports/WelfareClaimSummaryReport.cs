using HRM.Models;
using HRM.Models.Reporting;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Reporting.Reports;

// Welfare claims (Wel_Claim) for a chosen year, grouped by benefit type
// (Wel_BenefitType) with count + total amount. The type name is resolved from
// the welfare catalog via a code→name lookup dictionary, same pattern as
// HeadcountByEmploymentTypeReport.
//
// Wel_Claim has NO status column of its own — the live approval status is read
// back from job_master (JobMasterId) via the lazy apply-on-read pattern, so
// there is no approved/paid flag to filter on here. This report therefore
// counts ALL claims whose EventDate falls in the selected year (submitted
// drafts included), which is noted in the subtitle.
public class WelfareClaimSummaryReport(IDbContextFactory<HRMContext> dbFactory) : IReportDefinition, IReportDynamicOptions
{
    public string Code => "welfare-claim-summary";
    public string Category => "สวัสดิการ (Welfare)";
    public string Name => "สรุปการเบิกสวัสดิการตามประเภท";
    public string? Description => "จำนวนและยอดเงินการเบิกสวัสดิการ แยกตามประเภท ในปีที่เลือก";

    // Claim status is the workflow job's status (job_master.status), read here
    // by joining JobMasterId; "draft" = never entered the workflow.
    private static readonly IReadOnlyList<ReportParamOption> StatusOptions = new[]
    {
        new ReportParamOption("", "ทุกสถานะ"),
        new ReportParamOption("draft", "ยังไม่ส่งอนุมัติ"),
        new ReportParamOption("pending", "รออนุมัติ / ส่งกลับแก้ไข"),
        new ReportParamOption("completed", "อนุมัติแล้ว (ปิดงาน)"),
        new ReportParamOption("rejected", "ไม่อนุมัติ"),
        new ReportParamOption("cancelled", "ยกเลิก"),
    };

    public IReadOnlyList<ReportParameter> Parameters => new ReportParameter[]
    {
        new ReportParameter("year", "ปี (ค.ศ.)", ReportParamType.Year, Required: true,
            DefaultValue: DateTime.Now.Year.ToString()),
        new ReportParameter("from", "ตั้งแต่วันที่", ReportParamType.Date, HelperText: "ระบุแล้วจำกัดเฉพาะช่วงวันที่เกิดเหตุนี้ภายในปีที่เลือก"),
        new ReportParameter("to", "ถึงวันที่", ReportParamType.Date),
        new ReportParameter("benefit", "ประเภทสวัสดิการ", ReportParamType.Select, HelperText: "เว้นว่าง = ทุกประเภท"),
        new ReportParameter("claimstatus", "สถานะการเบิก", ReportParamType.Select, Options: StatusOptions,
            HelperText: "เว้นว่าง = นับทุกรายการ"),
    }.Concat(ReportCriteria.Standard()).ToList();

    public async Task<IReadOnlyList<ReportParamOption>> GetOptionsAsync(string parameterKey, ReportContext ctx, CancellationToken ct = default)
    {
        if (parameterKey != "benefit") return Array.Empty<ReportParamOption>();
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var types = await context.Wel_BenefitTypes
            .Where(t => t.CompanyId == ctx.CompanyId)
            .OrderBy(t => t.NameTh)
            .Select(t => new ReportParamOption(t.Id.ToString(), t.NameTh ?? t.Id.ToString()))
            .ToListAsync(ct);
        return new[] { new ReportParamOption("", "ทั้งหมด") }.Concat(types).ToList();
    }

    private static DateOnly? ParseDate(IReadOnlyDictionary<string, string?> args, string key)
        => ReportCriteria.Arg(args, key) is { } s
           && DateOnly.TryParse(s, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var d) ? d : null;

    public async Task<ReportResult> RunAsync(IReadOnlyDictionary<string, string?> args, ReportContext ctx, CancellationToken ct = default)
    {
        var year = args.TryGetValue("year", out var yStr) && int.TryParse(yStr, out var y)
            ? y : DateTime.Now.Year;

        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var q = context.Wel_Claims
            .Where(c => c.CompanyId == ctx.CompanyId && c.EventDate.Year == year);

        var dFrom = ParseDate(args, "from");
        var dTo = ParseDate(args, "to");
        if (dFrom is { } f) q = q.Where(c => c.EventDate >= f);
        if (dTo is { } t) q = q.Where(c => c.EventDate <= t);

        var benefitFilter = long.TryParse(ReportCriteria.Arg(args, "benefit"), out var bid) ? bid : (long?)null;
        if (benefitFilter is { } bf) q = q.Where(c => c.BenefitTypeId == bf);

        var statusKey = ReportCriteria.Arg(args, "claimstatus");
        if (statusKey == "draft")
            q = q.Where(c => c.JobMasterId == null);
        else if (statusKey is "pending" or "completed" or "rejected" or "cancelled")
        {
            var statuses = statusKey switch
            {
                "pending" => new[] { "PENDING", "RETURNED" },
                "completed" => new[] { "COMPLETED" },
                "rejected" => new[] { "REJECTED" },
                _ => new[] { "CANCELLED" },
            };
            var jobIds = context.job_masters.Where(j => j.status != null && statuses.Contains(j.status)).Select(j => j.jobmasterid);
            q = q.Where(c => c.JobMasterId != null && jobIds.Contains(c.JobMasterId.Value));
        }

        if (ReportCriteria.Arg(args, ReportCriteria.DeptKey) is not null || ReportCriteria.Arg(args, ReportCriteria.EmpTypeKey) is not null)
        {
            var empIds = (await ReportCriteria.ApplyEmployeeAsync(context,
                context.Hremployee.Where(e => e.companyid == ctx.CompanyId), args, ct)).Select(e => e.id);
            q = q.Where(c => empIds.Contains(c.HremployeeId));
        }

        var claims = await q
            .Select(c => new { c.BenefitTypeId, c.Amount })
            .ToListAsync(ct);

        var extra = new List<string>();
        if (dFrom is not null || dTo is not null)
            extra.Add($"วันที่เกิดเหตุ {(dFrom is { } a ? a.ToString("dd/MM/yyyy") : "…")} - {(dTo is { } b ? b.ToString("dd/MM/yyyy") : "…")}");
        if (statusKey is not null) extra.Add(StatusOptions.FirstOrDefault(o => o.Value == statusKey)?.Label ?? statusKey);
        if (await ReportCriteria.DescribeAsync(context, ctx.CompanyId, args, ct) is { } criteria) extra.Add(criteria);

        var typeNames = await context.Wel_BenefitTypes
            .Where(t => t.CompanyId == ctx.CompanyId)
            .Select(t => new { t.Id, t.NameTh })
            .ToListAsync(ct);
        var nameById = typeNames
            .GroupBy(t => t.Id).ToDictionary(g => g.Key, g => g.First().NameTh ?? "—");

        var grouped = claims
            .GroupBy(c => c.BenefitTypeId)
            .Select(g => new
            {
                Name = nameById.TryGetValue(g.Key, out var n) ? n : "(ไม่ระบุประเภท)",
                Count = g.Count(),
                Amount = g.Sum(x => x.Amount),
            })
            .OrderByDescending(x => x.Amount)
            .ToList();

        var rows = grouped.Select(g => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
        {
            ["type"] = g.Name,
            ["count"] = g.Count,
            ["amount"] = g.Amount,
        }).ToList();

        var totals = new Dictionary<string, object?>
        {
            ["type"] = "รวมทั้งหมด",
            ["count"] = grouped.Sum(g => g.Count),
            ["amount"] = grouped.Sum(g => g.Amount),
        };

        return new ReportResult(
            $"สรุปการเบิกสวัสดิการตามประเภท — ปี {year}",
            new[]
            {
                new ReportColumn("type", "ประเภทสวัสดิการ"),
                new ReportColumn("count", "จำนวนรายการ", ReportColumnType.Number),
                new ReportColumn("amount", "ยอดเบิกรวม", ReportColumnType.Money),
            },
            rows, totals,
            Subtitle: $"บริษัท {ctx.CompanyId} · "
                + (statusKey is null ? "นับทุกรายการ (ไม่มีคอลัมน์สถานะใน Wel_Claim — สถานะอนุมัติอ่านจาก job_master)" : "สถานะอ่านจาก job_master")
                + (extra.Count == 0 ? "" : " · " + string.Join(" · ", extra))
                + $" · ณ {DateTime.Now:dd/MM/yyyy}");
    }
}
