using HRM.Models;
using HRM.Services.Pay.Calculators;
using Xunit;

namespace HRM.Tests.Pay;

public class TaxBracketCalculatorTests
{
    // Same shape as the seeded 2026 Thai PIT brackets in HRMContext.OnModelCreating.
    private static List<Pay_TaxBracket> StandardBrackets() =>
    [
        new() { Id = 1, EffectiveYear = 2026, Step = 1, MinIncome = 0m, MaxIncome = 150000m, RatePercent = 0m, IsActive = true },
        new() { Id = 2, EffectiveYear = 2026, Step = 2, MinIncome = 150000m, MaxIncome = 300000m, RatePercent = 5m, IsActive = true },
        new() { Id = 3, EffectiveYear = 2026, Step = 3, MinIncome = 300000m, MaxIncome = 500000m, RatePercent = 10m, IsActive = true },
        new() { Id = 4, EffectiveYear = 2026, Step = 4, MinIncome = 500000m, MaxIncome = 750000m, RatePercent = 15m, IsActive = true },
        new() { Id = 5, EffectiveYear = 2026, Step = 5, MinIncome = 750000m, MaxIncome = 1000000m, RatePercent = 20m, IsActive = true },
        new() { Id = 6, EffectiveYear = 2026, Step = 6, MinIncome = 1000000m, MaxIncome = 2000000m, RatePercent = 25m, IsActive = true },
        new() { Id = 7, EffectiveYear = 2026, Step = 7, MinIncome = 2000000m, MaxIncome = 5000000m, RatePercent = 30m, IsActive = true },
        new() { Id = 8, EffectiveYear = 2026, Step = 8, MinIncome = 5000000m, MaxIncome = null, RatePercent = 35m, IsActive = true },
    ];

    [Fact]
    public void Income_within_exempt_bracket_pays_zero_tax()
    {
        var result = TaxBracketCalculator.CalculateProgressiveTax(100000m, StandardBrackets());
        Assert.Equal(0m, result.TotalAnnualTax);
    }

    [Fact]
    public void Income_spanning_multiple_brackets_is_taxed_cumulatively_not_at_a_single_flat_rate()
    {
        // 400,000 THB annual income spans brackets 1 (0%), 2 (5%), 3 (10%):
        //   0-150,000      @ 0%  = 0
        //   150,000-300,000 @ 5%  = 150,000 * 0.05 = 7,500
        //   300,000-400,000 @ 10% = 100,000 * 0.10 = 10,000
        // total = 17,500
        //
        // The legacy CalculateTax bug taxed only the single matched bracket flat,
        // which would have given 100,000 * 0.10 = 10,000 — wrong. This asserts
        // the correct cumulative/marginal result.
        var result = TaxBracketCalculator.CalculateProgressiveTax(400000m, StandardBrackets());

        Assert.Equal(17500m, result.TotalAnnualTax);
        // brackets 1 (0% rate), 2, and 3 are all traversed (income passes through each);
        // only bracket 1 contributes zero tax because its rate is 0%, not because it's skipped.
        Assert.Equal(3, result.Breakdown.Count);
    }

    [Fact]
    public void Breakdown_reports_each_bracket_traversed_with_correct_partial_amounts()
    {
        var result = TaxBracketCalculator.CalculateProgressiveTax(400000m, StandardBrackets());

        var bracket2 = Assert.Single(result.Breakdown, b => b.Step == 2);
        Assert.Equal(150000m, bracket2.TaxableAmountInBracket);
        Assert.Equal(7500m, bracket2.TaxInBracket);

        var bracket3 = Assert.Single(result.Breakdown, b => b.Step == 3);
        Assert.Equal(100000m, bracket3.TaxableAmountInBracket);
        Assert.Equal(10000m, bracket3.TaxInBracket);
    }

    [Fact]
    public void Income_in_top_uncapped_bracket_is_taxed_correctly()
    {
        // 6,000,000 spans all 8 brackets; verify total against manual sum.
        var result = TaxBracketCalculator.CalculateProgressiveTax(6000000m, StandardBrackets());

        var expected =
            150000m * 0.00m +   // bracket 1
            150000m * 0.05m +   // bracket 2
            200000m * 0.10m +   // bracket 3
            250000m * 0.15m +   // bracket 4
            250000m * 0.20m +   // bracket 5
            1000000m * 0.25m +  // bracket 6
            3000000m * 0.30m +  // bracket 7
            1000000m * 0.35m;   // bracket 8 (6,000,000 - 5,000,000)

        Assert.Equal(expected, result.TotalAnnualTax);
    }

