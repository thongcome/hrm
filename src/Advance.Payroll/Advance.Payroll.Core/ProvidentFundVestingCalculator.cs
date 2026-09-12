namespace Advance.Payroll.Core;

// Ported from HRM Services/Pay/Calculators/ProvidentFundVestingCalculator.cs, with
// Pay_ProvidentFundVestingTier -> ProvidentFundVestingTier (trimmed POCO in Pocos.cs).
public static class ProvidentFundVestingCalculator
{
    public record VestingResult(int ServiceDays, decimal YearsOfService, decimal VestingPercent, string? MatchedTierNote);

    public static VestingResult ResolveVesting(DateOnly hireDate, DateOnly asOfDate, IReadOnlyList<ProvidentFundVestingTier> tiers)
    {
        if (asOfDate < hireDate)
            throw new ArgumentException("asOfDate must not be before hireDate");

        var serviceDays = asOfDate.DayNumber - hireDate.DayNumber + 1;
        var years = Math.Round(serviceDays / 365m, 2);

        if (tiers.Count == 0)
            return new VestingResult(serviceDays, years, 100m, null);

        var matched = tiers
            .OrderBy(t => t.MinYearsOfService)
            .FirstOrDefault(t => years >= t.MinYearsOfService && (t.MaxYearsOfService == null || years < t.MaxYearsOfService));

        return matched is null
            ? new VestingResult(serviceDays, years, 100m, null)
            : new VestingResult(serviceDays, years, matched.VestingPercent, $"{matched.MinYearsOfService}-{matched.MaxYearsOfService?.ToString() ?? "+"} ปี");
    }
}
