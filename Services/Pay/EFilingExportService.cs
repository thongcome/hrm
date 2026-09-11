namespace HRM.Services.Pay;

using System.Globalization;
using System.Text;
using HRM.Models;
using Microsoft.EntityFrameworkCore;

// ไฟล์ยื่นแบบอิเล็กทรอนิกส์ (12 ก.ย. 2569 — CEO: "ทำให้ครบเลย"): จากเดิมมีแต่รายงาน PDF ยื่นออนไลน์ไม่ได้
//   ภ.ง.ด.1 / ภ.ง.ด.1ก  → ไฟล์ข้อความคั่นด้วย | สำหรับ "โปรแกรมโอนย้ายข้อมูล" ของกรมสรรพากร (RD Prep) ซึ่งผู้ใช้จับคู่คอลัมน์
//                          ครั้งเดียวแล้วสร้าง .rdx ไปยื่น e-Filing — ลำดับคอลัมน์ตามแบบที่โปรแกรมเงินเดือนไทยใช้ร่วมกัน (EFilingFormats)
//   สปส.1-10 ส่วนที่ 1–2 → ไฟล์ความยาวคงที่ 135 ตัวอักษร/บรรทัด สำหรับ e-Service ประกันสังคม ("ส่งข้อมูลเงินสมทบ" แบบแนบไฟล์)
// ตัวเลขทั้งหมดอ่านจากรอบที่อนุมัติแล้วของงวด (รวมกลับรายการ/ปรับปรุง) ทางเดียวกับ ภ.ง.ด.1 PDF
public static class EFilingExportService
{
    public sealed record TextFile(string FileName, byte[] Content, int RowCount, decimal Total1, decimal Total2, IReadOnlyList<string> Warnings);

    private sealed record Person(long HremployeeId, string? IdCard, string? FirstName, string? LastName, string? Sex);

    // ── ภ.ง.ด.1 รายเดือน ──────────────────────────────────────────────────────────
    public static async Task<TextFile?> BuildPnd1Async(HRMContext context, string companyId, string payrollPeriod, CancellationToken ct = default)
    {
        var data = await Por1DataService.BuildMonthlyAsync(context, companyId, payrollPeriod, ct);
        if (data is null) return null;
        var people = await LoadPeopleAsync(context, data.Lines.Select(l => l.HremployeeId), ct);
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
            sb.Append(EFilingFormats.Pnd1Line(++seq, taxId, prefix, p?.FirstName ?? l.EmployeeName, p?.LastName ?? "", payDate, l.TaxableIncome, l.TaxWithheld)).Append("\r\n");
        }
        return new TextFile($"PND1_{companyId}_{payrollPeriod}.txt", EFilingFormats.Utf8(sb.ToString()), seq, data.TotalTaxableIncome, data.TotalTaxWithheld, warnings);
    }

    // ── ภ.ง.ด.1ก ทั้งปี ──────────────────────────────────────────────────────────
    public static async Task<TextFile?> BuildPnd1KorAsync(HRMContext context, string companyId, int taxYear, CancellationToken ct = default)
    {
        var data = await Por1DataService.BuildAnnualAsync(context, companyId, taxYear, ct);
        if (data is null) return null;
        var people = await LoadPeopleAsync(context, data.Lines.Select(l => l.HremployeeId), ct);
        var warnings = new List<string>();
        var sb = new StringBuilder();
        var seq = 0;
        foreach (var l in data.Lines.Where(l => l.TotalTaxableIncome > 0))
        {
            var p = people.GetValueOrDefault(l.HremployeeId);
            var taxId = EFilingFormats.Digits(p?.IdCard);
            if (taxId.Length != 13) warnings.Add($"{l.EmpNo} เลขประจำตัวประชาชนไม่ครบ 13 หลัก ({p?.IdCard ?? "ว่าง"})");
            var (prefix, _) = EFilingFormats.PrefixBySex(p?.Sex);
            sb.Append(EFilingFormats.Pnd1KorLine(++seq, taxId, prefix, p?.FirstName ?? l.EmployeeName, p?.LastName ?? "", l.TotalTaxableIncome, l.TotalTaxWithheld)).Append("\r\n");
        }
        return new TextFile($"PND1K_{companyId}_{taxYear}.txt", EFilingFormats.Utf8(sb.ToString()), seq, data.TotalTaxableIncome, data.TotalTaxWithheld, warnings);
    }

    // ── สปส.1-10 ส่วนที่ 1 + 2 ────────────────────────────────────────────────────
    public static async Task<TextFile?> BuildSso110Async(HRMContext context, string companyId, string payrollPeriod, CancellationToken ct = default)
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
        var people = await LoadPeopleAsync(context, perEmp.Select(x => x.HremployeeId), ct);

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
            details.Append(EFilingFormats.Sso110Detail(idCard, code, p?.FirstName ?? "", p?.LastName ?? "", wage, x.Employee)).Append("\r\n");
            totalWages += wage; totalEmp += x.Employee; totalEr += x.Employer;
        }
        var header = EFilingFormats.Sso110Header(settings?.SsoEmployerAccountNo, settings?.SsoBranchSeq, payDate, periodStart,
            settings?.CompanyName ?? companyId, ratePercent, perEmp.Count, totalWages, totalEmp + totalEr, totalEmp, totalEr);
        var text = header + "\r\n" + details;
        return new TextFile($"SSO110_{companyId}_{payrollPeriod}.txt", EFilingFormats.Tis620(text), perEmp.Count, totalWages, totalEmp + totalEr, warnings);
    }

    private static async Task<Dictionary<long, Person>> LoadPeopleAsync(HRMContext context, IEnumerable<long> ids, CancellationToken ct)
    {
        var list = ids.Distinct().ToList();
        if (list.Count == 0) return new();
        return await context.Hremployee.AsNoTracking()
            .Where(e => list.Contains(e.id))
            .Select(e => new Person(e.id, e.IdCard, e.EmpName, e.EmpSurname, e.Sex))
            .ToDictionaryAsync(p => p.HremployeeId, ct);
    }
}

