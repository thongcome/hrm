namespace Advance.Payroll.Engine;

using System.Text;
using Advance.Payroll.Contracts;
using Advance.Payroll.Core;
using Advance.Payroll.Domain;
using Advance.Payroll.Data;
using Microsoft.EntityFrameworkCore;

// ไฟล์ยื่นแบบอิเล็กทรอนิกส์ — ported from HRM Services/Pay/EFilingExportService.cs. The pure
// formatting half of the original file (the nested `EFilingFormats` static class) was
// already split out verbatim into Advance.Payroll.Core.EFilingFormats per the task brief
// ("split it out if it's currently nested") — this file keeps only the DB-touching half
// and calls into Core's EFilingFormats for every field/line format.
//
// เดิม: Person record + LoadPeopleAsync(context) อ่าน context.Hremployee โดยตรง — ตอนนี้ผ่าน IEmployeeSource
// TODO(seam): LoadRegisteredAddressesAsync (HRM's `addresses` table, address_type_id=1) has NO
// Contracts interface — same open seam as WithholdingCertificateDataService.cs. ภ.ง.ด.1ก's
// address columns are left blank until an address seam exists (see EXTRACTION-PLAN.md
// "seams beyond the original 7").
public static class EFilingExportService
{
    public sealed record TextFile(string FileName, byte[] Content, int RowCount, decimal Total1, decimal Total2, IReadOnlyList<string> Warnings);

    // ── ภ.ง.ด.1 รายเดือน ──────────────────────────────────────────────────────────
    public static async Task<TextFile?> BuildPnd1Async(PayrollDbContext context, IEmployeeSource employeeSource, string companyId, string payrollPeriod, CancellationToken ct = default)
    {
        var data = await Por1DataService.BuildMonthlyAsync(context, employeeSource, companyId, payrollPeriod, ct);
        if (data is null) return null;
        var people = await employeeSource.GetByIdsAsync(data.Lines.Select(l => l.HremployeeId).Distinct().ToList(), ct);
        var payDate = await context.Pay_PayrollRuns
            .Where(r => r.CompanyId == companyId && r.PayrollPeriod == payrollPeriod && r.Status >= PayrollRunStatus.Approved && r.Status != PayrollRunStatus.Cancelled)
            .MaxAsync(r => (DateOnly?)r.PayDate, ct) ?? data.PeriodStart.AddMonths(1).AddDays(-1);

        var warnings = new List<string>();
        var sb = new StringBuilder();
        var seq = 0;
        foreach (var l in data.Lines)
        {
            var p = people.GetValueOrDefault(l.HremployeeId);
            var taxId = EFilingFormats.Digits(p?.IdCard);
            if (taxId.Length != 13) warnings.Add($"{l.EmpNo} เลขประจำตัวประชาชนไม่ครบ 13 หลัก ({p?.IdCard ?? "ว่าง"})");
            var (prefix, _) = EFilingFormats.PrefixBySex(p?.Sex);
            sb.Append(EFilingFormats.Pnd1Line(++seq, taxId, prefix, p?.EmpName ?? l.EmployeeName, p?.EmpSurname ?? "", payDate, l.TaxableIncome, l.TaxWithheld)).Append("\r\n");
        }
        return new TextFile($"PND1_{companyId}_{payrollPeriod}.txt", EFilingFormats.Utf8(sb.ToString()), seq, data.TotalTaxableIncome, data.TotalTaxWithheld, warnings);
    }

    // ── ภ.ง.ด.1ก ทั้งปี ──────────────────────────────────────────────────────────
    public static async Task<TextFile?> BuildPnd1KorAsync(PayrollDbContext context, IEmployeeSource employeeSource, string companyId, int taxYear, CancellationToken ct = default)
    {
        var data = await Por1DataService.BuildAnnualAsync(context, employeeSource, companyId, taxYear, ct);
        if (data is null) return null;
        var people = await employeeSource.GetByIdsAsync(data.Lines.Select(l => l.HremployeeId).Distinct().ToList(), ct);
        var warnings = new List<string>();
        var sb = new StringBuilder();
        var seq = 0;
        foreach (var l in data.Lines.Where(l => l.TotalTaxableIncome > 0))
        {
            var p = people.GetValueOrDefault(l.HremployeeId);
            var taxId = EFilingFormats.Digits(p?.IdCard);
            if (taxId.Length != 13) warnings.Add($"{l.EmpNo} เลขประจำตัวประชาชนไม่ครบ 13 หลัก ({p?.IdCard ?? "ว่าง"})");
            var (prefix, _) = EFilingFormats.PrefixBySex(p?.Sex);
            // TODO(seam): registered address — see class header comment; address fields always blank for now.
            sb.Append(EFilingFormats.Pnd1KorLine(++seq, taxId, prefix, p?.EmpName ?? l.EmployeeName, p?.EmpSurname ?? "", taxYear, l.TotalTaxableIncome, l.TotalTaxWithheld,
                null, null, null)).Append("\r\n");
        }
        return new TextFile($"PND1K_{companyId}_{taxYear}.txt", EFilingFormats.Utf8(sb.ToString()), seq, data.TotalTaxableIncome, data.TotalTaxWithheld, warnings);
    }

