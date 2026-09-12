namespace Advance.Payroll.Contracts;

// Replaces `context.Att_DailyAttendances.Where(...)` in PayrollCalculationService.cs:255-260.
// HRM implements this from Att_DailyAttendance (the real time-clock/shift module).
// Advance.Payroll Lite has no time/shift module (see plan section 5 "ไม่มี") — its
// implementation returns an empty feed unless/until HR keys in late/absence rows on a
// simple "รายการเฉพาะกิจ" screen, or imports them from Excel.
public interface IAttendanceFeed
{
    Task<IReadOnlyDictionary<long, IReadOnlyList<AttendanceDay>>> GetAttendanceForPeriodAsync(
        string companyId, DateOnly periodStart, DateOnly periodEnd, CancellationToken ct = default);
}

// HremployeeId is the dictionary key above, not repeated per-row.
public sealed record AttendanceDay(DateOnly WorkDate, bool IsAbsent, int LateMinutes);
