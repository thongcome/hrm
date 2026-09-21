using ClosedXML.Excel;
using HRM.Services.Hr.EmployeeImport;
using Xunit;

namespace HRM.Tests.Hr;

// ตัวอ่านไฟล์นำเข้าพนักงาน — อ่านจากแบบฟอร์มจริงที่ระบบสร้าง แล้วตรวจทุกช่อง
public class EmployeeImportParserTests
{
    private static readonly EmployeeImportLookups Lookups = new(
        "NEWCO", "บริษัท นิวโค จำกัด",
        [new("004", "ธนาคารกสิกรไทย")], [new("A01", "พนักงาน")], []);
    private static readonly ImportReferenceData Refs = new(
        new HashSet<string> { "004" }, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "A01" },
        new HashSet<string>(StringComparer.OrdinalIgnoreCase));

    public static string IdCard(string first12)
    {
        var sum = 0;
        for (var i = 0; i < 12; i++) sum += (first12[i] - '0') * (13 - i);
        return first12 + ((11 - sum % 11) % 10);
    }

    // the real template, with rows filled in by column key
    public static byte[] File(IEnumerable<Dictionary<string, object>> orgs, IEnumerable<Dictionary<string, object>> employees)
    {
        using var wb = new XLWorkbook(new MemoryStream(EmployeeImportTemplateBuilder.Build(Lookups)));
        Fill(wb.Worksheet(EmployeeImportSchema.Org.Name), EmployeeImportSchema.Org, orgs);
        Fill(wb.Worksheet(EmployeeImportSchema.Employee.Name), EmployeeImportSchema.Employee, employees);
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public static void Fill(IXLWorksheet ws, ImportSheet sheet, IEnumerable<Dictionary<string, object>> rows)
    {
        var r = (ws.LastRowUsed()?.RowNumber() ?? 1) + 1;
        foreach (var row in rows)
        {
            foreach (var (key, value) in row)
            {
                var col = sheet.Columns.ToList().FindIndex(c => c.Key == key) + 1;
                var cell = ws.Cell(r, col);
                switch (value)
                {
                    case DateTime d: cell.Value = d; break;
                    case decimal m: cell.Value = m; break;
                    case int i: cell.Value = i; break;
                    default: cell.SetValue(value.ToString()); break;
                }
            }
            r++;
        }
    }

    public static Dictionary<string, object> Employee(string empNo, string idFirst12, string org, string payType = EmployeeImportSchema.PayMonthly, decimal pay = 30000m) => new()
    {
        ["EmpNo"] = empNo, ["Prename"] = "นางสาว", ["FirstName"] = "สมใจ", ["LastName"] = "ใจดี " + empNo,
        ["IdCard"] = IdCard(idFirst12), ["HireDate"] = new DateTime(2024, 6, 1), ["OrgCode"] = org,
        ["Position"] = "A01 - พนักงาน", ["PayType"] = payType, ["Pay"] = pay,
        ["Bank"] = "004 - ธนาคารกสิกรไทย", ["BankAccount"] = "0123456789",
    };

    private static ParsedFile Parse(byte[] file) => EmployeeImportParser.Parse(new MemoryStream(file), Refs);

    [Fact]
    public void A_correct_file_parses_with_no_issues()
    {
        var parsed = Parse(File(
            [new() { ["OrgCode"] = "HR", ["OrgName"] = "ฝ่ายบุคคล" }],
            [Employee("E001", "110170020345", "HR"), Employee("E002", "310170020345", "HR", EmployeeImportSchema.PayDaily, 500m)]));
        Assert.Empty(parsed.Issues);
        Assert.Equal(2, parsed.Employees.Count);
        var daily = parsed.Employees.Single(e => e.EmpNo == "E002");
        Assert.True(daily.IsDaily);
        Assert.Equal("F", daily.Sex);          // derived from นางสาว
        Assert.Equal("3", daily.PrenameCode);
        Assert.Equal("004", daily.BankCode);   // "004 - ธนาคารกสิกรไทย" → code
        Assert.Equal("0123456789", daily.BankAccount);   // leading zero kept
    }

    [Fact]
    public void Home_address_columns_are_optional_and_read_when_filled()
    {
        var withAddress = Employee("E001", "110170020345", "HR");
        withAddress["AddrNo"] = "99/1"; withAddress["AddrMoo"] = "4"; withAddress["AddrSubdistrict"] = "จอมพล";
        withAddress["AddrDistrict"] = "จตุจักร"; withAddress["AddrProvince"] = "กรุงเทพมหานคร"; withAddress["AddrPostcode"] = "10900";
        var badPostcode = Employee("E003", "510170020345", "HR");
        badPostcode["AddrPostcode"] = "109";

        var parsed = Parse(File(
            [new() { ["OrgCode"] = "HR", ["OrgName"] = "ฝ่ายบุคคล" }],
            [withAddress, Employee("E002", "310170020345", "HR"), badPostcode]));

        var e1 = parsed.Employees.Single(e => e.EmpNo == "E001");
        Assert.Equal(("99/1", "4", "จอมพล", "จตุจักร", "กรุงเทพมหานคร", "10900"),
            (e1.Address!.No, e1.Address.Moo, e1.Address.Subdistrict, e1.Address.District, e1.Address.Province, e1.Address.Postcode));
        Assert.Null(parsed.Employees.Single(e => e.EmpNo == "E002").Address);   // ไม่กรอกที่อยู่ = ไม่แตะตาราง address
        Assert.Contains(parsed.Issues, i => i.Column.Contains("ไปรษณีย์") || i.Column == "AddrPostcode");
        Assert.DoesNotContain(parsed.Employees, e => e.EmpNo == "E003");
    }

    [Fact]
    public void Every_problem_is_reported_with_sheet_row_and_column()
    {
        var bad = Employee("E001", "110170020345", "NOPE");
        bad["IdCard"] = "1101700203451";       // wrong check digit
        bad["BankAccount"] = "12-AB";
        bad["OrgCode"] = "NO-PE";       // unit codes keep their dashes — "NO-PE" is reported as-is, not "NOPE"
        var dup = Employee("E001", "310170020345", "NOPE");
        var parsed = Parse(File([], [bad, dup]));
        Assert.Contains(parsed.Issues, i => i.Row == 2 && i.Column == "เลขบัตรประชาชน");
        Assert.Contains(parsed.Issues, i => i.Row == 2 && i.Column == "รหัสหน่วยงาน" && i.Message.Contains("NO-PE"));
        Assert.Contains(parsed.Issues, i => i.Row == 2 && i.Column == "เลขที่บัญชี");
        Assert.Contains(parsed.Issues, i => i.Row == 3 && i.Message.Contains("ซ้ำ"));
        Assert.Empty(parsed.Employees.Where(e => e.Row == 2));
    }

    [Fact]
    public void Buddhist_era_dates_are_converted()
    {
        var e = Employee("E001", "110170020345", "HR");
        e["HireDate"] = "01/06/2567";
        var parsed = Parse(File([new() { ["OrgCode"] = "HR", ["OrgName"] = "ฝ่ายบุคคล" }], [e]));
        Assert.Empty(parsed.Issues);
        Assert.Equal(new DateTime(2024, 6, 1), parsed.Employees[0].HireDate);
    }

    [Fact]
    public void A_parent_loop_between_units_is_rejected()
    {
        var parsed = Parse(File(
            [new() { ["OrgCode"] = "A", ["OrgName"] = "A", ["ParentCode"] = "B" }, new() { ["OrgCode"] = "B", ["OrgName"] = "B", ["ParentCode"] = "A" }],
            []));
        Assert.Contains(parsed.Issues, i => i.Message.Contains("วนกลับ"));
    }

    [Fact]
    public void Missing_required_header_is_an_error_not_a_crash()
    {
        using var wb = new XLWorkbook(new MemoryStream(EmployeeImportTemplateBuilder.Build(Lookups)));
        wb.Worksheet(EmployeeImportSchema.Employee.Name).Cell(1, 1).Value = "รหัส";   // renamed header
        using var ms = new MemoryStream(); wb.SaveAs(ms);
        var parsed = Parse(ms.ToArray());
        Assert.Contains(parsed.Issues, i => i.Row == 1 && i.Column == "รหัสพนักงาน*");
    }
}
