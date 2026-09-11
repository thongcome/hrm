using HRM.Models;
using HRM.Services.Pay;
using HRM.Services.Pay.Calculators;
using Xunit;

namespace HRM.Tests.Pay;

// 11 ก.ย. 2569 — audit C1/C2: the run TYPE, not only the status, decides what
// may happen to a run. A reversal is the negation of a posted run and must
// never be recalculated (it would turn positive and pay the month twice);
// a bonus run withholds tax on the bonus alone, not "bonus × remaining months".
public class PayrollRunTypeGuardTests
{
    [Fact]
    public void Reversal_run_cannot_be_recalculated_reversed_or_adjusted()
    {
        var reversal = new Pay_PayrollRun { RunType = PayrollRunType.Reversal, Status = PayrollRunStatus.Calculated };
        var allowed = PayrollWorkflowService.GetAllowedActions(reversal);

        Assert.DoesNotContain(PayrollAction.Calculate, allowed);
        Assert.Contains(PayrollAction.SubmitForReview, allowed);
        Assert.Contains(PayrollAction.Cancel, allowed);

        var postedReversal = new Pay_PayrollRun { RunType = PayrollRunType.Reversal, Status = PayrollRunStatus.Posted };
        var allowedPosted = PayrollWorkflowService.GetAllowedActions(postedReversal);
        Assert.DoesNotContain(PayrollAction.Reverse, allowedPosted);
        Assert.DoesNotContain(PayrollAction.CreateAdjustment, allowedPosted);
        Assert.Contains(PayrollAction.MarkPaid, allowedPosted);
    }

    [Fact]
    public void Regular_run_keeps_the_status_only_action_set()
    {
        var regular = new Pay_PayrollRun { RunType = PayrollRunType.Regular, Status = PayrollRunStatus.Calculated };
        Assert.Equal(
            PayrollWorkflowService.GetAllowedActions(PayrollRunStatus.Calculated),
            PayrollWorkflowService.GetAllowedActions(regular));
    }

    private static List<Pay_TaxBracket> ThaiBrackets2026() => new()
    {
        new() { EffectiveYear = 2026, MinIncome = 0m, MaxIncome = 150000m, RatePercent = 0m, IsActive = true },
        new() { EffectiveYear = 2026, MinIncome = 150000m, MaxIncome = 300000m, RatePercent = 5m, IsActive = true },
        new() { EffectiveYear = 2026, MinIncome = 300000m, MaxIncome = 500000m, RatePercent = 10m, IsActive = true },
        new() { EffectiveYear = 2026, MinIncome = 500000m, MaxIncome = 750000m, RatePercent = 15m, IsActive = true },
        new() { EffectiveYear = 2026, MinIncome = 750000m, MaxIncome = 1000000m, RatePercent = 20m, IsActive = true },
        new() { EffectiveYear = 2026, MinIncome = 1000000m, MaxIncome = 2000000m, RatePercent = 25m, IsActive = true },
        new() { EffectiveYear = 2026, MinIncome = 2000000m, MaxIncome = 5000000m, RatePercent = 30m, IsActive = true },
        new() { EffectiveYear = 2026, MinIncome = 5000000m, MaxIncome = null, RatePercent = 35m, IsActive = true },
    };

    [Fact]
    public void Bonus_withholding_is_the_difference_between_annual_tax_with_and_without_the_bonus()
    {
        var brackets = ThaiBrackets2026();
        // June: 6 months of 50,000 already approved (incl. this month), 6 months left,
        // flat deductions 5,000/month (personal allowance), bonus 100,000.
        var (withholding, _) = TaxBracketCalculator.CalculateBonusWithholding(
            ytdIncomeIncludingThisPeriod: 300000m, ytdDeductionIncludingThisPeriod: 30000m, ytdTaxWithheld: 0m,
            regularMonthlyIncome: 50000m, regularMonthlyFlatDeduction: 5000m,
            bonusAmount: 100000m, remainingPeriodsAfterThis: 6,
            expenseDeductionRate: 0.5m, expenseDeductionCap: 100000m, brackets: brackets);

        // without bonus: income 600,000 − deductions 60,000 − expense 100,000 = 440,000 taxable → 7,500 + 14,000 = 21,500
        // with bonus:    income 700,000 − 60,000 − 100,000 = 540,000 taxable → 7,500 + 20,000 + 6,000 = 33,500
        Assert.Equal(12000m, withholding);
    }

    [Fact]
    public void Bonus_withholding_is_never_negative_and_zero_when_bonus_is_zero()
    {
        var brackets = ThaiBrackets2026();
        var (withholding, _) = TaxBracketCalculator.CalculateBonusWithholding(
            300000m, 30000m, 0m, 50000m, 5000m, 0m, 6, 0.5m, 100000m, brackets);
        Assert.Equal(0m, withholding);
    }
}
