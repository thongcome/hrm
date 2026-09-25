namespace HRM.Services.Pay.Calculators;

// พ.ร.บ.คุ้มครองแรงงาน ม.76: deductions other than tax/statutory contributions — union dues,
// cooperative/savings debts and welfare, security deposit/damages, provident fund — may each take
// at most 10% of the pay the employee is entitled to that round, and at most one fifth together,
// unless the employee has consented in writing. Consent is normal for loans, so payroll never
// blocks on this; it only warns so HR checks the consent is on file before approving.
public static class Section76Check
{
    public const decimal EachCategoryLimit = 0.10m;
    public const decimal TotalLimit = 0.20m;

    public sealed record Finding(decimal Total, decimal Limit, IReadOnlyList<string> CategoriesOverTenPercent);

    public static Finding? Evaluate(decimal entitledPay, IReadOnlyDictionary<string, decimal> deductionsByCategory)
    {
        if (entitledPay <= 0m) return null;
        var total = deductionsByCategory.Values.Sum();
        var over = deductionsByCategory
            .Where(kv => kv.Value > entitledPay * EachCategoryLimit)
            .Select(kv => kv.Key)
            .OrderBy(k => k)
            .ToList();
        var totalLimit = Math.Round(entitledPay * TotalLimit, 2, MidpointRounding.AwayFromZero);
        return total > totalLimit || over.Count > 0 ? new Finding(total, totalLimit, over) : null;
    }
}
