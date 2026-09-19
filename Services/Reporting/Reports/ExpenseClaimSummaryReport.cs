using HRM.Models;
using HRM.Models.Reporting;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Reporting.Reports;

// Expense claims per month for a year — count and total amount. Claim approval
// status lives on the workflow job (read lazily), so this counts all claims
// filed; it's a volume/spend view, not an approved-only figure.
public class ExpenseClaimSummaryReport(IDbContextFactory<HRMContext> dbFactory) : IReportDefinition
{
    private static readonly string[] ThaiMonths =
        { "มกราคม","กุมภาพันธ์","มีนาคม","เมษายน","พฤษภาคม","มิถุนายน","กรกฎาคม","สิงหาคม","กันยายน","ตุลาคม","พฤศจิกายน","ธันวาคม" };

    public string Code => "expense-claim-summary";
    public string Category => "เบิกจ่าย (Expense & Claims)";
    public string Name => "สรุปการเบิกค่าใช้จ่าย (รายเดือน)";
    public string? Description => "จำนวนใบเบิกและยอดเงินรวม แยกตามเดือน ในปีที่เลือก";

    // Claim status is the workflow job's status (job_master.status), read by
    // joining JobMasterId; "draft" = never entered the workflow.
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
        new ReportParameter("year", "ปี (ค.ศ.)", ReportParamType.Year, Required: true, DefaultValue: DateTime.Today.Year.ToString()),
        new ReportParameter("from", "ตั้งแต่วันที่", ReportParamType.Date, HelperText: "ระบุแล้วจำกัดเฉพาะช่วงวันที่ขอเบิกนี้ภายในปีที่เลือก"),
        new ReportParameter("to", "ถึงวันที่", ReportParamType.Date),
        new ReportParameter("claimstatus", "สถานะการเบิก", ReportParamType.Select, Options: StatusOptions,
            HelperText: "เว้นว่าง = นับทุกใบเบิก"),
        new ReportParameter("minamount", "ยอดใบเบิกไม่น้อยกว่า (บาท)", ReportParamType.Number, HelperText: "เว้นว่าง = ทุกยอด"),
    }.Concat(ReportCriteria.Standard()).ToList();

    private static DateTime? ParseDate(IReadOnlyDictionary<string, string?> args, string key)
        => ReportCriteria.Arg(args, key) is { } s
           && DateTime.TryParse(s, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var d) ? d.Date : null;

    public async Task<ReportResult> RunAsync(IReadOnlyDictionary<string, string?> args, ReportContext ctx, CancellationToken ct = default)
    {
        var year = args.TryGetValue("year", out var y) && int.TryParse(y, out var yy) ? yy : DateTime.Today.Year;

        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var q = context.Exp_ClaimHeaders
            .Where(c => c.CompanyId == ctx.CompanyId && c.RequestedDate.Year == year);

        var dFrom = ParseDate(args, "from");
        var dTo = ParseDate(args, "to");
        if (dFrom is { } f) q = q.Where(c => c.RequestedDate >= f);
        if (dTo is { } t) { var end = t.AddDays(1); q = q.Where(c => c.RequestedDate < end); }

        decimal? minAmount = ReportCriteria.Arg(args, "minamount") is { } ma
            && decimal.TryParse(ma, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var mav) && mav > 0 ? mav : null;
        if (minAmount is { } minA) q = q.Where(c => c.TotalAmount >= minA);

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
            .Select(c => new { c.RequestedDate, c.TotalAmount })
            .ToListAsync(ct);

        var extra = new List<string>();
        if (dFrom is not null || dTo is not null)
            extra.Add($"วันที่ขอเบิก {(dFrom is { } a ? a.ToString("dd/MM/yyyy") : "…")} - {(dTo is { } b ? b.ToString("dd/MM/yyyy") : "…")}");
        if (statusKey is not null) extra.Add(StatusOptions.FirstOrDefault(o => o.Value == statusKey)?.Label ?? statusKey);
        if (minAmount is not null) extra.Add($"ยอดใบเบิก ≥ {minAmount:N2} บาท");
        if (await ReportCriteria.DescribeAsync(context, ctx.CompanyId, args, ct) is { } criteria) extra.Add(criteria);

        var byMonth = claims.GroupBy(c => c.RequestedDate.Month)
            .Select(g => new { Month = g.Key, Count = g.Count(), Amount = g.Sum(x => x.TotalAmount) })
            .OrderBy(x => x.Month)
            .ToList();

        var rows = byMonth.Select(m => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
        {
            ["month"] = ThaiMonths[m.Month - 1],
            ["count"] = m.Count,
            ["amount"] = m.Amount,
        }).ToList();

        var totals = new Dictionary<string, object?>
        {
            ["month"] = "รวมทั้งปี",
            ["count"] = byMonth.Sum(m => m.Count),
            ["amount"] = byMonth.Sum(m => m.Amount),
        };

        return new ReportResult(
            $"สรุปการเบิกค่าใช้จ่าย — ปี {year}",
            new[]
            {
                new ReportColumn("month", "เดือน"),
                new ReportColumn("count", "จำนวนใบเบิก", ReportColumnType.Number),
                new ReportColumn("amount", "ยอดเบิกรวม", ReportColumnType.Money),
            },
            rows, totals,
            Subtitle: $"บริษัท {ctx.CompanyId}" + (extra.Count == 0 ? "" : " · " + string.Join(" · ", extra)));
    }
}
