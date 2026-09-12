namespace Advance.Payroll.Core;

// Ported verbatim (logic unchanged) from HRM Services/Pay/Calculators/TaxBracketCalculator.cs,
// with Pay_TaxBracket -> TaxBracket (the trimmed POCO in Pocos.cs). See that file's original
// header comment for the legacy-bug history this fixes (PayrollProcess.razor CalculateTax).
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

    public static TaxCalculationResult CalculateProgressiveTax(decimal annualizedTaxableIncome, IReadOnlyList<TaxBracket> brackets)
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

    public static (decimal MonthlyWithholding, TaxCalculationResult AnnualCalculation) CalculateMonthlyWithholding(
        decimal ytdAccumulatedIncome,
        decimal thisPeriodIncome,
        decimal ytdAccumulatedDeduction,
        decimal thisPeriodFlatDeduction,
        decimal expenseDeductionRate,
        decimal expenseDeductionCap,
        int remainingPeriodsIncludingThis,
        decimal ytdAccumulatedTax,
        IReadOnlyList<TaxBracket> brackets,
        decimal annualFixedDeduction = 0m)
        => CalculatePeriodWithholding(ytdAccumulatedIncome, thisPeriodIncome, ytdAccumulatedDeduction, thisPeriodFlatDeduction,
            expenseDeductionRate, expenseDeductionCap, remainingPeriodsIncludingThis, remainingPeriodsIncludingThis, 1,
            ytdAccumulatedTax, brackets, annualFixedDeduction);

    public static (decimal MonthlyWithholding, TaxCalculationResult AnnualCalculation) CalculatePeriodWithholding(
        decimal ytdAccumulatedIncome,
        decimal thisPeriodIncome,
        decimal ytdAccumulatedDeduction,
        decimal thisPeriodFlatDeduction,
        decimal expenseDeductionRate,
        decimal expenseDeductionCap,
        decimal remainingMonthsIncludingThis,
        int remainingPeriodsIncludingThis,
        int periodsPerMonth,
        decimal ytdAccumulatedTax,
        IReadOnlyList<TaxBracket> brackets,
        decimal annualFixedDeduction = 0m,
        decimal thisPeriodOneOffIncome = 0m,
        decimal? projectedRemainingFlatDeduction = null)
    {
        if (periodsPerMonth <= 0) periodsPerMonth = 1;
        if (remainingPeriodsIncludingThis <= 0) remainingPeriodsIncludingThis = 1;
        if (remainingMonthsIncludingThis <= 0) remainingMonthsIncludingThis = 1m / periodsPerMonth;
        if (thisPeriodOneOffIncome < 0) thisPeriodOneOffIncome = 0m;

        var recurringThisPeriod = thisPeriodIncome - thisPeriodOneOffIncome;
        var monthlyIncome = recurringThisPeriod * periodsPerMonth;
        var monthlyFlatDeduction = thisPeriodFlatDeduction * periodsPerMonth;

        var projectedRecurringIncome = ytdAccumulatedIncome + monthlyIncome * remainingMonthsIncludingThis;
        var remainingFlat = projectedRemainingFlatDeduction ?? monthlyFlatDeduction * remainingMonthsIncludingThis;
        var projectedFlatDeduction = ytdAccumulatedDeduction + remainingFlat + annualFixedDeduction;

        TaxCalculationResult TaxOn(decimal annualIncome)
        {
            var expenseDeduction = Math.Min(Math.Max(0m, annualIncome) * expenseDeductionRate, expenseDeductionCap);
            return CalculateProgressiveTax(Math.Max(0m, annualIncome - projectedFlatDeduction - expenseDeduction), brackets);
        }

        var recurringCalculation = TaxOn(projectedRecurringIncome);
        var remainingTax = Math.Max(0m, recurringCalculation.TotalAnnualTax - ytdAccumulatedTax);
        var periodWithholding = Math.Round(remainingTax / remainingPeriodsIncludingThis, 2, MidpointRounding.AwayFromZero);
        if (thisPeriodOneOffIncome <= 0m)
            return (periodWithholding, recurringCalculation);

        var withOneOff = TaxOn(projectedRecurringIncome + thisPeriodOneOffIncome);
        var oneOffTax = Math.Round(Math.Max(0m, withOneOff.TotalAnnualTax - recurringCalculation.TotalAnnualTax), 2, MidpointRounding.AwayFromZero);
        return (periodWithholding + oneOffTax, withOneOff);
    }

    public static (decimal Withholding, TaxCalculationResult AnnualCalculation) CalculateBonusWithholding(
        decimal ytdIncomeIncludingThisPeriod,
        decimal ytdDeductionIncludingThisPeriod,
        decimal ytdTaxWithheld,
        decimal regularMonthlyIncome,
        decimal regularMonthlyFlatDeduction,
        decimal bonusAmount,
        int remainingPeriodsAfterThis,
        decimal expenseDeductionRate,
        decimal expenseDeductionCap,
        IReadOnlyList<TaxBracket> brackets,
        decimal annualFixedDeduction = 0m)
        => CalculateBonusWithholding(ytdIncomeIncludingThisPeriod, ytdDeductionIncludingThisPeriod, ytdTaxWithheld,
            regularMonthlyIncome, regularMonthlyFlatDeduction, bonusAmount, (decimal)remainingPeriodsAfterThis,
            expenseDeductionRate, expenseDeductionCap, brackets, annualFixedDeduction);

    public static (decimal Withholding, TaxCalculationResult AnnualCalculation) CalculateBonusWithholding(
        decimal ytdIncomeIncludingThisPeriod,
        decimal ytdDeductionIncludingThisPeriod,
        decimal ytdTaxWithheld,
        decimal regularMonthlyIncome,
        decimal regularMonthlyFlatDeduction,
        decimal bonusAmount,
        decimal remainingMonthsAfterThis,
        decimal expenseDeductionRate,
        decimal expenseDeductionCap,
        IReadOnlyList<TaxBracket> brackets,
        decimal annualFixedDeduction = 0m)
    {
        var remainingPeriodsAfterThis = Math.Max(0m, remainingMonthsAfterThis);

        var baseIncome = ytdIncomeIncludingThisPeriod + regularMonthlyIncome * remainingPeriodsAfterThis;
        var baseDeduction = ytdDeductionIncludingThisPeriod + regularMonthlyFlatDeduction * remainingPeriodsAfterThis + annualFixedDeduction;

        TaxCalculationResult TaxOn(decimal income)
        {
            var expense = Math.Min(Math.Max(0m, income) * expenseDeductionRate, expenseDeductionCap);
            return CalculateProgressiveTax(Math.Max(0m, income - baseDeduction - expense), brackets);
        }

        var withBonus = TaxOn(baseIncome + bonusAmount);
        var without = TaxOn(baseIncome);
        var withholding = Math.Round(Math.Max(0m, withBonus.TotalAnnualTax - without.TotalAnnualTax), 2, MidpointRounding.AwayFromZero);
        return (withholding, withBonus);
    }
}
