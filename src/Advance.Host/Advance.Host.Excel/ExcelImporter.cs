using ClosedXML.Excel;

namespace Advance.Host.Excel;

/// <summary>
/// Generic, reusable .xlsx import helper — genuinely domain-agnostic (no
/// Payroll/Workflow/Employee types referenced anywhere in this file), so it
/// can be the one "นำเข้า Excel" building block the plan calls for in both
/// standalone products (importing people, org charts, OT/absence corrections,
/// ...) instead of each screen writing its own ClosedXML loop. Deliberately
/// mirrors the read-only, row-by-row shape of
/// Services/Reporting/Export/ExcelReportExporter.cs's write-side counterpart
/// already in HRM, so the two feel like the same family of helper.
/// </summary>
public static class ExcelImporter
{
    /// <summary>
    /// Reads rows from the first worksheet of an .xlsx stream, starting after
    /// <paramref name="headerRowNumber"/> header rows, mapping each data row
    /// with <paramref name="mapRow"/>. A row whose mapper throws is recorded
    /// as a row-level error (with the 1-based Excel row number for a useful
    /// error message back to the uploader) and skipped — the import as a
    /// whole never throws for bad data, only for a structurally unreadable
    /// file (not a valid .xlsx, no worksheets, etc).
    /// </summary>
    /// <typeparam name="T">The row type the caller's mapper produces.</typeparam>
    /// <param name="stream">The uploaded file's content stream.</param>
    /// <param name="mapRow">
    /// Maps one worksheet row to a <typeparamref name="T"/>. Throw
    /// <see cref="ExcelRowValidationException"/> (or let any exception
    /// propagate) to reject a row — its message is captured verbatim.
    /// </param>
    /// <param name="headerRowNumber">
    /// How many leading rows to skip as header(s). Default 1 (a single
    /// header row) — pass 0 for a headerless sheet.
    /// </param>
    /// <param name="skipBlankRows">
    /// Skip rows where every cell in the row is empty (default true) —
    /// trailing blank rows are extremely common in real-world uploaded
    /// spreadsheets and are not import errors.
    /// </param>
    public static ExcelImportResult<T> Read<T>(
        Stream stream,
        Func<IXLRow, T> mapRow,
        int headerRowNumber = 1,
        bool skipBlankRows = true)
    {
        var result = new ExcelImportResult<T>();

        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.FirstOrDefault()
            ?? throw new InvalidOperationException("The uploaded workbook has no worksheets.");

        var lastRowUsed = worksheet.LastRowUsed();
        if (lastRowUsed is null)
            return result; // empty sheet — not an error, just nothing to import

        var firstDataRow = headerRowNumber + 1;
        var lastRowNumber = lastRowUsed.RowNumber();

        for (var rowNumber = firstDataRow; rowNumber <= lastRowNumber; rowNumber++)
        {
            var row = worksheet.Row(rowNumber);

            if (skipBlankRows && row.IsEmpty())
                continue;

            try
            {
                var mapped = mapRow(row);
                result.AddRow(mapped);
            }
            catch (Exception ex)
            {
                result.AddError(rowNumber, ex.Message);
            }
        }

        return result;
    }
}

/// <summary>
/// Thrown by a caller's <c>mapRow</c> delegate to reject one row with a
/// specific, user-facing message (e.g. "EmpNo is required"). Any other
/// exception type is also caught and recorded the same way — this type
/// exists only so validation code reads intentionally rather than abusing a
/// generic exception type for control flow.
/// </summary>
public class ExcelRowValidationException : Exception
{
    public ExcelRowValidationException(string message) : base(message)
    {
    }
}
