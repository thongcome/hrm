using HRM.Models;

namespace HRM.Services.Pay.Calculators;

// How much of each elected personal-income-tax deduction (ม.47 ประมวลรัษฎากร) the monthly
// withholding may use, per the method configured for that year (Pay_TaxDeductionType):
//   FixedCap        — amount paid, up to MaxAmountPerYear
//   PerPerson       — AmountPerPerson × persons declared (persons ≤ MaxPersons)
//   PercentOfIncome — amount paid, up to PercentCap % of the base and MaxAmountPerYear
// then shared group caps (RETIREMENT: RMF+SSF+pension insurance, with PVD taking what is left of
// the same 500,000; LIFE_HEALTH: life + health insurance 100,000). Items whose base is net income
// (donation: 10% of income after expenses and every other deduction) are worked out last, in a
// second call, because their base depends on everything else. MaxAmountPerYear 0 = no baht cap.
public static class TaxDeductionRules
{
    public sealed record Election(int SortOrder, string Code, string Name, TaxDeductionCalcMethod Method, decimal AnnualAmount, int? PersonCount,
        decimal MaxAmountPerYear, decimal? AmountPerPerson, int? MaxPersons, decimal? PercentCap, TaxDeductionPercentBase? PercentBase,
        string? CapGroup, decimal? GroupCapPerYear);

    public sealed record Item(string Code, decimal Amount, string Note);

    public const string RetirementGroup = "RETIREMENT";

    public static bool IsNetBased(Election e)
        => e.Method == TaxDeductionCalcMethod.PercentOfIncome && e.PercentBase == TaxDeductionPercentBase.NetIncomeAfterDeductions;

    /// <summary>Everything except net-income-based items. RetirementUsed feeds the PVD room.</summary>
    public static (List<Item> Items, decimal RetirementUsed) ResolveIncomeBased(IEnumerable<Election> elections, decimal projectedAssessableIncome)
    {
        var raw = elections.Where(e => !IsNetBased(e)).OrderBy(e => e.SortOrder).ThenBy(e => e.Code)
            .Select(e => (e, amount: Raw(e, projectedAssessableIncome))).ToList();

        var usedByGroup = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var items = new List<Item>();
        foreach (var (e, amount) in raw)
        {
            var allowed = amount;
            var note = Describe(e, amount);
            if (!string.IsNullOrWhiteSpace(e.CapGroup) && e.GroupCapPerYear is decimal cap && cap > 0)
            {
                var used = usedByGroup.GetValueOrDefault(e.CapGroup!);
                allowed = Math.Max(0m, Math.Min(amount, cap - used));
                usedByGroup[e.CapGroup!] = used + allowed;
                if (allowed < amount) note += $" · เพดานรวมกลุ่ม {cap:N0} เหลือ {allowed:N2}";
            }
            items.Add(new Item(e.Code, allowed, note));
        }
        return (items, usedByGroup.GetValueOrDefault(RetirementGroup));
    }

    /// <summary>Net-income-based items (donation), from income left after every other deduction.</summary>
    public static List<Item> ResolveNetBased(IEnumerable<Election> elections, decimal netIncomeAfterOtherDeductions)
    {
        var baseAmount = Math.Max(0m, netIncomeAfterOtherDeductions);
        return elections.Where(IsNetBased).OrderBy(e => e.SortOrder).ThenBy(e => e.Code)
            .Select(e =>
            {
                var amount = CapPercent(e, e.AnnualAmount, baseAmount);
                return new Item(e.Code, amount, $"{e.Name} {e.AnnualAmount:N2} ไม่เกิน {e.PercentCap:0.##}% ของเงินได้สุทธิ {baseAmount:N2} = {amount:N2}");
            }).ToList();
    }

    private static decimal Raw(Election e, decimal projectedIncome) => e.Method switch
    {
        TaxDeductionCalcMethod.PerPerson => BahtCap(e,
            Math.Min(Math.Max(0, e.PersonCount ?? 0), e.MaxPersons ?? int.MaxValue) * (e.AmountPerPerson ?? 0m)),
        TaxDeductionCalcMethod.PercentOfIncome => CapPercent(e, e.AnnualAmount, projectedIncome),
        _ => BahtCap(e, Math.Max(0m, e.AnnualAmount)),
    };

    private static decimal CapPercent(Election e, decimal paid, decimal baseAmount)
    {
        var byPercent = e.PercentCap is decimal pct && pct > 0 ? Math.Round(baseAmount * pct / 100m, 2, MidpointRounding.AwayFromZero) : decimal.MaxValue;
        return BahtCap(e, Math.Max(0m, Math.Min(paid, byPercent)));
    }

    private static decimal BahtCap(Election e, decimal amount)
        => e.MaxAmountPerYear > 0 ? Math.Min(amount, e.MaxAmountPerYear) : amount;

    private static string Describe(Election e, decimal amount) => e.Method switch
    {
        TaxDeductionCalcMethod.PerPerson => $"{e.Name} {e.PersonCount ?? 0} คน{(e.MaxPersons is int m && (e.PersonCount ?? 0) > m ? $" (นับได้ไม่เกิน {m})" : "")} × {e.AmountPerPerson:N0} = {amount:N2}",
        TaxDeductionCalcMethod.PercentOfIncome => $"{e.Name} {e.AnnualAmount:N2} ไม่เกิน {e.PercentCap:0.##}% ของเงินได้ทั้งปี{(e.MaxAmountPerYear > 0 ? $" และ {e.MaxAmountPerYear:N0}" : "")} = {amount:N2}",
        _ => $"{e.Name} {e.AnnualAmount:N2}{(e.MaxAmountPerYear > 0 ? $" (เพดาน {e.MaxAmountPerYear:N0})" : "")} = {amount:N2}",
    };
}
