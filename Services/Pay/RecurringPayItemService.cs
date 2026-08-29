namespace HRM.Services.Pay;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

public record RecurringItemRow(string TypeCode, string TypeName, string Detail, DateOnly? EffectiveFrom, DateOnly? EffectiveTo);
public record RecurringItemParticipant(long HremployeeId, string EmpNo, string EmpName, string Detail, DateOnly? EffectiveFrom);

// "Recurring" pay items — standing employee configuration pulled automatically
// every payroll run at the calculate step (base salary, provident fund, group
// insurance, company loan installments, welfare fund) — as opposed to
// Pay_AdhocPayItem ("เฉพาะกิจ"/one-off items HR enters per period). There is
// no single table for these; each lives in its own standing-config table
// exactly as PayrollCalculationService reads them (see its mutual-exclusion
// logic for PF vs WelfareFund, mirrored here). This service only reads —
// changes still happen on each item's own admin page
// (ProvidentFundElectionAdmin, EmployeeInsuranceAdmin, etc.).
public static class RecurringPayItemService
{
    public static readonly (string Code, string NameTh)[] ItemTypes =
    {
        ("BASE", "เงินเดือนฐาน"),
        ("PF", "กองทุนสำรองเลี้ยงชีพ"),
        ("INSURANCE", "ประกันกลุ่ม"),
        ("LOAN", "เงินกู้บริษัท"),
        ("WELFAREFUND", "กองทุนสงเคราะห์ลูกจ้าง"),
    };

    public static async Task<List<RecurringItemRow>> GetForEmployeeAsync(HRMContext ctx, long hremployeeId, CancellationToken ct = default)
    {
        var rows = new List<RecurringItemRow>();

        var emp = await ctx.Hremployee.FirstOrDefaultAsync(e => e.id == hremployeeId, ct);
        if (emp is null) return rows;

        if (emp.SalaryAmt is decimal salary)
            rows.Add(new RecurringItemRow("BASE", "เงินเดือนฐาน", salary.ToString("N2"), null, null));

        var pf = await ctx.Pay_ProvidentFundElections
            .Where(p => p.HremployeeId == hremployeeId && p.IsActive)
            .OrderByDescending(p => p.EffectiveFrom)
            .FirstOrDefaultAsync(ct);
        if (pf is not null)
            rows.Add(new RecurringItemRow("PF", "กองทุนสำรองเลี้ยงชีพ", $"พนักงาน {pf.EmployeeContributionRate:0.##}% / บริษัท {pf.CompanyContributionRate:0.##}%", pf.EffectiveFrom, pf.EffectiveTo));
        else if ((emp.ProvfEmprate ?? 0m) != 0m)
            rows.Add(new RecurringItemRow("PF", "กองทุนสำรองเลี้ยงชีพ", $"พนักงาน {emp.ProvfEmprate:0.##}% / บริษัท {emp.ProvfCorprate:0.##}% (ค่าเดิมจากทะเบียนพนักงาน)", null, null));

        var insurances = await ctx.Pay_EmployeeInsuranceEnrollments
            .Include(e => e.Pay_InsurancePlan)
            .Where(e => e.HremployeeId == hremployeeId && e.IsActive)
            .ToListAsync(ct);
        foreach (var ins in insurances)
            rows.Add(new RecurringItemRow("INSURANCE", $"ประกันกลุ่ม — {ins.Pay_InsurancePlan.PlanName}", $"พนักงาน {ins.EmployeeAmount:N2} / บริษัท {ins.CompanyAmount:N2}", ins.EffectiveFrom, ins.EffectiveTo));

        var loans = await ctx.Pay_EmployeeLoans
            .Where(l => l.HremployeeId == hremployeeId && l.Status == Pay_EmployeeLoanStatus.Active)
            .ToListAsync(ct);
        foreach (var loan in loans)
            rows.Add(new RecurringItemRow("LOAN", "เงินกู้บริษัท", $"งวดละ {loan.InstallmentAmount:N2} (คงเหลือ {loan.RemainingBalance:N2})", null, null));

        // มาตรา 130: mirrors PayrollCalculationService's exact mutual-exclusion
        // check — an active PF election (or legacy Hremployee.ProvfEmprate)
        // exempts the employee from welfare fund.
        var hasPf = pf is not null || (emp.ProvfEmprate ?? 0m) != 0m;
        var welfarePolicy = await ctx.Pay_WelfareFundPolicies
            .Where(w => w.CompanyId == emp.companyid && w.IsEnabled)
            .FirstOrDefaultAsync(ct);
        if (welfarePolicy is not null && !hasPf)
            rows.Add(new RecurringItemRow("WELFAREFUND", "กองทุนสงเคราะห์ลูกจ้าง", $"พนักงาน {welfarePolicy.EmployeeContributionRate:0.##}%", null, null));

        return rows;
    }

