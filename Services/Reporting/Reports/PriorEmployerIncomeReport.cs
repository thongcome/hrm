using HRM.Models;
using HRM.Models.Reporting;
using HRM.Services.Audit;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Reporting.Reports;

// Prior-employer / opening-balance income and tax per employee for a tax year
// (Pay_EmployeePriorEmployerIncome). IsSameEmployer = true rows are this company's own
// opening balance (ยอดยกมา); false rows are the previous employer's certificate.
public class PriorEmployerIncomeReport(IDbContextFactory<HRMContext> dbFactory, IAuditLogger audit) : IReportDefinition
{
    public string Code => "prior-employer-income";
    public string Category => "เงินเดือน / GL (Payroll)";
    public string Name => "รายได้นายจ้างเดิม / ยอดยกมา";
    public string? Description => "รายได้ ค่าลดหย่อน ภาษีที่หักแล้ว ประกันสังคม และกองทุนสำรองเลี้ยงชีพ ที่ยกมาจากนายจ้างเดิมหรือจากระบบเดิมของบริษัท ในปีภาษีที่เลือก";

    public IReadOnlyList<ReportParameter> Parameters => new[]
    {
        new ReportParameter("year", "ปีภาษี (ค.ศ.)", ReportParamType.Year, Required: true, DefaultValue: DateTime.Today.Year.ToString()),
    }.Concat(ReportCriteria.Standard()).ToList();

    public async Task<ReportResult> RunAsync(IReadOnlyDictionary<string, string?> args, ReportContext ctx, CancellationToken ct = default)
    {
        var year = args.TryGetValue("year", out var y) && int.TryParse(y, out var yy) ? yy : DateTime.Today.Year;

        await using var context = await dbFactory.CreateDbContextAsync(ct);
        await audit.LogAccessAsync("Report:prior-employer-income", ctx.CompanyId, isSensitive: true, note: $"tax year {year}", ct: ct);

        var empQuery = context.Hremployee.Where(e => e.companyid == ctx.CompanyId);
        empQuery = await ReportCriteria.ApplyEmployeeAsync(context, empQuery, args, ct);

        var data = await (from p in context.Pay_EmployeePriorEmployerIncomes
                          join e in empQuery on p.HremployeeId equals e.id
                          where p.TaxYear == year && p.IsActive
                          orderby e.EmpNo
                          select new
                          {
                              e.EmpNo, e.EmpName, e.EmpSurname,
                              p.PriorEmployerName, p.IsSameEmployer,
                              p.IncomeAmount, p.DeductionAmount, p.TaxWithheldAmount,
                              p.SocialSecurityAmount, p.ProvidentFundAmount,
                          }).ToListAsync(ct);

        var rows = data.Select(d => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
        {
            ["empno"] = d.EmpNo,
            ["name"] = $"{d.EmpName} {d.EmpSurname}".Trim(),
            ["source"] = d.IsSameEmployer ? "ยอดยกมา (บริษัทนี้)" : "นายจ้างเดิม",
            ["employer"] = d.IsSameEmployer ? "(บริษัทนี้)" : d.PriorEmployerName,
            ["income"] = d.IncomeAmount,
            ["deduction"] = d.DeductionAmount,
            ["tax"] = d.TaxWithheldAmount,
            ["sso"] = d.SocialSecurityAmount,
            ["pvd"] = d.ProvidentFundAmount,
        }).ToList();

        var totals = new Dictionary<string, object?>
        {
            ["empno"] = "รวม",
            ["name"] = $"{data.Count:N0} รายการ",
            ["income"] = data.Sum(d => d.IncomeAmount),
            ["deduction"] = data.Sum(d => d.DeductionAmount),
            ["tax"] = data.Sum(d => d.TaxWithheldAmount),
            ["sso"] = data.Sum(d => d.SocialSecurityAmount),
            ["pvd"] = data.Sum(d => d.ProvidentFundAmount),
        };

        var crit = await ReportCriteria.DescribeAsync(context, ctx.CompanyId, args, ct);
        return new ReportResult(
            $"รายได้นายจ้างเดิม / ยอดยกมา — ปีภาษี {year}",
            new[]
            {
                new ReportColumn("empno", "รหัสพนักงาน"),
                new ReportColumn("name", "ชื่อ-สกุล"),
                new ReportColumn("source", "ประเภทยอด"),
                new ReportColumn("employer", "นายจ้างเดิม"),
                new ReportColumn("income", "รายได้สะสม", ReportColumnType.Money),
                new ReportColumn("deduction", "ค่าลดหย่อน", ReportColumnType.Money),
                new ReportColumn("tax", "ภาษีที่หักแล้ว", ReportColumnType.Money),
                new ReportColumn("sso", "ประกันสังคม", ReportColumnType.Money),
                new ReportColumn("pvd", "กองทุนสำรองเลี้ยงชีพ", ReportColumnType.Money),
            },
            rows, totals,
            $"บริษัท {ctx.CompanyId}" + (crit is null ? "" : " · " + crit));
    }
}