// สูตรจัดรูปแบบล้วน ๆ ไม่แตะฐานข้อมูล — ทดสอบได้ (HRM.Tests/Pay/EFilingFormatsTests.cs)
public static class EFilingFormats
{
    static EFilingFormats() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    // คำนำหน้าชื่อ: ทะเบียนพนักงานไม่มีช่องคำนำหน้า จึงอนุมานจากเพศ — รหัส 3 หลักของ สปส. (003 นาย, 004 นาง, 005 นางสาว)
    // หญิงใช้ "นางสาว" เป็นค่าเริ่มต้น (สปส. รับได้ทั้งสองแบบ)
    public static (string Text, string SsoCode) PrefixBySex(string? sex)
        => string.Equals(sex, "F", StringComparison.OrdinalIgnoreCase) ? ("นางสาว", "005") : ("นาย", "003");

    public static string Digits(string? s) => new string((s ?? "").Where(char.IsDigit).ToArray());

    // ภ.ง.ด.1 (RD Prep, คั่น |): ประเภทเงินได้ | ลำดับ | เลขประจำตัวผู้เสียภาษี | คำนำหน้า | ชื่อ | สกุล | วันที่จ่าย ddMMyyyy (พ.ศ.) | เงินได้ | ภาษี | เงื่อนไข
    //   ประเภทเงินได้ 1 = เงินเดือน ค่าจ้าง ฯลฯ ตามมาตรา 40(1) · เงื่อนไข 1 = หัก ณ ที่จ่าย
    public static string Pnd1Line(int seq, string taxId, string prefix, string firstName, string lastName, DateOnly payDate, decimal income, decimal tax)
        => string.Join("|", "1", seq, taxId, Clean(prefix), Clean(firstName), Clean(lastName), ThaiDate(payDate), Money(income), Money(tax), "1");

    // ภ.ง.ด.1ก (RD Prep): ประเภทเงินได้ | ลำดับ | เลขประจำตัวผู้เสียภาษี | คำนำหน้า | ชื่อ | สกุล | เงินได้ทั้งปี | ภาษีที่หักทั้งปี | เงื่อนไข
    public static string Pnd1KorLine(int seq, string taxId, string prefix, string firstName, string lastName, decimal income, decimal tax)
        => string.Join("|", "1", seq, taxId, Clean(prefix), Clean(firstName), Clean(lastName), Money(income), Money(tax), "1");