    public static async Task<List<RecurringItemParticipant>> GetParticipantsByTypeAsync(HRMContext ctx, string companyId, string typeCode, CancellationToken ct = default)
    {
        switch (typeCode)
        {
            case "BASE":
                return await ctx.Hremployee
                    .Where(e => e.companyid == companyId && e.ResignDate == null && e.SalaryAmt != null)
                    .OrderBy(e => e.EmpNo)
                    .Select(e => new RecurringItemParticipant(e.id, e.EmpNo, (e.EmpName ?? "") + " " + (e.EmpSurname ?? ""), e.SalaryAmt!.Value.ToString("N2"), null))
                    .ToListAsync(ct);

            case "PF":
                return await ctx.Pay_ProvidentFundElections
                    .Include(p => p.Hremployee)
                    .Where(p => p.IsActive && p.Hremployee.companyid == companyId)
                    .OrderBy(p => p.Hremployee.EmpNo)
                    .Select(p => new RecurringItemParticipant(p.HremployeeId, p.Hremployee.EmpNo, (p.Hremployee.EmpName ?? "") + " " + (p.Hremployee.EmpSurname ?? ""),
                        $"พนักงาน {p.EmployeeContributionRate:0.##}% / บริษัท {p.CompanyContributionRate:0.##}%", p.EffectiveFrom))
                    .ToListAsync(ct);

            case "INSURANCE":
                return await ctx.Pay_EmployeeInsuranceEnrollments
                    .Include(e => e.Hremployee).Include(e => e.Pay_InsurancePlan)
                    .Where(e => e.IsActive && e.Hremployee.companyid == companyId)
                    .OrderBy(e => e.Hremployee.EmpNo)
                    .Select(e => new RecurringItemParticipant(e.HremployeeId, e.Hremployee.EmpNo, (e.Hremployee.EmpName ?? "") + " " + (e.Hremployee.EmpSurname ?? ""),
                        $"{e.Pay_InsurancePlan.PlanName}: {e.EmployeeAmount:N2}", e.EffectiveFrom))
                    .ToListAsync(ct);

            case "LOAN":
                return await ctx.Pay_EmployeeLoans
                    .Include(l => l.Hremployee)
                    .Where(l => l.Status == Pay_EmployeeLoanStatus.Active && l.Hremployee.companyid == companyId)
                    .OrderBy(l => l.Hremployee.EmpNo)
                    .Select(l => new RecurringItemParticipant(l.HremployeeId, l.Hremployee.EmpNo, (l.Hremployee.EmpName ?? "") + " " + (l.Hremployee.EmpSurname ?? ""),
                        $"งวดละ {l.InstallmentAmount:N2} (คงเหลือ {l.RemainingBalance:N2})", null))
                    .ToListAsync(ct);

            case "WELFAREFUND":
                var policy = await ctx.Pay_WelfareFundPolicies.FirstOrDefaultAsync(w => w.CompanyId == companyId && w.IsEnabled, ct);
                if (policy is null) return new List<RecurringItemParticipant>();
                return await ctx.Hremployee
                    .Where(e => e.companyid == companyId && e.ResignDate == null
                        && !ctx.Pay_ProvidentFundElections.Any(p => p.HremployeeId == e.id && p.IsActive)
                        && (e.ProvfEmprate == null || e.ProvfEmprate == 0m))
                    .OrderBy(e => e.EmpNo)
                    .Select(e => new RecurringItemParticipant(e.id, e.EmpNo, (e.EmpName ?? "") + " " + (e.EmpSurname ?? ""), $"พนักงาน {policy.EmployeeContributionRate:0.##}%", null))
                    .ToListAsync(ct);

            default:
                return new List<RecurringItemParticipant>();
        }
    }
}
