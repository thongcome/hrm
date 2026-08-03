namespace HRM.Services.Pay.Calculators;

using HRM.Models;

// Pure, no DB access. Resolves what % of the EMPLOYER's provident-fund
// contribution an employee is entitled to, based on years of continuous
// service, against a company-configured vesting tier table. The employee's
// own contribution is never subject to vesting (always 100%) — this
// calculator is only for the employer-match portion, and only matters at
// exit/withdrawal time, not during the monthly payroll deduction (see
// ProvidentFundCalculator for that).
//
// Day-counting mirrors SeveranceCalculator.cs's style (DateOnly.DayNumber
// arithmetic), but the tier lookup here is a configurable table rather than
// a hardcoded switch, because — unlike severance (a statutory table) — PF
// vesting has no legal standard; each fund's own "ข้อบังคับกองทุน" sets it.
public static class ProvidentFundVestingCalculator
{
    public record VestingResult(int ServiceDays, decimal YearsOfService, decimal VestingPercent, string? MatchedTierNote);

    public static VestingResult ResolveVesting(DateOnly hireDate, DateOnly asOfDate, IReadOnlyList<Pay_ProvidentFundVestingTier> tiers)
    {
        if (asOfDate < hireDate)
            throw new ArgumentException("asOfDate must not be before hireDate");

        var serviceDays = asOfDate.DayNumber - hireDate.DayNumber + 1;
        var years = Math.Round(serviceDays / 365m, 2);

        // No tiers configured = full 100% vesting. HR must explicitly set up
        // tiers before an employee's entitlement can be reduced — this is
        // never the default that silently shortchanges someone.
        if (tiers.Count == 0)
            return new VestingResult(serviceDays, years, 100m, null);

        var matched = tiers
            .OrderBy(t => t.MinYearsOfService)
            .FirstOrDefault(t => years >= t.MinYearsOfService && (t.MaxYearsOfService == null || years < t.MaxYearsOfService));

        return matched is null
            // Service exceeds every configured tier ceiling = fully vested.
            ? new VestingResult(serviceDays, years, 100m, null)
            : new VestingResult(serviceDays, years, matched.VestingPercent, $"{matched.MinYearsOfService}-{matched.MaxYearsOfService?.ToString() ?? "+"} ปี");
    }
}
