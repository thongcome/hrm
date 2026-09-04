namespace HRM.Models.Reporting;

// The report framework is deliberately data-shaped, not page-shaped: a report
// is a class that declares its parameters and returns a ReportResult (columns +
// rows + optional totals). One generic viewer renders any ReportResult online,
// and one exporter-per-format turns the SAME ReportResult into Excel/PDF/Word/
// CSV — so adding a report is adding one class, never a new page or a new
// export path. This is the config-first spine: the framework knows nothing
// about any specific report, and every report is discovered from DI.

public enum ReportParamType
{
    Text,
    Number,
    Date,
    Select,       // fixed Options list
    Company,      // resolved to the current user's company automatically if omitted
    Organization, // org picker (com_organization subtree)
    Period,       // a named date range / payroll period
    Year,
}

public record ReportParamOption(string Value, string Label);

public record ReportParameter(
    string Key,
    string Label,
    ReportParamType Type,
    bool Required = false,
    string? DefaultValue = null,
    IReadOnlyList<ReportParamOption>? Options = null,
    string? HelperText = null);

public enum ReportColumnType { Text, Number, Money, Date, Percent }

public record ReportColumn(
    string Key,
    string Label,
    ReportColumnType Type = ReportColumnType.Text)
{
    public bool IsNumeric => Type is ReportColumnType.Number or ReportColumnType.Money or ReportColumnType.Percent;
}

// A finished report: title (with the parameter context baked in), the column
// definitions, the rows (keyed by column Key), and an optional totals row that
// the viewer pins and the exporters render in bold. Cell values are raw
// objects; formatting is driven by the column Type, in one place, so online and
// every export format agree.
public record ReportResult(
    string Title,
    IReadOnlyList<ReportColumn> Columns,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows,
    IReadOnlyDictionary<string, object?>? Totals = null,
    string? Subtitle = null);

// Ambient context passed to every report run — who is asking and for which
// company, so a report never has to re-resolve the caller.
public record ReportContext(string CompanyId, long UserId, string? UserName = null);
