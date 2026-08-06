using HRM.Services.Pay.Calculators;
using Xunit;

namespace HRM.Tests.Pay;

public class ProvidentFundCalculatorTests
{
    [Fact]
    public void Splits_employee_and_company_amounts_independently_by_their_own_rates()
    {
        var result = ProvidentFundCalculator.Calculate(grossWage: 30000m, employeeRatePercent: 3m, companyRatePercent: 5m);

        Assert.Equal(900m, result.EmployeeAmount);
        Assert.Equal(1500m, result.CompanyAmount);
    }

    [Fact]
    public void Zero_rates_produce_zero_amounts()
    {
        var result = ProvidentFundCalculator.Calculate(30000m, 0m, 0m);

        Assert.Equal(0m, result.EmployeeAmount);
        Assert.Equal(0m, result.CompanyAmount);
    }

    [Fact]
    public void Negative_gross_wage_is_clamped_to_zero_not_negative_deduction()
    {
        var result = ProvidentFundCalculator.Calculate(grossWage: -5000m, employeeRatePercent: 3m, companyRatePercent: 5m);

        Assert.Equal(0m, result.EmployeeAmount);
        Assert.Equal(0m, result.CompanyAmount);
    }

    [Fact]
    public void Rounds_away_from_zero_at_the_midpoint()
    {
        // 100 * 2.505% = 2.505 -> should round to 2.51, not bankers'-round to 2.50
        var result = ProvidentFundCalculator.Calculate(grossWage: 100m, employeeRatePercent: 2.505m, companyRatePercent: 2.505m);

        Assert.Equal(2.51m, result.EmployeeAmount);
        Assert.Equal(2.51m, result.CompanyAmount);
    }
}
