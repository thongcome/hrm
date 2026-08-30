using HRM.Models;
using HRM.Services.Pay;
using Xunit;

namespace HRM.Tests.Pay;

public class PayrollCalculationServiceTests
{
    [Fact]
    public void No_prior_employer_income_leaves_ytd_accumulators_unchanged()
    {
        var (income, deduction, tax) = PayrollCalculationService.FoldPriorEmployerIncome(50000m, 8000m, 1200m, null);

        Assert.Equal(50000m, income);
        Assert.Equal(8000m, deduction);
        Assert.Equal(1200m, tax);
    }

    [Fact]
    public void Prior_employer_income_is_added_on_top_of_this_companys_own_ytd()
    {
        var prior = new Pay_EmployeePriorEmployerIncome
        {
            IncomeAmount = 200000m,
            DeductionAmount = 30000m,
            TaxWithheldAmount = 5000m,
        };

        var (income, deduction, tax) = PayrollCalculationService.FoldPriorEmployerIncome(50000m, 8000m, 1200m, prior);

        Assert.Equal(250000m, income);
        Assert.Equal(38000m, deduction);
        Assert.Equal(6200m, tax);
    }

    [Fact]
    public void First_period_of_a_mid_year_hire_folds_prior_employer_income_even_with_zero_own_history()
    {
        var prior = new Pay_EmployeePriorEmployerIncome
        {
            IncomeAmount = 350000m,
            DeductionAmount = 0m,
            TaxWithheldAmount = 12500m,
        };

        var (income, deduction, tax) = PayrollCalculationService.FoldPriorEmployerIncome(0m, 0m, 0m, prior);

        Assert.Equal(350000m, income);
        Assert.Equal(0m, deduction);
        Assert.Equal(12500m, tax);
    }
}
