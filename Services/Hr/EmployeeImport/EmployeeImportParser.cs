using System.Globalization;
using ClosedXML.Excel;

namespace HRM.Services.Hr.EmployeeImport;

public sealed record ImportIssue(string Sheet, int Row, string Column, string Message);

public sealed record ParsedOrg(int Row, string Code, string Name, string? ParentCode, string? ApproverEmpNo);

public sealed record ParsedEmployee(
    int Row, string EmpNo, string PrenameCode, string Sex, string FirstName, string LastName,
    string? FirstNameEn, string? LastNameEn, string IdCard, DateTime? BirthDate, DateTime HireDate,
    string OrgCode, string? PosCode, string EmpTypeCode, bool IsDaily, decimal Pay,
    string BankCode, string BankAccount, string? BankBranch,
    decimal? PvdEmployeeRate, decimal? PvdEmployerRate, string? Email, string? Phone, bool CreateLogin,
    ParsedAddress? Address = null);

// ที่อยู่ตามทะเบียนบ้านจากไฟล์ — null เมื่อไม่ได้กรอกสักช่อง (ไฟล์เดิมที่ไม่มีคอลัมน์ที่อยู่ยังนำเข้าได้เหมือนเดิม)
public sealed record ParsedAddress(string? No, string? Moo, string? Village, string? Soi, string? Road,
    string? Subdistrict, string? District, string? Province, string? Postcode);

// one month already paid by the customer's previous system; TaxYear is Christian era
public sealed record ParsedOpeningBalance(
    int Row, string EmpNo, int TaxYear, int Month, decimal GrossIncome, decimal TaxableIncome, decimal TaxWithheld,
    decimal SsoEmployee, decimal SsoEmployer, decimal PvdEmployee, decimal PvdEmployer, decimal? NetPay);

public sealed record ParsedFile(
    IReadOnlyList<ParsedOrg> Orgs, IReadOnlyList<ParsedEmployee> Employees, IReadOnlyList<ParsedOpeningBalance> OpeningBalances,
    IReadOnlyList<ImportIssue> Issues)
{
    public bool HasErrors => Issues.Count > 0;
}

// What the parser needs from the database to validate references — passed in, so the parser
// itself stays pure and unit-testable.
public sealed record ImportReferenceData(
    IReadOnlySet<string> BankCodes, IReadOnlySet<string> PositionCodes, IReadOnlySet<string> ExistingOrgCodes);

// Reads the customer's file (EmployeeImportSchema) and reports every problem by sheet / row /
// column — nothing is written anywhere. Columns are found by header text, so a column moved
// by the customer still works; a missing required header is itself an error.
public static class EmployeeImportParser
{
    public static ParsedFile Parse(Stream xlsx, ImportReferenceData refs)
    {
        var issues = new List<ImportIssue>();
        XLWorkbook wb;
        try { wb = new XLWorkbook(xlsx); }
        catch (Exception)
        {
            issues.Add(new("ไฟล์", 0, "", "เปิดไฟล์ไม่ได้ — ต้องเป็นไฟล์ Excel (.xlsx) ที่ดาวน์โหลดจากแบบฟอร์มของระบบ"));
            return new ParsedFile([], [], [], issues);
        }
        using (wb)
        {
            var orgs = ReadOrgs(wb, issues);
            var employees = ReadEmployees(wb, refs, orgs, issues);
            CheckOrgReferences(orgs, employees, refs, issues);
            var opening = ReadOpeningBalances(wb, issues);
            return new ParsedFile(orgs, employees, opening, issues);
        }
    }

    // ── sheets ──────────────────────────────────────────────────────────────────
    private static List<ParsedOrg> ReadOrgs(XLWorkbook wb, List<ImportIssue> issues)
    {
        var sheet = EmployeeImportSchema.Org;
        var result = new List<ParsedOrg>();
        foreach (var (row, get) in Rows(wb, sheet, issues))
        {
            var r = new RowReader(sheet.Name, row, get, issues);
            var code = r.Text("OrgCode", required: true, max: 50);
            var name = r.Text("OrgName", required: true, max: 250);
            var parent = r.Code("ParentCode");
            var approver = r.Code("ApproverEmpNo");
            if (code is null || name is null) continue;
            if (result.Any(o => o.Code.Equals(code, StringComparison.OrdinalIgnoreCase)))
            { r.Error("OrgCode", $"รหัสหน่วยงาน {code} ซ้ำในไฟล์"); continue; }
            if (parent is not null && parent.Equals(code, StringComparison.OrdinalIgnoreCase))
                r.Error("ParentCode", "หน่วยงานแม่เป็นตัวเองไม่ได้");
            result.Add(new ParsedOrg(row, code, name, parent, approver));
        }
        return result;
    }

