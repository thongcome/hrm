using ClosedXML.Excel;
using HRM.Models.Reporting;

namespace HRM.Services.Reporting;

// Real .xlsx via ClosedXML: a title row, a bold header, typed cells (numbers
// stay numbers with a display format so the sheet is computable), and a bold
// totals row. One sheet named after the report.
public class ExcelReportExporter : IReportExporter
{
    public string Format => "excel";
    public string Label => "Excel";
    public string ContentType => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    public string Extension => "xlsx";

    public byte[] Export(ReportResult result)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Report");

        var colCount = result.Columns.Count;
        var r = 1;

        // Title (merged across columns)
        ws.Cell(r, 1).Value = result.Title;
        ws.Range(r, 1, r, Math.Max(1, colCount)).Merge().Style.Font.SetBold().Font.SetFontSize(13);
        r++;
        if (!string.IsNullOrWhiteSpace(result.Subtitle))
        {
            ws.Cell(r, 1).Value = result.Subtitle;
            ws.Range(r, 1, r, Math.Max(1, colCount)).Merge().Style.Font.SetItalic().Font.SetFontColor(XLColor.Gray);
            r++;
        }
        r++; // blank spacer

        // Header
        var headerRow = r;
        for (var c = 0; c < colCount; c++)
        {
            var cell = ws.Cell(headerRow, c + 1);
            cell.Value = result.Columns[c].Label;
            cell.Style.Font.SetBold();
            cell.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#1565C0"));
            cell.Style.Font.SetFontColor(XLColor.White);
            cell.Style.Alignment.Horizontal = result.Columns[c].IsNumeric
                ? XLAlignmentHorizontalValues.Right : XLAlignmentHorizontalValues.Left;
        }
        r++;

        foreach (var row in result.Rows)
        {
            WriteRow(ws, r, result.Columns, row, bold: false);
            r++;
        }

        if (result.Totals is not null)
        {
            WriteRow(ws, r, result.Columns, result.Totals, bold: true);
            ws.Range(r, 1, r, Math.Max(1, colCount)).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#E3F2FD"));
        }

        ws.Columns().AdjustToContents();
        ws.SheetView.FreezeRows(headerRow);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static void WriteRow(IXLWorksheet ws, int r, IReadOnlyList<ReportColumn> cols, IReadOnlyDictionary<string, object?> row, bool bold)
    {
        for (var c = 0; c < cols.Count; c++)
        {
            var col = cols[c];
            row.TryGetValue(col.Key, out var v);
            var cell = ws.Cell(r, c + 1);

            if (col.IsNumeric && ReportFormatting.ToDecimal(v) is decimal num)
            {
                cell.Value = num;
                cell.Style.NumberFormat.Format = col.Type switch
                {
                    ReportColumnType.Money => "#,##0.00",
                    ReportColumnType.Percent => "#,##0.##\"%\"",
                    _ => "#,##0.###",
                };
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            }
            else
            {
                cell.Value = ReportFormatting.Text(v, col.Type);
            }
            if (bold) cell.Style.Font.SetBold();
        }
    }
}
