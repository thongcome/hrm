namespace HRM.Services.Shared;

// Months-of-service calculation. Was previously duplicated inline in
// ProvidentFundRateChangeRequestService.cs and WorkforceReport.razor (each
// with its own day-diff/365.25 approach) — extracted here on this, its third
// use (Leave eligibility, Lve_LeavePolicy.MinServiceMonths), per the standing
// "extract once a second/third use case proves the pattern" principle also
// behind DirectReportResolverHelper/OrgEmployeeResolverHelper. Static, not a
// DI service — same convention as those.
public static class TenureHelper
{
    // Whole months of service between workDate and asOf — null workDate (hire
    // date unknown) returns null so callers can decide how to treat "unknown"
    // rather than silently guessing 0 or int.MaxValue.
    public static int? MonthsOfService(DateTime? workDate, DateOnly asOf)
    {
        if (workDate is null) return null;

        var hireDate = DateOnly.FromDateTime(workDate.Value);
        if (hireDate > asOf) return null;

        var months = (asOf.Year - hireDate.Year) * 12 + (asOf.Month - hireDate.Month);
        if (asOf.Day < hireDate.Day) months--;
        return Math.Max(0, months);
    }
}
