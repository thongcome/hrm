namespace HRM.Services.Pay.Calculators;

using HRM.Models;

// Fixes the broken progressive tax calculation in PayrollProcess.razor
// CalculateTax (lines ~945-1076 in the legacy file): that method queried
// HRUcfTaxRates for the single bracket containing the income and taxed only
// that bracket at its flat rate — the cumulative "sum tax across every lower
// bracket" behavior it appeared to intend was never actually reached because
// the loop only ever recomputed the same (single) matched row.
//
// This calculator sums tax marginally across every bracket the income passes
// through, which is the correct definition of a progressive tax schedule.
public static class TaxBracketCalculator
{
    public record TaxBracketDetail(
        int Step,
        decimal MinIncome,
        decimal? MaxIncome,
        decimal RatePercent,
        decimal TaxableAmountInBracket,
        decimal TaxInBracket);

    public record TaxCalculationResult(decimal TotalAnnualTax, IReadOnlyList<TaxBracketDetail> Breakdown);

    public static TaxCalculationResult CalculateProgressiveTax(decimal annualizedTaxableIncome, IReadOnlyList<Pay_TaxBracket> brackets)
    {
        if (annualizedTaxableIncome < 0) annualizedTaxableIncome = 0;

        var breakdown = new List<TaxBracketDetail>();
        var totalTax = 0m;

        foreach (var bracket in brackets.Where(b => b.IsActive).OrderBy(b => b.Step))
        {
            if (annualizedTaxableIncome <= bracket.MinIncome)
                break;

            var bracketTop = bracket.MaxIncome ?? decimal.MaxValue;
            var taxableInBracket = Math.Min(annualizedTaxableIncome, bracketTop) - bracket.MinIncome;
            if (taxableInBracket <= 0) continue;

            var taxInBracket = Math.Round(taxableInBracket * bracket.RatePercent / 100m, 2, MidpointRounding.AwayFromZero);
            totalTax += taxInBracket;
            breakdown.Add(new TaxBracketDetail(bracket.Step, bracket.MinIncome, bracket.MaxIncome, bracket.RatePercent, taxableInBracket, taxInBracket));
        }

        return new TaxCalculationResult(totalTax, breakdown);
    }

    // Thai-style withholding: project this period's income across the remaining
    // pay periods of the year (added to what's already accumulated YTD),
    // compute the full annual tax progressively, subtract tax already withheld
    // YTD, and spread the remainder over the remaining periods (including this one).
    //
    // Deductions come in two shapes, handled differently:
    //   - thisPeriodFlatDeduction (personal allowance/12, SSO, PF, elected
    //     monthly deductions) is a flat amount that recurs every period, so
    //     it's projected the same way income is: × remainingPeriodsIncludingThis,
    //     added to ytdAccumulatedDeduction (the sum of PRIOR periods' actual
    //     flat deductions).
    //   - The expense deduction (ค่าใช้จ่าย) is NOT flat — it's a percentage
    //     of ANNUAL income capped at a fixed ceiling, so it must be computed
    //     here from projectedAnnualIncome directly, not passed in as an
    //     external per-period number (there's no way to know the correct
    //     per-period share of it without first knowing the annual total,
    //     which is exactly what this method is computing).
    public static (decimal MonthlyWithholding, TaxCalculationResult AnnualCalculation) CalculateMonthlyWithholding(
        decimal ytdAccumulatedIncome,
        decimal thisPeriodIncome,
        decimal ytdAccumulatedDeduction,
        decimal thisPeriodFlatDeduction,
        decimal expenseDeductionRate,
        decimal expenseDeductionCap,
        int remainingPeriodsIncludingThis,
        decimal ytdAccumulatedTax,
        IReadOnlyList<Pay_TaxBracket> brackets)
    {
        if (remainingPeriodsIncludingThis <= 0) remainingPeriodsIncludingThis = 1;

        var projectedAnnualIncome = ytdAccumulatedIncome + thisPeriodIncome * remainingPeriodsIncludingThis;
        var expenseDeduction = Math.Min(Math.Max(0m, projectedAnnualIncome) * expenseDeductionRate, expenseDeductionCap);
        var projectedAnnualDeduction = ytdAccumulatedDeduction + thisPeriodFlatDeduction * remainingPeriodsIncludingThis + expenseDeduction;
        var taxableIncome = Math.Max(0m, projectedAnnualIncome - projectedAnnualDeduction);

        var annualCalculation = CalculateProgressiveTax(taxableIncome, brackets);

        var remainingTax = Math.Max(0m, annualCalculation.TotalAnnualTax - ytdAccumulatedTax);
        var monthlyWithholding = Math.Round(remainingTax / remainingPeriodsIncludingThis, 2, MidpointRounding.AwayFromZero);

        return (monthlyWithholding, annualCalculation);
    }
}
