using HRM.Models;
using HRM.Services.Pay;
using Xunit;

namespace HRM.Tests.Pay;

// ยอดยกมา: an employee can hold a prior-employer row AND an opening-balance row in the same tax year.
// Both must reach the year-to-date figures — picking only one silently under-withholds tax.
public class OpeningBalanceTests
{
    private static Pay_EmployeePriorEmployerIncome Row(decimal income, decimal deduction, decimal tax, bool same) => new()
    {
        HremployeeId = 1, TaxYear = 2026, IncomeAmount = income, DeductionAmount = deduction, TaxWithheldAmount = tax,
        IsSameEmployer = same, PriorEmployerName = same ? "opening" : "old employer",
    };

    [Fact]
    public void No_rows_means_nothing_to_fold()
    {
        Assert.Null(PayrollCalculationService.CombinePriorIncome(Array.Empty<Pay_EmployeePriorEmployerIncome>()));
    }

    [Fact]
    public void A_single_row_is_used_as_is()
    {
        var row = Row(100_000m, 0m, 2_000m, same: false);
        Assert.Same(row, PayrollCalculationService.CombinePriorIncome(new[] { row }));
    }

    [Fact]
    public void Two_rows_are_summed_so_neither_is_dropped()
    {
        var combined = PayrollCalculationService.CombinePriorIncome(new[]
        {
            Row(300_000m, 10_000m, 5_000m, same: false),
            Row(200_000m, 8_000m, 3_000m, same: true),
        })!;
        Assert.Equal(500_000m, combined.IncomeAmount);
        Assert.Equal(18_000m, combined.DeductionAmount);
        Assert.Equal(8_000m, combined.TaxWithheldAmount);

        var (income, deduction, tax) = PayrollCalculationService.FoldPriorEmployerIncome(50_000m, 1_000m, 500m, combined);
        Assert.Equal(550_000m, income);
        Assert.Equal(19_000m, deduction);
        Assert.Equal(8_500m, tax);
    }
}