    // ── สปส.1-10 ส่วนที่ 1 + 2 ────────────────────────────────────────────────────
    public static async Task<TextFile?> BuildSso110Async(PayrollDbContext context, IEmployeeSource employeeSource, string companyId, string payrollPeriod, CancellationToken ct = default)
    {
        var rows = await context.Pay_PayrollEmployees.AsNoTracking()
            .Where(e => e.CompanyId == companyId && !e.IsExcluded
                        && e.Pay_PayrollRun.PayrollPeriod == payrollPeriod
                        && e.Pay_PayrollRun.Status >= PayrollRunStatus.Approved && e.Pay_PayrollRun.Status != PayrollRunStatus.Cancelled)
            .Select(e => new { e.HremployeeId, e.EmpNo, e.SocialSecurityAmount, e.SocialSecurityCompanyAmount, e.Pay_PayrollRun.PayDate, e.Pay_PayrollRun.PeriodStart })
            .ToListAsync(ct);
        if (rows.Count == 0) return null;

        var rate = await context.Hrucfsecuritys.AsNoTracking()
            .Where(x => x.companyid == companyId && x.SecurityCode == HrucfsecurityRateProvider.CurrentEmployeeSecurityCode)
            .Select(x => new { x.PercenSecurity, x.SecurityMoney }).FirstOrDefaultAsync(ct);
        var ratePercent = rate?.PercenSecurity ?? 5m;
        var cap = rate?.SecurityMoney ?? 15000m;
        var settings = await context.Pay_PayslipSettings.AsNoTracking().FirstOrDefaultAsync(s => s.CompanyId == companyId, ct);

        var warnings = new List<string>();
        if (string.IsNullOrWhiteSpace(settings?.SsoEmployerAccountNo))
            warnings.Add("ยังไม่ได้ตั้งเลขที่บัญชีนายจ้างประกันสังคม (ตั้งค่าบริษัท/สลิป) — ไฟล์ใส่ค่าว่างไว้ ต้องแก้ก่อนยื่น");

        var perEmp = rows.GroupBy(r => r.HremployeeId)
            .Select(g => new { HremployeeId = g.Key, EmpNo = g.First().EmpNo, Employee = g.Sum(r => r.SocialSecurityAmount), Employer = g.Sum(r => r.SocialSecurityCompanyAmount) })
            .Where(x => x.Employee > 0m)
            .OrderBy(x => x.EmpNo)
            .ToList();
        var people = await employeeSource.GetByIdsAsync(perEmp.Select(x => x.HremployeeId).ToList(), ct);

        var payDate = rows.Max(r => r.PayDate);
        var periodStart = rows.Min(r => r.PeriodStart);
        var details = new StringBuilder();
        decimal totalWages = 0, totalEmp = 0, totalEr = 0;
        foreach (var x in perEmp)
        {
            var p = people.GetValueOrDefault(x.HremployeeId);
            var idCard = EFilingFormats.Digits(p?.IdCard);
            if (idCard.Length != 13) warnings.Add($"{x.EmpNo} เลขประจำตัวประชาชนไม่ครบ 13 หลัก ({p?.IdCard ?? "ว่าง"})");
            // ค่าจ้างที่นำส่ง = เงินสมทบ ÷ อัตรา (ไม่เกินเพดาน) — ทางเดียวกับรายงาน สปส.1-10 ที่มีอยู่
            var wage = ratePercent > 0 ? Math.Min(cap, Math.Round(x.Employee * 100m / ratePercent, 2, MidpointRounding.AwayFromZero)) : 0m;
            var (_, code) = EFilingFormats.PrefixBySex(p?.Sex);
            details.Append(EFilingFormats.Sso110Detail(idCard, code, p?.EmpName ?? "", p?.EmpSurname ?? "", wage, x.Employee)).Append("\r\n");
            totalWages += wage; totalEmp += x.Employee; totalEr += x.Employer;
        }
        var header = EFilingFormats.Sso110Header(settings?.SsoEmployerAccountNo, settings?.SsoBranchSeq, payDate, periodStart,
            settings?.CompanyName ?? companyId, ratePercent, perEmp.Count, totalWages, totalEmp + totalEr, totalEmp, totalEr);
        var text = header + "\r\n" + details;
        return new TextFile($"SSO110_{companyId}_{payrollPeriod}.txt", EFilingFormats.Tis620(text), perEmp.Count, totalWages, totalEmp + totalEr, warnings);
    }
}