    private static List<ParsedEmployee> ReadEmployees(XLWorkbook wb, ImportReferenceData refs, List<ParsedOrg> orgs, List<ImportIssue> issues)
    {
        var sheet = EmployeeImportSchema.Employee;
        var result = new List<ParsedEmployee>();
        var orgCodes = orgs.Select(o => o.Code).Concat(refs.ExistingOrgCodes).ToHashSet(StringComparer.OrdinalIgnoreCase);
        // duplicates are checked against EVERY row seen, not only rows that passed — a code repeated
        // after a faulty first row must still be reported
        var seenEmpNos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenIdCards = new HashSet<string>();

        foreach (var (row, get) in Rows(wb, sheet, issues))
        {
            var r = new RowReader(sheet.Name, row, get, issues);
            var before = issues.Count;

            var empNo = r.Text("EmpNo", required: true, max: 50);
            var prename = r.Pick("Prename", EmployeeImportSchema.Prenames.Select(p => p.Text), required: true);
            var first = r.Text("FirstName", required: true, max: 250);
            var last = r.Text("LastName", required: true, max: 250);
            var firstEn = r.Text("FirstNameEn", max: 50);
            var lastEn = r.Text("LastNameEn", max: 50);
            var idCard = r.Digits("IdCard", required: true);
            if (idCard is not null && !IsThaiIdCard(idCard)) r.Error("IdCard", "เลขบัตรประชาชนไม่ถูกต้อง (ต้อง 13 หลัก และเลขตรวจสอบหลักสุดท้ายต้องถูก)");
            var birth = r.Date("BirthDate");
            var sexText = r.Pick("Sex", EmployeeImportSchema.Sexes.Select(s => s.Text));
            var hire = r.Date("HireDate", required: true);
            var orgCode = r.Code("OrgCode", required: true);
            if (orgCode is not null && !orgCodes.Contains(orgCode)) r.Error("OrgCode", $"ไม่พบหน่วยงาน {orgCode} ทั้งในชีตหน่วยงานและในระบบ");
            var pos = r.CodeBeforeDash("Position");
            if (pos is not null && !refs.PositionCodes.Contains(pos)) r.Error("Position", $"ไม่พบตำแหน่ง {pos} ในระบบ");
            var empType = r.Pick("EmpType", EmployeeImportSchema.EmpTypes.Select(t => t.Text));
            var payType = r.Pick("PayType", [EmployeeImportSchema.PayMonthly, EmployeeImportSchema.PayDaily], required: true);
            var pay = r.Number("Pay", required: true);
            if (pay is <= 0) r.Error("Pay", "ต้องมากกว่า 0");
            var bank = r.CodeBeforeDash("Bank", required: true);
            if (bank is not null && !refs.BankCodes.Contains(bank)) r.Error("Bank", $"ไม่พบธนาคารรหัส {bank}");
            var account = r.Digits("BankAccount", required: true);
            if (account is not null && !account.All(char.IsDigit)) r.Error("BankAccount", "เลขบัญชีต้องเป็นตัวเลขล้วน");
            var branch = r.Digits("BankBranch");
            var pvdEmp = r.Number("PvdEmployeeRate");
            var pvdCo = r.Number("PvdEmployerRate");
            if (pvdEmp is not null && (pvdEmp < 2 || pvdEmp > 15)) r.Error("PvdEmployeeRate", "อัตรากองทุนสำรองฯ ลูกจ้างต้องอยู่ระหว่าง 2–15%");
            if (pvdCo is not null && (pvdCo < 2 || pvdCo > 15)) r.Error("PvdEmployerRate", "อัตรากองทุนสำรองฯ นายจ้างต้องอยู่ระหว่าง 2–15%");
            if (pvdCo is not null && pvdEmp is null) r.Error("PvdEmployeeRate", "ระบุอัตรานายจ้างแล้ว ต้องระบุอัตราลูกจ้างด้วย");
            var email = r.Text("Email", max: 100);
            if (email is not null && !System.Net.Mail.MailAddress.TryCreate(email, out _)) r.Error("Email", "รูปแบบอีเมลไม่ถูกต้อง");
            var phone = r.Digits("Phone");
            var login = r.Pick("CreateLogin", [EmployeeImportSchema.Yes, EmployeeImportSchema.No]);
            var postcode = r.Digits("AddrPostcode");
            if (postcode is not null && postcode.Length != 5) r.Error("AddrPostcode", "รหัสไปรษณีย์ต้องเป็นตัวเลข 5 หลัก");
            var addr = new ParsedAddress(r.Text("AddrNo", max: 100), r.Text("AddrMoo", max: 50), r.Text("AddrVillage", max: 100),
                r.Text("AddrSoi", max: 250), r.Text("AddrRoad", max: 250), r.Text("AddrSubdistrict", max: 100),
                r.Text("AddrDistrict", max: 100), r.Text("AddrProvince", max: 100), postcode);
            var hasAddr = new[] { addr.No, addr.Moo, addr.Village, addr.Soi, addr.Road, addr.Subdistrict, addr.District, addr.Province, addr.Postcode }
                .Any(v => v is not null);

            if (empNo is not null && !seenEmpNos.Add(empNo))
                r.Error("EmpNo", $"รหัสพนักงาน {empNo} ซ้ำในไฟล์");
            if (idCard is not null && !seenIdCards.Add(idCard))
                r.Error("IdCard", "เลขบัตรประชาชนซ้ำกับแถวอื่นในไฟล์");
            if (issues.Count > before) continue;

            var p = EmployeeImportSchema.Prenames.First(x => x.Text == prename);
            var sex = sexText is null ? p.Sex : EmployeeImportSchema.Sexes.First(s => s.Text == sexText).Code;
            var type = empType is null ? "01" : EmployeeImportSchema.EmpTypes.First(t => t.Text == empType).Code;
            result.Add(new ParsedEmployee(row, empNo!, p.Code, sex, first!, last!, firstEn, lastEn, idCard!, birth, hire!.Value,
                orgCode!, pos, type, payType == EmployeeImportSchema.PayDaily, pay!.Value, bank!, account!, branch,
                pvdEmp, pvdCo, email, phone, login != EmployeeImportSchema.No, hasAddr ? addr : null));
        }
        return result;
    }

