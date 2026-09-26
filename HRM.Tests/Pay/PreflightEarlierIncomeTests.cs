using HRM.Services.Pay;
using Xunit;

namespace HRM.Tests.Pay;

// Pre-flight warning for a company that starts payroll mid-year without carried-forward income:
// the engine can only project tax from what it knows, so a missing Jan–Sep means under-withholding.
public class PreflightEarlierIncomeTests
{
    private static readonly DateOnly October = new(2026, 10, 1);

    [Fact]
    public void Employed_since_last_year_with_nothing_known_before_October_is_flagged_from_January()
        => Assert.Equal(new DateOnly(2026, 1, 1), PayrollPreflightService.EarlierIncomeMissingSince(new DateTime(2022, 1, 1), October, 2026, hasEarlierIncome: false));

    [Fact]
    public void Hired_mid_year_is_flagged_from_the_hire_date()
        => Assert.Equal(new DateOnly(2026, 6, 1), PayrollPreflightService.EarlierIncomeMissingSince(new DateTime(2026, 6, 1), October, 2026, hasEarlierIncome: false));

    [Fact]
    public void Known_earlier_income_from_any_source_is_not_flagged()
        => Assert.Null(PayrollPreflightService.EarlierIncomeMissingSince(new DateTime(2022, 1, 1), October, 2026, hasEarlierIncome: true));

    [Fact]
    public void Hired_this_period_or_less_than_a_month_before_is_not_flagged()
    {
        Assert.Null(PayrollPreflightService.EarlierIncomeMissingSince(new DateTime(2026, 10, 16), October, 2026, false));
        Assert.Null(PayrollPreflightService.EarlierIncomeMissingSince(new DateTime(2026, 9, 15), October, 2026, false));
    }

    [Fact]
    public void The_January_run_is_never_flagged()
        => Assert.Null(PayrollPreflightService.EarlierIncomeMissingSince(new DateTime(2020, 1, 1), new DateOnly(2026, 1, 1), 2026, false));

    [Fact]
    public void A_December_period_paid_in_January_belongs_to_the_new_tax_year_and_is_not_flagged()
        => Assert.Null(PayrollPreflightService.EarlierIncomeMissingSince(new DateTime(2020, 1, 1), new DateOnly(2025, 12, 1), 2026, false));

    [Fact]
    public void Unknown_hire_date_is_left_to_the_NO_HIRE_DATE_check()
        => Assert.Null(PayrollPreflightService.EarlierIncomeMissingSince(null, October, 2026, false));
}
