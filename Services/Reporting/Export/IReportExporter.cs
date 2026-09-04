using HRM.Models.Reporting;

namespace HRM.Services.Reporting;

// One implementation per file format. Each takes the SAME ReportResult and
// produces a downloadable byte[]; the framework picks one by Format. Adding a
// format = adding one class registered as IReportExporter.
public interface IReportExporter
{
    string Format { get; }       // "excel" | "pdf" | "word" | "csv"
    string Label { get; }        // button label, e.g. "Excel"
    string ContentType { get; }
    string Extension { get; }    // without dot, e.g. "xlsx"
    byte[] Export(ReportResult result);
}

// Single source of truth for how a cell value is rendered as text, driven by
// the column type — so the online table and every export agree on formatting.
public static class ReportFormatting
{
    public static string Text(object? value, ReportColumnType type)
    {
        if (value is null) return "";
        return type switch
        {
            ReportColumnType.Money => ToDecimal(value)?.ToString("#,##0.00") ?? value.ToString() ?? "",
            ReportColumnType.Number => ToDecimal(value)?.ToString("#,##0.###") ?? value.ToString() ?? "",
            ReportColumnType.Percent => (ToDecimal(value)?.ToString("#,##0.##") ?? value.ToString()) + "%",
            ReportColumnType.Date => value is DateTime dt ? dt.ToString("dd/MM/yyyy")
                : value is DateOnly d ? d.ToString("dd/MM/yyyy") : value.ToString() ?? "",
            _ => value.ToString() ?? "",
        };
    }

    public static decimal? ToDecimal(object? value) => value switch
    {
        null => null,
        decimal m => m,
        double db => (decimal)db,
        float f => (decimal)f,
        int i => i,
        long l => l,
        _ => decimal.TryParse(value.ToString(), out var p) ? p : null,
    };
}
