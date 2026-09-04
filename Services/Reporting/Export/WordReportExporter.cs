using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using HRM.Models.Reporting;

namespace HRM.Services.Reporting;

// Real .docx via OpenXML: a title, optional subtitle, and a bordered table with
// a shaded header row and a bold totals row. Verbose by nature (OpenXML has no
// high-level table helper), but produces a genuine Word document, not HTML.
public class WordReportExporter : IReportExporter
{
    public string Format => "word";
    public string Label => "Word";
    public string ContentType => "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
    public string Extension => "docx";

    public byte[] Export(ReportResult result)
    {
        using var ms = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(ms, DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
        {
            var main = doc.AddMainDocumentPart();
            main.Document = new Document();
            var body = main.Document.AppendChild(new Body());

            body.AppendChild(TextParagraph(result.Title, bold: true, size: 28));
            if (!string.IsNullOrWhiteSpace(result.Subtitle))
                body.AppendChild(TextParagraph(result.Subtitle!, italic: true, size: 18, color: "808080"));

            var table = new Table();
            table.AppendChild(new TableProperties(
                new TableBorders(
                    new TopBorder { Val = BorderValues.Single, Size = 4 },
                    new BottomBorder { Val = BorderValues.Single, Size = 4 },
                    new LeftBorder { Val = BorderValues.Single, Size = 4 },
                    new RightBorder { Val = BorderValues.Single, Size = 4 },
                    new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                    new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 }),
                new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct }));

            // header
            var headerRow = new TableRow();
            foreach (var c in result.Columns)
                headerRow.AppendChild(Cell(c.Label, bold: true, shade: "1565C0", fontColor: "FFFFFF", right: c.IsNumeric));
            table.AppendChild(headerRow);

            foreach (var row in result.Rows)
            {
                var tr = new TableRow();
                foreach (var c in result.Columns)
                {
                    row.TryGetValue(c.Key, out var v);
                    tr.AppendChild(Cell(ReportFormatting.Text(v, c.Type), right: c.IsNumeric));
                }
                table.AppendChild(tr);
            }

            if (result.Totals is not null)
            {
                var tr = new TableRow();
                foreach (var c in result.Columns)
                {
                    result.Totals.TryGetValue(c.Key, out var v);
                    tr.AppendChild(Cell(ReportFormatting.Text(v, c.Type), bold: true, shade: "E3F2FD", right: c.IsNumeric));
                }
                table.AppendChild(tr);
            }

            body.AppendChild(table);
            main.Document.Save();
        }
        return ms.ToArray();
    }

    private static Paragraph TextParagraph(string text, bool bold = false, bool italic = false, int size = 22, string? color = null)
    {
        var runProps = new RunProperties(new RunFonts { Ascii = "Tahoma", HighAnsi = "Tahoma", ComplexScript = "Tahoma" }, new FontSize { Val = size.ToString() }, new FontSizeComplexScript { Val = size.ToString() });
        if (bold) { runProps.AppendChild(new Bold()); runProps.AppendChild(new BoldComplexScript()); }
        if (italic) runProps.AppendChild(new Italic());
        if (color is not null) runProps.AppendChild(new Color { Val = color });
        return new Paragraph(new Run(runProps, new Text(text) { Space = SpaceProcessingModeValues.Preserve }));
    }

    private static TableCell Cell(string text, bool bold = false, string? shade = null, string? fontColor = null, bool right = false)
    {
        var runProps = new RunProperties(new RunFonts { Ascii = "Tahoma", HighAnsi = "Tahoma", ComplexScript = "Tahoma" }, new FontSize { Val = "18" }, new FontSizeComplexScript { Val = "18" });
        if (bold) { runProps.AppendChild(new Bold()); runProps.AppendChild(new BoldComplexScript()); }
        if (fontColor is not null) runProps.AppendChild(new Color { Val = fontColor });

        var paraProps = new ParagraphProperties(new Justification { Val = right ? JustificationValues.Right : JustificationValues.Left });
        var para = new Paragraph(paraProps, new Run(runProps, new Text(text) { Space = SpaceProcessingModeValues.Preserve }));

        var cellProps = new TableCellProperties();
        if (shade is not null)
            cellProps.AppendChild(new Shading { Val = ShadingPatternValues.Clear, Fill = shade });
        return new TableCell(cellProps, para);
    }
}
