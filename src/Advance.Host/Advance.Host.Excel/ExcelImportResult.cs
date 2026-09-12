namespace Advance.Host.Excel;

/// <summary>
/// One row-level problem found while importing a spreadsheet — reported
/// instead of thrown, so a single bad row (e.g. row 214 of a 3,000-row
/// employee import) doesn't abort the whole file.
/// </summary>
public record ExcelImportRowError(int RowNumber, string Message);

/// <summary>
/// The result of <see cref="ExcelImporter.Read{T}"/>: every row that mapped
/// successfully, plus every row that didn't and why. Generic over T so the
/// same helper serves any "นำเข้า Excel" screen (employee list, org chart,
/// OT/attendance corrections — see Plan_Split_Payroll_Workflow_v1.1.md
/// section 3.2) rather than one bespoke importer per module.
/// </summary>
public class ExcelImportResult<T>
{
    public List<T> Rows { get; } = new();
    public List<ExcelImportRowError> Errors { get; } = new();

    public bool HasErrors => Errors.Count > 0;
    public int SuccessCount => Rows.Count;
    public int ErrorCount => Errors.Count;

    public void AddRow(T row) => Rows.Add(row);
    public void AddError(int rowNumber, string message) => Errors.Add(new ExcelImportRowError(rowNumber, message));
}
