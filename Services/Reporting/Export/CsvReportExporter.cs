using System.Text;
using HRM.Models.Reporting;

namespace HRM.Services.Reporting;

// CSV with a UTF-8 BOM so Excel opens Thai text correctly. Numeric columns are
// written unformatted (raw numbers) so the file stays spreadsheet-friendly.
public class CsvReportExporter : IReportExporter
{
    public string Format => "csv";
    public string Label => "CSV";
    public string ContentType => "text/csv";
    public string Extension => "csv";

    public byte[] Export(ReportResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine(Escape(result.Title));
        if (!string.IsNullOrWhiteSpace(result.Subtitle)) sb.AppendLine(Escape(result.Subtitle!));
        sb.AppendLine(string.Join(",", result.Columns.Select(c => Escape(c.Label))));

        foreach (var row in result.Rows)
            sb.AppendLine(string.Join(",", result.Columns.Select(c => Escape(CellText(row, c)))));

        if (result.Totals is not null)
            sb.AppendLine(string.Join(",", result.Columns.Select(c => Escape(CellText(result.Totals, c)))));

        var bom = new byte[] { 0xEF, 0xBB, 0xBF };
        return bom.Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    private static string CellText(IReadOnlyDictionary<string, object?> row, ReportColumn c)
    {
        row.TryGetValue(c.Key, out var v);
        // raw numbers for CSV so spreadsheets can compute on them
        if (c.IsNumeric) return ReportFormatting.ToDecimal(v)?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "";
        return ReportFormatting.Text(v, c.Type);
    }

    private static string Escape(string s)
    {
        if (s.Contains(',') || s.Contains('"') || s.Contains('\n'))
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        return s;
    }
}