    [Fact]
    public void Negative_income_is_treated_as_zero()
    {
        var result = TaxBracketCalculator.CalculateProgressiveTax(-500m, StandardBrackets());
        Assert.Equal(0m, result.TotalAnnualTax);
        Assert.Empty(result.Breakdown);
    }

    [Fact]
    public void Monthly_withholding_spreads_remaining_annual_tax_over_remaining_periods()
    {
        // Employee earning 40,000/month with no YTD history yet, mid-year (June, 7 periods remaining).
        var (monthlyTax, annual) = TaxBracketCalculator.CalculateMonthlyWithholding(
            ytdAccumulatedIncome: 0m,
            thisPeriodIncome: 40000m,
            ytdAccumulatedDeduction: 0m,
            thisPeriodFlatDeduction: 0m,
            expenseDeductionRate: 0m,
            expenseDeductionCap: 0m,
            remainingPeriodsIncludingThis: 7,
            ytdAccumulatedTax: 0m,
            brackets: StandardBrackets());

        // projected annual income = 40,000 * 7 = 280,000 -> tax = (280,000-150,000)*0.05 = 6,500
        Assert.Equal(6500m, annual.TotalAnnualTax);
        Assert.Equal(Math.Round(6500m / 7, 2), monthlyTax);
    }

    [Fact]
    public void Monthly_withholding_subtracts_tax_already_paid_ytd()
    {
        var (monthlyTax, _) = TaxBracketCalculator.CalculateMonthlyWithholding(
            ytdAccumulatedIncome: 200000m,
            thisPeriodIncome: 40000m,
            ytdAccumulatedDeduction: 0m,
            thisPeriodFlatDeduction: 0m,
            expenseDeductionRate: 0m,
            expenseDeductionCap: 0m,
            remainingPeriodsIncludingThis: 5,
            ytdAccumulatedTax: 2500m,
            brackets: StandardBrackets());

        // projected annual = 200,000 + 40,000*5 = 400,000 -> total tax = 17,500 (from earlier test)
        // remaining = 17,500 - 2,500 = 15,000; monthly = 15,000/5 = 3,000
        Assert.Equal(3000m, monthlyTax);
    }

    [Fact]
    public void Expense_deduction_is_computed_from_projected_annual_income_and_capped()
    {
        // 50,000/month, 12 remaining periods -> projected annual income 600,000.
        // Expense deduction = min(600,000 * 50%, 100,000) = 100,000 (capped,
        // 50% would be 300,000 uncapped). Personal allowance flat 60,000/year
        // via thisPeriodFlatDeduction = 5,000/month * 12 = 60,000.
        // Taxable = 600,000 - 100,000 - 60,000 = 440,000.
        var (_, annual) = TaxBracketCalculator.CalculateMonthlyWithholding(
            ytdAccumulatedIncome: 0m,
            thisPeriodIncome: 50000m,
            ytdAccumulatedDeduction: 0m,
            thisPeriodFlatDeduction: 5000m,
            expenseDeductionRate: 0.50m,
            expenseDeductionCap: 100000m,
            remainingPeriodsIncludingThis: 12,
            ytdAccumulatedTax: 0m,
            brackets: StandardBrackets());

        // 0-150,000 @0% + 150,000-300,000 @5%=7,500 + 300,000-440,000 @10%=14,000 = 21,500
        Assert.Equal(21500m, annual.TotalAnnualTax);
    }

    [Fact]
    public void Expense_deduction_below_cap_uses_the_percentage_not_the_cap()
    {
        // 10,000/month, 12 periods -> projected annual income 120,000.
        // Expense deduction = min(120,000*50%, 100,000) = 60,000 (percentage wins, cap doesn't bind).
        // No personal allowance/flat deduction in this test to isolate the expense-deduction math.
        // Taxable = 120,000 - 60,000 = 60,000, entirely within the 0% bracket.
        var (monthlyTax, annual) = TaxBracketCalculator.CalculateMonthlyWithholding(
            ytdAccumulatedIncome: 0m,
            thisPeriodIncome: 10000m,
            ytdAccumulatedDeduction: 0m,
            thisPeriodFlatDeduction: 0m,
            expenseDeductionRate: 0.50m,
            expenseDeductionCap: 100000m,
            remainingPeriodsIncludingThis: 12,
            ytdAccumulatedTax: 0m,
            brackets: StandardBrackets());

        Assert.Equal(0m, annual.TotalAnnualTax);
        Assert.Equal(0m, monthlyTax);
    }
}
