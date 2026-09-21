using ClosedXML.Excel;
using HRM.Services.Hr.EmployeeImport;
using Xunit;

namespace HRM.Tests.Hr;

public class EmployeeImportErrorMarkerTests
{
    private static readonly ImportReferenceData Refs = new(
        new HashSet<string> { "004" }, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "A01" },
        new HashSet<string>(StringComparer.OrdinalIgnoreCase));

    [Fact]
    public void Problems_are_marked_on_the_customers_own_cells_and_listed_first()
    {
        var bad = EmployeeImportParserTests.Employee("E001", "110170020345", "HR");
        bad["IdCard"] = "1101700203451";   // wrong check digit
        var file = EmployeeImportParserTests.File([], [bad]);
        var issues = EmployeeImportParser.Parse(new MemoryStream(file), Refs).Issues;
        Assert.NotEmpty(issues);

        using var wb = new XLWorkbook(new MemoryStream(EmployeeImportErrorMarker.Mark(file, issues)));

        var summary = wb.Worksheet(1);
        Assert.Equal(EmployeeImportErrorMarker.SummarySheet, summary.Name);
        Assert.Equal(issues.Count + 1, summary.LastRowUsed()!.RowNumber());

        var emp = wb.Worksheet(EmployeeImportSchema.Employee.Name);
        var idCol = emp.Row(1).CellsUsed().First(c => c.GetString().TrimEnd('*') == "เลขบัตรประชาชน").Address.ColumnNumber;
        var cell = emp.Cell(2, idCol);
        Assert.Equal("1101700203451", cell.GetString());          // the customer's value is untouched
        Assert.True(cell.HasComment);
        Assert.Contains("เลขบัตรประชาชนไม่ถูกต้อง", cell.GetComment().Text);
        Assert.NotEqual(XLColor.NoColor, cell.Style.Fill.BackgroundColor);
    }
}
