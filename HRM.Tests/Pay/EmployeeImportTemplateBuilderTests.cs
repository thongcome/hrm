using ClosedXML.Excel;
using HRM.Services.Hr.EmployeeImport;
using Xunit;

namespace HRM.Tests.Hr;

public class EmployeeImportTemplateBuilderTests
{
    private static readonly EmployeeImportLookups Lookups = new(
        "ADVD", "บริษัท แอดวานซ์ ดิจิทัล จำกัด",
        [new("002", "ธนาคารกรุงเทพ"), new("004", "ธนาคารกสิกรไทย")],
        [new("A01", "พนักงาน"), new("A04", "ผู้จัดการแผนก")],
        [new("AD-HRM", "ฝ่ายทรัพยากรบุคคล", null, "AD0005"), new("AD-HRM-02", "แผนกค่าตอบแทนและสวัสดิการ", "AD-HRM", "AD0024")]);

    private static XLWorkbook Open() => new(new MemoryStream(EmployeeImportTemplateBuilder.Build(Lookups)));

    [Fact]
    public void Every_schema_sheet_has_its_headers_in_order()
    {
        using var wb = Open();
        foreach (var sheet in EmployeeImportSchema.DataSheets)
        {
            var ws = wb.Worksheet(sheet.Name);
            for (var i = 0; i < sheet.Columns.Count; i++)
                Assert.Equal(sheet.Columns[i].Header, ws.Cell(1, i + 1).GetString());
        }
    }

    [Fact]
    public void Guide_is_first_and_lists_sheet_is_hidden()
    {
        using var wb = Open();
        Assert.Equal(EmployeeImportTemplateBuilder.GuideSheetName, wb.Worksheet(1).Name);
        Assert.Equal(XLWorksheetVisibility.Hidden, wb.Worksheet(EmployeeImportTemplateBuilder.ListsSheetName).Visibility);
    }

    [Fact]
    public void Existing_org_tree_is_prefilled()
    {
        using var wb = Open();
        var ws = wb.Worksheet(EmployeeImportSchema.Org.Name);
        Assert.Equal("AD-HRM", ws.Cell(2, 1).GetString());
        Assert.Equal("AD-HRM-02", ws.Cell(3, 1).GetString());
        Assert.Equal("AD-HRM", ws.Cell(3, 3).GetString());
    }

    [Fact]
    public void Bank_dropdown_offers_code_and_name()
    {
        using var wb = Open();
        var lists = wb.Worksheet(EmployeeImportTemplateBuilder.ListsSheetName);
        var bankCol = lists.Row(1).CellsUsed().First(c => c.GetString() == EmployeeImportSchema.ListBank).Address.ColumnNumber;
        Assert.Equal("004 - ธนาคารกสิกรไทย", lists.Cell(3, bankCol).GetString());
    }

    [Fact]
    public void Employee_sheet_has_validation_on_required_list_and_date_columns()
    {
        using var wb = Open();
        var ws = wb.Worksheet(EmployeeImportSchema.Employee.Name);
        var cols = EmployeeImportSchema.Employee.Columns;
        foreach (var key in new[] { "Prename", "HireDate", "OrgCode", "PayType", "Bank", "IdCard" })
        {
            var col = cols.ToList().FindIndex(c => c.Key == key) + 1;
            Assert.True(ws.Cell(2, col).HasDataValidation, $"{key} should be validated");
        }
    }

    [Fact]
    public void Id_card_example_passes_the_thai_checksum()
    {
        var example = EmployeeImportSchema.Employee.Columns.Single(c => c.Key == "IdCard").Example;
        var sum = 0;
        for (var i = 0; i < 12; i++) sum += (example[i] - '0') * (13 - i);
        Assert.Equal((11 - sum % 11) % 10, example[12] - '0');
    }
}
