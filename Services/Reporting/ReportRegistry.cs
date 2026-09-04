using HRM.Models.Reporting;

namespace HRM.Services.Reporting;

// Collects every IReportDefinition registered in DI and every IReportExporter,
// so the viewer/endpoint can look a report up by Code and export in any
// available format without knowing the concrete types. Pure lookup — no state.
public class ReportRegistry(IEnumerable<IReportDefinition> reports, IEnumerable<IReportExporter> exporters)
{
    private readonly Dictionary<string, IReportDefinition> _byCode =
        reports.ToDictionary(r => r.Code, StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IReportExporter> _exportersByFormat =
        exporters.ToDictionary(e => e.Format, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<IReportDefinition> All => _byCode.Values
        .OrderBy(r => r.Category).ThenBy(r => r.Name).ToList();

    public IEnumerable<IGrouping<string, IReportDefinition>> ByCategory =>
        All.GroupBy(r => r.Category);

    public IReportDefinition? Find(string code) =>
        _byCode.TryGetValue(code, out var r) ? r : null;

    public IReadOnlyList<IReportExporter> Exporters =>
        _exportersByFormat.Values.ToList();

    public IReportExporter? Exporter(string format) =>
        _exportersByFormat.TryGetValue(format, out var e) ? e : null;
}
