using ClosedXML.Excel;

namespace HRM.Services.Hr.EmployeeImport;

// Hands the customer's own file back with every problem marked where it is: the cell turns red
// and carries a comment with the message, plus a "ข้อผิดพลาด" sheet listing everything (opened
// first). On a 2,000-row file, fixing in place beats matching a list of row numbers by hand.
// Pure: bytes in, bytes out — nothing is stored.
public static class EmployeeImportErrorMarker
{
    public const string SummarySheet = "ข้อผิดพลาด";
    private static readonly XLColor Bad = XLColor.FromHtml("#FFC7CE");

    public static byte[] Mark(byte[] original, IReadOnlyList<ImportIssue> issues)
    {
        using var wb = new XLWorkbook(new MemoryStream(original));

        foreach (var group in issues.Where(i => i.Row > 0).GroupBy(i => (i.Sheet, i.Row, i.Column)))
        {
            if (!wb.TryGetWorksheet(group.Key.Sheet, out var ws)) continue;
            var col = FindColumn(ws, group.Key.Column);
            // a problem with no single cell (e.g. a missing header) marks the row's first cell
            var cell = ws.Cell(group.Key.Row, col ?? 1);
            cell.Style.Fill.BackgroundColor = Bad;
            var text = string.Join("\n", group.Select(i => (col is null && i.Column.Length > 0 ? i.Column + ": " : "") + i.Message));
            if (cell.HasComment) cell.GetComment().AddNewLine().AddText(text);
            else cell.CreateComment().AddText(text);
        }

        if (wb.TryGetWorksheet(SummarySheet, out var old)) old.Delete();
        var sum = wb.Worksheets.Add(SummarySheet, 1);
        string[] headers = ["ชีต", "แถว", "คอลัมน์", "ปัญหา"];
        for (var c = 0; c < headers.Length; c++) sum.Cell(1, c + 1).Value = headers[c];
        sum.Row(1).Style.Font.Bold = true;
        sum.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml("#1B2A4A");
        sum.Row(1).Style.Font.FontColor = XLColor.White;
        var r = 2;
        foreach (var i in issues.OrderBy(i => i.Sheet).ThenBy(i => i.Row))
        {
            sum.Cell(r, 1).Value = i.Sheet;
            if (i.Row > 0) sum.Cell(r, 2).Value = i.Row; else sum.Cell(r, 2).Value = "-";
            sum.Cell(r, 3).Value = i.Column;
            sum.Cell(r, 4).Value = i.Message;
            r++;
        }
        sum.Column(1).Width = 14; sum.Column(2).Width = 8; sum.Column(3).Width = 24; sum.Column(4).Width = 90;
        sum.SheetView.FreezeRows(1);
        foreach (var ws in wb.Worksheets) ws.TabSelected = false;
        sum.SetTabActive();
        sum.TabSelected = true;

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // issues name the column by its header text without the required-marker "*" (see the parser)
    private static int? FindColumn(IXLWorksheet ws, string header)
    {
        if (string.IsNullOrWhiteSpace(header)) return null;
        var want = Norm(header);
        return ws.Row(1).CellsUsed().FirstOrDefault(c => Norm(c.GetString()) == want)?.Address.ColumnNumber;
    }

    private static string Norm(string s) => s.Trim().TrimEnd('*').Replace(" ", "");
}