    // สปส.1-10 ส่วนที่ 1 (135 ตัวอักษร): ประเภท(1)=1 · เลขที่บัญชีนายจ้าง(10) · ลำดับที่สาขา(6) · วันที่ชำระ ddMMyy พ.ศ.(6) · งวดค่าจ้าง MMyy พ.ศ.(4)
    //   · ชื่อสถานประกอบการ(45) · อัตราเงินสมทบ(4 เช่น 0500 = 5.00%) · จำนวนผู้ประกันตน(6) · ค่าจ้างรวม(15) · เงินสมทบรวม(14) · ส่วนลูกจ้าง(12) · ส่วนนายจ้าง(12)
    public static string Sso110Header(string? employerAccountNo, string? branchSeq, DateOnly payDate, DateOnly periodStart, string companyName,
        decimal ratePercent, int insuredCount, decimal totalWages, decimal totalContribution, decimal employeePart, decimal employerPart)
    {
        var s = "1"
            + Fixed(Digits(employerAccountNo), 10, numeric: true)
            + Fixed(Digits(string.IsNullOrWhiteSpace(branchSeq) ? "0" : branchSeq), 6, numeric: true)
            + ThaiDate(payDate, "ddMMyy")
            + ThaiDate(periodStart, "MMyy")
            + Fixed(companyName, 45)
            + Fixed(((int)Math.Round(ratePercent * 100m)).ToString(CultureInfo.InvariantCulture), 4, numeric: true)
            + Fixed(insuredCount.ToString(CultureInfo.InvariantCulture), 6, numeric: true)
            + Fixed(Money(totalWages), 15, numeric: true)
            + Fixed(Money(totalContribution), 14, numeric: true)
            + Fixed(Money(employeePart), 12, numeric: true)
            + Fixed(Money(employerPart), 12, numeric: true);
        return s;
    }

    // สปส.1-10 ส่วนที่ 2 (135): ประเภท(1)=2 · เลขประจำตัวประชาชน(13) · รหัสคำนำหน้า(3) · ชื่อ(30) · สกุล(35) · ค่าจ้าง(14) · เงินสมทบลูกจ้าง(12) · ว่าง(27)
    public static string Sso110Detail(string idCard, string prefixCode, string firstName, string lastName, decimal wage, decimal employeeContribution)
        => "2" + Fixed(idCard, 13, numeric: true) + Fixed(prefixCode, 3, numeric: true) + Fixed(firstName, 30) + Fixed(lastName, 35)
           + Fixed(Money(wage), 14, numeric: true) + Fixed(Money(employeeContribution), 12, numeric: true) + new string(' ', 27);

    public static string Money(decimal v) => v.ToString("0.00", CultureInfo.InvariantCulture);

    // วันที่แบบไทย: ปี พ.ศ. — "ddMMyyyy" → 25012568, "ddMMyy" → 250168, "MMyy" → 0168
    public static string ThaiDate(DateOnly d, string format = "ddMMyyyy")
    {
        var be = d.Year + 543;
        return format switch
        {
            "ddMMyy" => $"{d.Day:00}{d.Month:00}{be % 100:00}",
            "MMyy" => $"{d.Month:00}{be % 100:00}",
            _ => $"{d.Day:00}{d.Month:00}{be:0000}",
        };
    }

    // ช่องความยาวคงที่: ตัวเลขชิดขวาเติม 0, ข้อความชิดซ้ายเติมช่องว่าง, ยาวเกินตัดทิ้ง
    public static string Fixed(string? value, int width, bool numeric = false)
    {
        var v = (value ?? "").Trim();
        if (v.Length > width) v = numeric ? v[^width..] : v[..width];
        return numeric ? v.PadLeft(width, '0') : v.PadRight(width, ' ');
    }

    private static string Clean(string s) => (s ?? "").Replace("|", " ").Replace("\r", " ").Replace("\n", " ").Trim();

    public static byte[] Utf8(string text) => new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(text);
    public static byte[] Tis620(string text) => Encoding.GetEncoding(874).GetBytes(text);
}
