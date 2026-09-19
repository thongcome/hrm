using HRM.Models;
using HRM.Models.Reporting;
using HRM.Services.Audit;
using HRM.Services.Pay;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Reporting.Reports;

// Employees whose pay rate is under the configured legal daily minimum wage on the as-of date.
// The rule itself (province row beats the all-province row, effective-date aware, salary / 30 for
// monthly staff) lives in MinimumWageRule — this report only lists what that rule flags.
// Salary data: kept in the payroll category only.
public class BelowMinimumWageReport(IDbContextFactory<HRMContext> dbFactory, IAuditLogger audit) : IReportDefinition
{
    public string Code => "below-minimum-wage";
    public string Category => "เงินเดือน / GL (Payroll)";
    public string Name => "พนักงานที่ค่าจ้างต่ำกว่าค่าจ้างขั้นต่ำ";
    public string? Description => "รายชื่อพนักงานที่ค่าจ้างต่อวัน (ค่าจ้างรายวัน หรือเงินเดือน ÷ 30) ต่ำกว่าค่าจ้างขั้นต่ำของจังหวัดที่ทำงาน ณ วันที่เลือก";

    public IReadOnlyList<ReportParameter> Parameters => new[]
    {
        new ReportParameter("asof", "ณ วันที่", ReportParamType.Date, DefaultValue: DateTime.Today.ToString("yyyy-MM-dd"),
            HelperText: "ใช้อัตราค่าจ้างขั้นต่ำที่มีผลบังคับใช้ ณ วันนี้"),
    }.Concat(ReportCriteria.Standard(statusDefault: ReportCriteria.StatusWorking)).ToList();

    public async Task<ReportResult> RunAsync(IReadOnlyDictionary<string, string?> args, ReportContext ctx, CancellationToken ct = default)
    {
        var asOfDt = args.TryGetValue("asof", out var a) && DateTime.TryParse(a, out var aa) ? aa.Date : DateTime.Today;
        var asOf = DateOnly.FromDateTime(asOfDt);

        await using var context = await dbFactory.CreateDbContextAsync(ct);
        await audit.LogAccessAsync("Report:below-minimum-wage", ctx.CompanyId, isSensitive: true, note: "compensation report", ct: ct);

        var province = await context.Pay_PayslipSettings
            .Where(s => s.CompanyId == ctx.CompanyId)
            .Select(s => s.WorkProvince)
            .FirstOrDefaultAsync(ct);
        var wageRows = await context.Pay_MinimumWages.ToListAsync(ct);
        var minimum = MinimumWageRule.DailyMinimum(wageRows, province, asOf);

        var crit = await ReportCriteria.DescribeAsync(context, ctx.CompanyId, args, ct);
        var subtitle = $"บริษัท {ctx.CompanyId} · ณ วันที่ {asOfDt:dd/MM/yyyy} · จังหวัด {(string.IsNullOrWhiteSpace(province) ? "(ไม่ได้ตั้งค่า — ใช้อัตราทุกจังหวัด)" : province)}"
            + (crit is null ? "" : " · " + crit);

        var columns = new[]
        {
            new ReportColumn("empno", "รหัสพนักงาน"),
            new ReportColumn("name", "ชื่อ-สกุล"),
            new ReportColumn("province", "จังหวัดที่ทำงาน"),
            new ReportColumn("basis", "ฐานค่าจ้าง"),
            new ReportColumn("daily", "ค่าจ้าง/วัน", ReportColumnType.Money),
            new ReportColumn("minimum", "ขั้นต่ำ/วัน", ReportColumnType.Money),
            new ReportColumn("shortfall", "ขาด/วัน", ReportColumnType.Money),
            new ReportColumn("monthly", "ขาด/เดือน (×30)", ReportColumnType.Money),
        };

        if (minimum is null)
            return new ReportResult("พนักงานที่ค่าจ้างต่ำกว่าค่าจ้างขั้นต่ำ", columns,
                new List<IReadOnlyDictionary<string, object?>>(), null,
                subtitle + " · ยังไม่มีอัตราค่าจ้างขั้นต่ำที่มีผลบังคับใช้ (ตั้งค่าที่ Pay_MinimumWage)");

        var query = context.Hremployee.Where(e => e.companyid == ctx.CompanyId);
        query = await ReportCriteria.ApplyEmployeeAsync(context, query, args, ct);
        var emps = await query
            .Select(e => new { e.EmpNo, e.EmpName, e.EmpSurname, e.SalaryAmt, e.DailyWage })
            .ToListAsync(ct);

        var min = minimum.Value;
        var rows = emps
            .Where(e => MinimumWageRule.IsBelow(e.SalaryAmt, e.DailyWage, min))
            .Select(e =>
            {
                var daily = MinimumWageRule.DailyEquivalent(e.SalaryAmt, e.DailyWage)!.Value;
                var gap = min - daily;
                return new
                {
                    e.EmpNo,
                    Name = $"{e.EmpName} {e.EmpSurname}".Trim(),
                    Basis = (e.DailyWage ?? 0m) > 0m ? "รายวัน" : "เงินเดือน ÷ 30",
                    Daily = Math.Round(daily, 2),
                    Gap = Math.Round(gap, 2),
                };
            })
            .OrderByDescending(x => x.Gap)
            .ToList();

        var reportRows = rows.Select(x => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
        {
            ["empno"] = x.EmpNo,
            ["name"] = x.Name,
            ["province"] = string.IsNullOrWhiteSpace(province) ? "ทุกจังหวัด" : province,
            ["basis"] = x.Basis,
            ["daily"] = x.Daily,
            ["minimum"] = min,
            ["shortfall"] = x.Gap,
            ["monthly"] = Math.Round(x.Gap * MinimumWageRule.DaysPerMonth, 2),
        }).ToList();

        var totals = new Dictionary<string, object?>
        {
            ["empno"] = "รวม",
            ["name"] = $"{rows.Count:N0} คน",
            ["monthly"] = rows.Sum(x => Math.Round(x.Gap * MinimumWageRule.DaysPerMonth, 2)),
        };

        return new ReportResult("พนักงานที่ค่าจ้างต่ำกว่าค่าจ้างขั้นต่ำ", columns, reportRows, totals, subtitle);
    }
}
