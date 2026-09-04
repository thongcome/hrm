using HRM.Models.Reporting;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace HRM.Services.Reporting;

// PDF via QuestPDF (already used for payslips/certs). Landscape A4 so wide
// reports fit; Tahoma is the base font because it carries Thai glyphs. Header
// repeats on every page, the totals row is emphasized, and page numbers sit in
// the footer.
public class PdfReportExporter : IReportExporter
{
    public string Format => "pdf";
    public string Label => "PDF";
    public string ContentType => "application/pdf";
    public string Extension => "pdf";

    public byte[] Export(ReportResult result)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1.2f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontFamily("Tahoma").FontSize(9));

                page.Header().Column(col =>
                {
                    col.Item().Text(result.Title).FontSize(14).Bold();
                    if (!string.IsNullOrWhiteSpace(result.Subtitle))
                        col.Item().Text(result.Subtitle!).FontSize(9).FontColor(Colors.Grey.Medium);
                });

                page.Content().PaddingVertical(8).Table(table =>
                {
                    table.ColumnsDefinition(cd =>
                    {
                        foreach (var c in result.Columns)
                        {
                            if (c.IsNumeric) cd.ConstantColumn(80);
                            else cd.RelativeColumn();
                        }
                    });

                    table.Header(header =>
                    {
                        foreach (var c in result.Columns)
                        {
                            header.Cell().Background(Colors.Blue.Darken2).Padding(4)
                                .Text(c.Label).FontColor(Colors.White).Bold();
                        }
                    });

                    foreach (var row in result.Rows)
                    {
                        foreach (var c in result.Columns)
                        {
                            row.TryGetValue(c.Key, out var v);
                            var cell = table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4);
                            var text = cell.Text(ReportFormatting.Text(v, c.Type));
                            if (c.IsNumeric) text.AlignRight();
                        }
                    }

                    if (result.Totals is not null)
                    {
                        foreach (var c in result.Columns)
                        {
                            result.Totals.TryGetValue(c.Key, out var v);
                            var cell = table.Cell().Background(Colors.Blue.Lighten5).Padding(4);
                            var text = cell.Text(ReportFormatting.Text(v, c.Type)).Bold();
                            if (c.IsNumeric) text.AlignRight();
                        }
                    }
                });

                page.Footer().AlignRight().Text(t =>
                {
                    t.CurrentPageNumber(); t.Span(" / "); t.TotalPages();
                });
            });
        }).GeneratePdf();
    }
}
