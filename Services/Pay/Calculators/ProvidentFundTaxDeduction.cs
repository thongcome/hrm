namespace HRM.Services.Pay.Calculators;

// How much of the employee's provident-fund contribution lowers taxable income (Revenue Code
// ม.47(1)(ญ) + ministerial rules): at most 15% of wages, and the retirement-savings group together
// (PVD + RMF + SSF + pension insurance) at most 500,000 a year. The money above the limit still goes
// into the fund; it just doesn't reduce tax. Before this, only the 500,000 cap was applied and it
// ignored RMF/SSF the employee had elected, so high earners were under-withheld (audit M-02).
public static class ProvidentFundTaxDeduction
{
    // Which elections share the cap is configured per year (Pay_TaxDeductionType.CapGroup = RETIREMENT);
    // TaxDeductionRules reports how much of it the elections used.
    public const decimal WageShareCap = 0.15m;

    /// <summary>Room left in the retirement-savings cap for PVD this year.</summary>
    public static decimal AnnualRoom(decimal groupCapPerYear, decimal retirementGroupElected, decimal ytdPvdDeducted)
        => Math.Max(0m, groupCapPerYear - retirementGroupElected - ytdPvdDeducted);

    /// <summary>Deductible part of this period's contribution.</summary>
    public static decimal Deductible(decimal contribution, decimal wageBase, decimal annualRoom)
        => Math.Max(0m, Math.Min(contribution, Math.Min(Math.Round(wageBase * WageShareCap, 2, MidpointRounding.AwayFromZero), annualRoom)));
}
