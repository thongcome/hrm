using HRM.Models;
using HRM.Services.Pay;
using Xunit;

namespace HRM.Tests.Pay;

// audit M14: ยอดสะสมทั้งปีพับจากแถวที่โหลดมาครั้งเดียว ต้องให้ผลเหมือน query รายคนเดิม
public class YtdFoldTests
{
    private static PayrollCalculationService.YtdRow Row(int month, decimal income, decimal flat, decimal tax, decimal pf)
        => new(new DateOnly(2026, month, 1), income, flat, tax, pf);

    [Fact]
    public void Regular_run_counts_only_periods_before_this_one()
    {
        var rows = new[] { Row(1, 50000m, 750m, 1000m, 500m), Row(2, 50000m, 750m, 1000m, 500m), Row(3, 50000m, 750m, 1000m, 500m) };
        var (income, ded, tax, pf) = PayrollCalculationService.FoldYtd(rows, new DateOnly(2026, 3, 1), includeSamePeriod: false, null);
        Assert.Equal(100000m, income);
        Assert.Equal(1500m, ded);
        Assert.Equal(2000m, tax);
        Assert.Equal(1000m, pf);
    }

    [Fact]
    public void Supplementary_run_includes_the_regular_run_of_the_same_period()
    {
        var rows = new[] { Row(1, 50000m, 750m, 1000m, 500m), Row(2, 50000m, 750m, 1000m, 500m) };
        var (income, _, _, _) = PayrollCalculationService.FoldYtd(rows, new DateOnly(2026, 2, 1), includeSamePeriod: true, null);
        Assert.Equal(100000m, income);
    }

    [Fact]
    public void Prior_employer_income_is_folded_in_and_no_rows_means_zero()
    {
        var prior = new Pay_EmployeePriorEmployerIncome { IncomeAmount = 200000m, DeductionAmount = 3000m, TaxWithheldAmount = 5000m };
        var (income, ded, tax, pf) = PayrollCalculationService.FoldYtd(null, new DateOnly(2026, 6, 1), false, prior);
        Assert.Equal(200000m, income);
        Assert.Equal(3000m, ded);
        Assert.Equal(5000m, tax);
        Assert.Equal(0m, pf);
    }

    // audit M-01: cash basis — what counts as "already paid" follows the pay date, not the period
    private static PayrollCalculationService.YtdRow Paid(int periodMonth, DateOnly payDate, decimal income)
        => new(new DateOnly(2026, periodMonth, 1), income, 0m, 0m, 0m, payDate);

    [Fact]
    public void Runs_are_ordered_by_pay_date()
    {
        // Jan period paid 25 Jan, Feb period paid 25 Feb; the Mar run is paid 25 Mar
        var rows = new[] { Paid(1, new(2026, 1, 25), 40000m), Paid(2, new(2026, 2, 25), 40000m) };
        var (income, _, _, _) = PayrollCalculationService.FoldYtd(rows, new DateOnly(2026, 3, 1), false, null, payDate: new DateOnly(2026, 3, 25));
        Assert.Equal(80000m, income);
    }

    [Fact]
    public void A_run_paid_later_than_this_one_is_not_yet_income_even_if_its_period_is_earlier()
    {
        // a late off-cycle run for the Feb period paid 2 Apr must not count toward the Mar run paid 25 Mar
        var rows = new[] { Paid(2, new(2026, 4, 2), 10000m), Paid(2, new(2026, 2, 25), 40000m) };
        var (income, _, _, _) = PayrollCalculationService.FoldYtd(rows, new DateOnly(2026, 3, 1), false, null, payDate: new DateOnly(2026, 3, 25));
        Assert.Equal(40000m, income);
    }

    [Fact]
    public void Same_pay_date_falls_back_to_the_period_rule()
    {
        var sameDay = new DateOnly(2026, 2, 25);
        var rows = new[] { Paid(2, sameDay, 40000m) };
        Assert.Equal(0m, PayrollCalculationService.FoldYtd(rows, new DateOnly(2026, 2, 1), includeSamePeriod: false, null, sameDay).YtdIncome);
        Assert.Equal(40000m, PayrollCalculationService.FoldYtd(rows, new DateOnly(2026, 2, 1), includeSamePeriod: true, null, sameDay).YtdIncome);
    }
}
