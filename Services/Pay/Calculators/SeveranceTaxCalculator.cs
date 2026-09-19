namespace HRM.Services.Pay.Calculators;

// Pure, no DB access. Splits a severance payout into the part exempt from income tax and the
// part that enters taxable income (after the expense deductions the law allows on it).
// The rule values come from Pay_SeveranceTaxRule (effective-dated) — nothing is hardcoded here.
public static class SeveranceTaxCalculator
{
    public record RuleValues(int ExemptDays, decimal ExemptCap, decimal ExpensePerYear,
        decimal RemainderExpenseRate, decimal? RemainderExpenseCap);

    // Exempt: not income at all. Excess: paid amount above the exemption (this is the income reported
    // on the withholding certificate). ExpenseDeduction: the two deductions taken off the excess.
    // TaxableBasis: what actually enters the tax calculation (Excess − ExpenseDeduction).
    public record Result(decimal Exempt, decimal Excess, decimal ExpenseDeduction, decimal TaxableBasis, int YearsCounted);

    public static Result Calculate(decimal amount, decimal dailyWage, DateOnly hireDate, DateOnly lastWorkDate, RuleValues rule)
    {
        if (amount < 0) throw new ArgumentException("amount must not be negative");
        if (dailyWage < 0) throw new ArgumentException("dailyWage must not be negative");

        var exemptLimit = Math.Min(rule.ExemptCap, rule.ExemptDays * dailyWage);
        var exempt = Math.Round(Math.Min(amount, Math.Max(0m, exemptLimit)), 2, MidpointRounding.AwayFromZero);
        var excess = amount - exempt;

        var years = YearsCounted(hireDate, lastWorkDate);
        var part1 = Math.Min(excess, rule.ExpensePerYear * years);
        var remainder = excess - part1;
        var part2 = remainder * rule.RemainderExpenseRate;
        if (rule.RemainderExpenseCap is decimal cap) part2 = Math.Min(part2, cap);

        var expense = Math.Round(part1 + part2, 2, MidpointRounding.AwayFromZero);
        var basis = Math.Max(0m, Math.Round(excess - expense, 2, MidpointRounding.AwayFromZero));
        return new Result(exempt, Math.Round(excess, 2, MidpointRounding.AwayFromZero), expense, basis, years);
    }

    // Whole years of service, a leftover fraction counts as one more year, never less than 1.
    public static int YearsCounted(DateOnly hireDate, DateOnly lastWorkDate)
    {
        if (lastWorkDate < hireDate) return 1;
        var endExclusive = lastWorkDate.AddDays(1);
        var years = 0;
        while (hireDate.AddYears(years + 1) <= endExclusive) years++;
        if (endExclusive > hireDate.AddYears(years)) years++;
        return Math.Max(1, years);
    }
}
