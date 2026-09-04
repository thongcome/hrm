using HRM.Models.Reporting;

namespace HRM.Services.Reporting;

// One report = one class implementing this. Register it in DI as
// IReportDefinition (see Program.cs) and it appears in the Report Center,
// gets a dynamic parameter form, renders online, and exports to every format
// — no page, no export code, no registry edit. Keep Code stable (it's in URLs
// and export filenames); Category groups reports in the center; Parameters
// declares the inputs the generic form builds.
public interface IReportDefinition
{
    string Code { get; }
    string Category { get; }
    string Name { get; }
    string? Description { get; }
    IReadOnlyList<ReportParameter> Parameters { get; }

    Task<ReportResult> RunAsync(IReadOnlyDictionary<string, string?> args, ReportContext ctx, CancellationToken ct = default);
}

// Optional companion a report implements when a Select/Period/Year/Organization
// parameter's choices are data-driven (e.g. the company's evaluation periods or
// payroll runs) rather than a fixed list. The viewer calls this to fill those
// dropdowns; reports with only static/typed parameters don't implement it.
public interface IReportDynamicOptions
{
    Task<IReadOnlyList<ReportParamOption>> GetOptionsAsync(string parameterKey, ReportContext ctx, CancellationToken ct = default);
}
