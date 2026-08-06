using HRM.Services.Pay.Calculators;
using Xunit;

namespace HRM.Tests.Pay;

public class WelfareFundCalculatorTests
{
    [Fact]
    public void No_cap_uses_full_gross_wage()
    {
        var result = WelfareFundCalculator.Calculate(grossWage: 50000m, employeeRatePercent: 1m, companyRatePercent: 1m, wageCap: null);

        Assert.Equal(500m, result.EmployeeAmount);
        Assert.Equal(500m, result.CompanyAmount);
    }

    [Fact]
    public void Wage_above_cap_is_calculated_on_the_cap_not_the_full_wage()
    {
        var result = WelfareFundCalculator.Calculate(grossWage: 50000m, employeeRatePercent: 1m, companyRatePercent: 1m, wageCap: 15000m);

        Assert.Equal(150m, result.EmployeeAmount);
        Assert.Equal(150m, result.CompanyAmount);
    }

    [Fact]
    public void Wage_below_cap_is_calculated_on_the_actual_wage()
    {
        var result = WelfareFundCalculator.Calculate(grossWage: 10000m, employeeRatePercent: 1m, companyRatePercent: 1m, wageCap: 15000m);

        Assert.Equal(100m, result.EmployeeAmount);
        Assert.Equal(100m, result.CompanyAmount);
    }

    [Fact]
    public void Zero_or_negative_cap_is_treated_as_no_cap()
    {
        var result = WelfareFundCalculator.Calculate(grossWage: 50000m, employeeRatePercent: 1m, companyRatePercent: 1m, wageCap: 0m);

        Assert.Equal(500m, result.EmployeeAmount);
    }

    [Fact]
    public void Negative_gross_wage_is_clamped_to_zero()
    {
        var result = WelfareFundCalculator.Calculate(grossWage: -1000m, employeeRatePercent: 1m, companyRatePercent: 1m, wageCap: 15000m);

        Assert.Equal(0m, result.EmployeeAmount);
        Assert.Equal(0m, result.CompanyAmount);
    }
}
