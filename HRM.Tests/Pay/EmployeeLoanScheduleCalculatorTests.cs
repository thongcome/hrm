using HRM.Services.Pay.Calculators;
using Xunit;

namespace HRM.Tests.Pay;

public class EmployeeLoanScheduleCalculatorTests
{
    [Fact]
    public void Installments_sum_exactly_to_principal_even_when_it_does_not_divide_evenly()
    {
        var lines = EmployeeLoanScheduleCalculator.Calculate(principal: 1000m, totalInstallments: 3, startPeriod: "202601");

        Assert.Equal(3, lines.Count);
        Assert.Equal(1000m, lines.Sum(l => l.Amount));
    }

    [Fact]
    public void Rounding_remainder_is_folded_into_the_last_installment_not_the_first()
    {
        var lines = EmployeeLoanScheduleCalculator.Calculate(principal: 1000m, totalInstallments: 3, startPeriod: "202601");

        Assert.Equal(333.33m, lines[0].Amount);
        Assert.Equal(333.33m, lines[1].Amount);
        Assert.Equal(333.34m, lines[2].Amount); // absorbs the leftover cent
    }

    [Fact]
    public void Balance_after_the_final_installment_is_exactly_zero()
    {
        var lines = EmployeeLoanScheduleCalculator.Calculate(principal: 1000m, totalInstallments: 3, startPeriod: "202601");

        Assert.Equal(0m, lines[^1].BalanceAfter);
    }

    [Fact]
    public void Evenly_divisible_principal_splits_equally_across_all_installments()
    {
        var lines = EmployeeLoanScheduleCalculator.Calculate(principal: 900m, totalInstallments: 3, startPeriod: "202601");

        Assert.All(lines, l => Assert.Equal(300m, l.Amount));
    }

    [Fact]
    public void Period_rolls_over_the_year_boundary_correctly()
    {
        var lines = EmployeeLoanScheduleCalculator.Calculate(principal: 300m, totalInstallments: 3, startPeriod: "202512");

        Assert.Equal(["202512", "202601", "202602"], lines.Select(l => l.Period).ToArray());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Non_positive_principal_throws(decimal principal)
    {
        Assert.Throws<ArgumentException>(() => EmployeeLoanScheduleCalculator.Calculate(principal, 3, "202601"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Non_positive_installment_count_throws(int totalInstallments)
    {
        Assert.Throws<ArgumentException>(() => EmployeeLoanScheduleCalculator.Calculate(1000m, totalInstallments, "202601"));
    }

    [Theory]
    [InlineData("2026")]     // too short
    [InlineData("20261")]    // too short
    [InlineData("202613")]   // invalid month
    [InlineData("202600")]   // invalid month
    [InlineData("abcdef")]   // not numeric
    public void Malformed_start_period_throws(string startPeriod)
    {
        Assert.Throws<ArgumentException>(() => EmployeeLoanScheduleCalculator.Calculate(1000m, 3, startPeriod));
    }
}