    private static void CheckOrgReferences(List<ParsedOrg> orgs, List<ParsedEmployee> employees, ImportReferenceData refs, List<ImportIssue> issues)
    {
        var codes = orgs.Select(o => o.Code).Concat(refs.ExistingOrgCodes).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var sheet = EmployeeImportSchema.Org;
        foreach (var o in orgs)
        {
            if (o.ParentCode is not null && !codes.Contains(o.ParentCode))
                issues.Add(new(sheet.Name, o.Row, Header(sheet, "ParentCode"), $"ไม่พบหน่วยงานแม่ {o.ParentCode}"));
            // approver must be an employee in this file or already in the system (checked by the service)
        }
        // a parent chain must not loop back
        var byCode = orgs.ToDictionary(o => o.Code, StringComparer.OrdinalIgnoreCase);
        foreach (var o in orgs)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { o.Code };
            var cur = o.ParentCode;
            while (cur is not null && byCode.TryGetValue(cur, out var parent))
            {
                if (!seen.Add(parent.Code))
                { issues.Add(new(sheet.Name, o.Row, Header(sheet, "ParentCode"), "หน่วยงานแม่วนกลับมาที่ตัวเอง")); break; }
                cur = parent.ParentCode;
            }
        }
    }

    // Whether the employee exists, and whether the month was already paid by THIS system, needs the
    // database — EmployeeImportService checks those. Here: shape, ranges and duplicates in the file.
    // Ported from Advance.Payroll (CEO order, 22 ก.ย. 2569: mirror the payroll domain).
    private static List<ParsedOpeningBalance> ReadOpeningBalances(XLWorkbook wb, List<ImportIssue> issues)
    {
        var sheet = EmployeeImportSchema.OpeningBalance;
        var result = new List<ParsedOpeningBalance>();
        var seen = new HashSet<(string, int, int)>();
        foreach (var (row, get) in Rows(wb, sheet, issues))
        {
            var r = new RowReader(sheet.Name, row, get, issues);
            var before = issues.Count;

            var empNo = r.Code("EmpNo", required: true);
            var year = r.WholeNumber("TaxYear", required: true);
            // the sheet asks for พ.ศ.; a Christian-era year is accepted too
            if (year is > 2400) year -= 543;
            if (year is not null && (year < 2000 || year > 2200)) { r.Error("TaxYear", "ปีไม่ถูกต้อง (ใส่ปี พ.ศ. เช่น 2569)"); year = null; }
            var month = r.WholeNumber("Month", required: true);
            if (month is not null && (month < 1 || month > 12)) r.Error("Month", "เดือนต้องเป็น 1–12");
            var gross = r.Money("GrossIncome", required: true);
            var taxable = r.Money("TaxableIncome", required: true);
            var tax = r.Money("TaxWithheld", required: true);
            var ssoEmp = r.Money("SsoEmployee");
            var ssoCo = r.Money("SsoEmployer");
            var pvdEmp = r.Money("PvdEmployee");
            var pvdCo = r.Money("PvdEmployer");
            var net = r.Money("NetPay");
            if (tax is not null && taxable is not null && tax > taxable)
                r.Error("TaxWithheld", "ภาษีหัก ณ ที่จ่ายมากกว่าเงินได้ที่ต้องเสียภาษี — ตรวจว่ากรอกสลับช่องหรือไม่");

            if (empNo is not null && year is not null && month is not null && !seen.Add((empNo.ToUpperInvariant(), year.Value, month.Value)))
                r.Error("Month", $"{empNo} เดือน {month}/{year + 543} ซ้ำกับแถวอื่นในไฟล์ — หนึ่งแถวต่อคนต่อเดือน");
            if (issues.Count > before) continue;

            result.Add(new ParsedOpeningBalance(row, empNo!, year!.Value, month!.Value, gross!.Value, taxable!.Value, tax!.Value,
                ssoEmp ?? 0m, ssoCo ?? 0m, pvdEmp ?? 0m, pvdCo ?? 0m, net));
        }
        return result;
    }

    // ── helpers ─────────────────────────────────────────────────────────────────
    private static IEnumerable<(int Row, Func<string, IXLCell?> Get)> Rows(XLWorkbook wb, ImportSheet sheet, List<ImportIssue> issues)
    {
        if (!wb.TryGetWorksheet(sheet.Name, out var ws))
        {
            issues.Add(new(sheet.Name, 0, "", $"ไม่พบชีต \"{sheet.Name}\" — ใช้แบบฟอร์มจากระบบ ห้ามเปลี่ยนชื่อชีต"));
            yield break;
        }
        var headerRow = ws.Row(1);
        var colByKey = new Dictionary<string, int>();
        foreach (var col in sheet.Columns)
        {
            var cell = headerRow.CellsUsed().FirstOrDefault(c => Norm(c.GetString()) == Norm(col.Header));
            if (cell is not null) colByKey[col.Key] = cell.Address.ColumnNumber;
            else if (col.Required) issues.Add(new(sheet.Name, 1, col.Header, "ไม่พบหัวคอลัมน์นี้ — ห้ามเปลี่ยนชื่อหรือลบหัวคอลัมน์"));
        }
        var last = ws.LastRowUsed()?.RowNumber() ?? 1;
        for (var row = 2; row <= last; row++)
        {
            var wsRow = ws.Row(row);
            if (colByKey.Values.All(c => wsRow.Cell(c).IsEmpty())) continue;   // blank line
            var captured = row;
            yield return (captured, key => colByKey.TryGetValue(key, out var c) ? ws.Row(captured).Cell(c) : null);
        }
    }


    private static string Norm(string s) => s.Trim().Replace(" ", "");
    private static string Header(ImportSheet sheet, string key) => sheet.Columns.First(c => c.Key == key).Header.TrimEnd('*');

    // Thai national ID: 12 digits × weights 13..2, check digit = (11 − sum mod 11) mod 10
    public static bool IsThaiIdCard(string s)
    {
        if (s.Length != 13 || !s.All(char.IsDigit)) return false;
        var sum = 0;
        for (var i = 0; i < 12; i++) sum += (s[i] - '0') * (13 - i);
        return (11 - sum % 11) % 10 == s[12] - '0';
    }

    private sealed class RowReader(string sheet, int row, Func<string, IXLCell?> get, List<ImportIssue> issues)
    {
        private readonly ImportSheet _schema = EmployeeImportSchema.DataSheets.First(s => s.Name == sheet);

        public void Error(string key, string message) => issues.Add(new(sheet, row, Header(_schema, key), message));

        private string? Raw(string key)
        {
            var cell = get(key);
            if (cell is null || cell.IsEmpty()) return null;
            var s = cell.GetFormattedString().Trim();
            return s.Length == 0 ? null : s;
        }

        public string? Text(string key, bool required = false, int max = 0)
        {
            var s = Raw(key);
            if (s is null) { if (required) Error(key, "ต้องกรอก"); return null; }
            if (max > 0 && s.Length > max) { Error(key, $"ยาวเกิน {max} ตัวอักษร"); return null; }
            return s;
        }

        // codes (unit / employee codes) are kept exactly as typed — "AD-PRD-01" has real dashes
        public string? Code(string key, bool required = false)
        {
            var s = Raw(key);
            if (s is null && required) Error(key, "ต้องกรอก");
            return s;
        }

        // number-like fields (ID card, account, branch, phone): keep leading zeros, drop the
        // spaces and dashes people type in them ("1-1017-00203-45-0")
        public string? Digits(string key, bool required = false)
        {
            var s = Raw(key)?.Replace(" ", "").Replace("-", "");
            if (string.IsNullOrEmpty(s)) { if (required) Error(key, "ต้องกรอก"); return null; }
            return s;
        }

        public string? CodeBeforeDash(string key, bool required = false)
        {
            var s = Raw(key);
            if (s is null) { if (required) Error(key, "ต้องเลือกจากรายการ"); return null; }
            var dash = s.IndexOf(" - ", StringComparison.Ordinal);
            return (dash > 0 ? s[..dash] : s).Trim();
        }

        public string? Pick(string key, IEnumerable<string> allowed, bool required = false)
        {
            var s = Raw(key);
            if (s is null) { if (required) Error(key, "ต้องเลือกจากรายการ"); return null; }
            var list = allowed.ToList();
            if (!list.Contains(s)) { Error(key, $"ค่า \"{s}\" ไม่อยู่ในรายการ ({string.Join(" / ", list)})"); return null; }
            return s;
        }

        public decimal? Number(string key, bool required = false)
        {
            var cell = get(key);
            if (cell is null || cell.IsEmpty()) { if (required) Error(key, "ต้องกรอก"); return null; }
            if (cell.DataType == XLDataType.Number) return (decimal)cell.GetDouble();
            var s = cell.GetString().Replace(",", "").Trim();
            if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var d)) return d;
            Error(key, $"\"{s}\" ไม่ใช่ตัวเลข");
            return null;
        }

        public int? WholeNumber(string key, bool required = false)
        {
            var d = Number(key, required);
            if (d is null) return null;
            if (d != decimal.Truncate(d.Value)) { Error(key, "ต้องเป็นจำนวนเต็ม"); return null; }
            return (int)d.Value;
        }

        // money: not negative, at most 2 decimals (satang)
        public decimal? Money(string key, bool required = false)
        {
            var d = Number(key, required);
            if (d is null) return null;
            if (d < 0) { Error(key, "ติดลบไม่ได้"); return null; }
            if (decimal.Round(d.Value, 2) != d.Value) { Error(key, "ทศนิยมได้ไม่เกิน 2 ตำแหน่ง"); return null; }
            return d;
        }

        // Excel dates, or text dd/MM/yyyy with a Christian or Buddhist-era year
        public DateTime? Date(string key, bool required = false)
        {
            var cell = get(key);
            if (cell is null || cell.IsEmpty()) { if (required) Error(key, "ต้องกรอก"); return null; }
            DateTime d;
            if (cell.DataType == XLDataType.DateTime) d = cell.GetDateTime();
            else if (!DateTime.TryParseExact(cell.GetString().Trim(), ["d/M/yyyy", "dd/MM/yyyy", "yyyy-MM-dd"],
                         CultureInfo.InvariantCulture, DateTimeStyles.None, out d))
            { Error(key, $"\"{cell.GetString()}\" ไม่ใช่วันที่ (ใช้ วัน/เดือน/ปี)"); return null; }
            if (d.Year > 2400) d = d.AddYears(-543);
            if (d.Year < 1900 || d.Year > 2200) { Error(key, "ปีไม่ถูกต้อง"); return null; }
            return d.Date;
        }
    }
}
