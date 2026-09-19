using HRM.Models;
using HRM.Models.Reporting;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Reporting.Reports;

// Overtime totals per month for a year, from the payroll OT rows (HrwOt).
// Sums the OT amount and OT minutes (shown as hours) and counts the rows.
public class OvertimeSummaryReport(IDbContextFactory<HRMContext> dbFactory) : IReportDefinition, IReportDynamicOptions
{
    private static readonly string[] ThaiMonths =
        { "มกราคม","กุมภาพันธ์","มีนาคม","เมษายน","พฤษภาคม","มิถุนายน","กรกฎาคม","สิงหาคม","กันยายน","ตุลาคม","พฤศจิกายน","ธันวาคม" };

    public string Code => "ot-summary";
    public string Category => "เวลาทำงาน & OT (Time & OT)";
    public string Name => "สรุปการทำงานล่วงเวลา (รายเดือน)";
    public string? Description => "จำนวนรายการ ชั่วโมง และเงินค่าล่วงเวลา แยกตามเดือน ในปีที่เลือก";

    public IReadOnlyList<ReportParameter> Parameters => new ReportParameter[]
    {
        new ReportParameter("year", "ปี (ค.ศ.)", ReportParamType.Year, Required: true,
            DefaultValue: DateTime.Today.Year.ToString(), HelperText: "ปีปฏิทินที่ต้องการสรุป"),
        new ReportParameter("from", "ตั้งแต่วันที่", ReportParamType.Date, HelperText: "ระบุแล้วจำกัดเฉพาะช่วงวันที่ทำงานนี้ภายในปีที่เลือก"),
        new ReportParameter("to", "ถึงวันที่", ReportParamType.Date),
        new ReportParameter("otcode", "ประเภท OT", ReportParamType.Select, HelperText: "เว้นว่าง = ทุกประเภท"),
        new ReportParameter("minhours", "OT ไม่น้อยกว่า (ชม./รายการ)", ReportParamType.Number,
            HelperText: "เว้นว่าง = ทุกรายการ"),
    }.Concat(ReportCriteria.Standard()).ToList();

    public async Task<IReadOnlyList<ReportParamOption>> GetOptionsAsync(string parameterKey, ReportContext ctx, CancellationToken ct = default)
    {
        if (parameterKey != "otcode") return Array.Empty<ReportParamOption>();
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var codes = await context.HrwOts
            .Where(o => o.companyid == ctx.CompanyId && o.OtCode != null && o.OtCode != "")
            .Select(o => o.OtCode!).Distinct().OrderBy(c => c)
            .ToListAsync(ct);
        return new[] { new ReportParamOption("", "ทั้งหมด") }.Concat(codes.Select(c => new ReportParamOption(c, c))).ToList();
    }

    private static DateTime? ParseDate(IReadOnlyDictionary<string, string?> args, string key)
        => ReportCriteria.Arg(args, key) is { } s
           && DateTime.TryParse(s, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var d) ? d.Date : null;

    public async Task<ReportResult> RunAsync(IReadOnlyDictionary<string, string?> args, ReportContext ctx, CancellationToken ct = default)
    {
        var year = args.TryGetValue("year", out var y) && int.TryParse(y, out var yy) ? yy : DateTime.Today.Year;

        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var q = context.HrwOts
            .Where(o => o.companyid == ctx.CompanyId && o.DateWork != null && o.DateWork!.Value.Year == year);

        if (ParseDate(args, "from") is { } dFrom) q = q.Where(o => o.DateWork >= dFrom);
        if (ParseDate(args, "to") is { } to) q = q.Where(o => o.DateWork < to.AddDays(1));
        var otCode = ReportCriteria.Arg(args, "otcode");
        if (otCode is not null) q = q.Where(o => o.OtCode == otCode);
        decimal? minHours = ReportCriteria.Arg(args, "minhours") is { } mh
            && decimal.TryParse(mh, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var mhv) && mhv > 0 ? mhv : null;
        if (minHours is { } minH) { var minMinutes = minH * 60m; q = q.Where(o => (o.OtPMinute ?? 0m) >= minMinutes); }

        if (ReportCriteria.Arg(args, ReportCriteria.DeptKey) is not null || ReportCriteria.Arg(args, ReportCriteria.EmpTypeKey) is not null)
        {
            var empNos = (await ReportCriteria.ApplyEmployeeAsync(context,
                context.Hremployee.Where(e => e.companyid == ctx.CompanyId), args, ct)).Select(e => e.EmpNo);
            q = q.Where(o => empNos.Contains(o.EmpNo));
        }

        var rows = await q
            .Select(o => new { o.DateWork, o.OtAmt, o.OtPMinute })
            .ToListAsync(ct);

        var criteria = await ReportCriteria.DescribeAsync(context, ctx.CompanyId, args, ct);
        var extra = new List<string>();
        if (ParseDate(args, "from") is not null || ParseDate(args, "to") is not null)
            extra.Add($"วันที่ {(ParseDate(args, "from") is { } a ? a.ToString("dd/MM/yyyy") : "…")} - {(ParseDate(args, "to") is { } b ? b.ToString("dd/MM/yyyy") : "…")}");
        if (otCode is not null) extra.Add($"ประเภท OT {otCode}");
        if (minHours is not null) extra.Add($"OT ≥ {minHours:0.##} ชม./รายการ");
        if (criteria is not null) extra.Add(criteria);

        var byMonth = rows
            .GroupBy(o => o.DateWork!.Value.Month)
            .Select(g => new
            {
                Month = g.Key,
                Count = g.Count(),
                Minutes = g.Sum(x => x.OtPMinute ?? 0m),
                Amount = g.Sum(x => x.OtAmt ?? 0m),
            })
            .OrderBy(x => x.Month)
            .ToList();

        var reportRows = byMonth.Select(m => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
        {
            ["month"] = ThaiMonths[m.Month - 1],
            ["count"] = m.Count,
            ["hours"] = Math.Round(m.Minutes / 60m, 1),
            ["amount"] = m.Amount,
        }).ToList();

        var totals = new Dictionary<string, object?>
        {
            ["month"] = "รวมทั้งปี",
            ["count"] = byMonth.Sum(m => m.Count),
            ["hours"] = Math.Round(byMonth.Sum(m => m.Minutes) / 60m, 1),
            ["amount"] = byMonth.Sum(m => m.Amount),
        };

        return new ReportResult(
            $"สรุปการทำงานล่วงเวลา — ปี {year}",
            new[]
            {
                new ReportColumn("month", "เดือน"),
                new ReportColumn("count", "จำนวนรายการ", ReportColumnType.Number),
                new ReportColumn("hours", "ชั่วโมง OT รวม", ReportColumnType.Number),
                new ReportColumn("amount", "เงิน OT รวม", ReportColumnType.Money),
            },
            reportRows, totals,
            Subtitle: $"บริษัท {ctx.CompanyId}" + (extra.Count == 0 ? "" : " · " + string.Join(" · ", extra)));
    }
}
