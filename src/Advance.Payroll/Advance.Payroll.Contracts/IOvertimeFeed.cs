namespace Advance.Payroll.Contracts;

// Replaces Services/Pay/Calculators/OvertimeEarningsCalculator.cs's
// GetOvertimeForPeriodByEmployeeAsync (queries HrwOt, the legacy OT module) — called
// from PayrollCalculationService.cs:273/558. Keyed by EmpNo (matching the original,
// which grouped by HrwOt.EmpNo) rather than HremployeeId — a pre-existing join quirk
// in the legacy OT table, not something this seam should silently "fix".
// Advance.Payroll Lite: no shift/OT module either — its implementation sources OT from
// a per-period "รายการเฉพาะกิจ" entry (already modeled as Pay_AdhocPayItem) instead,
// which means Lite's IOvertimeFeed implementation can simply return an empty feed and
// route OT through the ad-hoc pay item pathway that already exists.
public interface IOvertimeFeed
{
    Task<IReadOnlyDictionary<string, OvertimeSummary>> GetOvertimeForPeriodAsync(
        string companyId, DateOnly periodStart, DateOnly periodEnd, CancellationToken ct = default);
}

public sealed record OvertimeSummary(decimal Amount, int RecordCount);
