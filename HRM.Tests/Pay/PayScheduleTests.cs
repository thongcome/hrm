using HRM.Models;
using HRM.Services.Pay;
using HRM.Services.Pay.Calculators;
using Xunit;

namespace HRM.Tests.Pay;

// รอบจ่ายเป็น config (audit M8): ตัวคำนวณต้องอ่านปฏิทินจ่ายจริงของพนักงานแต่ละคน ไม่ใช่ "13 − เดือน"
public class PayScheduleTests
{
    private static List<Pay_TaxBracket> Brackets() =>
    [
        new() { Id = 1, EffectiveYear = 2026, Step = 1, MinIncome = 0m, MaxIncome = 150000m, RatePercent = 0m, IsActive = true },
        new() { Id = 2, EffectiveYear = 2026, Step = 2, MinIncome = 150000m, MaxIncome = 300000m, RatePercent = 5m, IsActive = true },
        new() { Id = 3, EffectiveYear = 2026, Step = 3, MinIncome = 300000m, MaxIncome = 500000m, RatePercent = 10m, IsActive = true },
        new() { Id = 4, EffectiveYear = 2026, Step = 4, MinIncome = 500000m, MaxIncome = 750000m, RatePercent = 15m, IsActive = true },
        new() { Id = 5, EffectiveYear = 2026, Step = 5, MinIncome = 750000m, MaxIncome = null, RatePercent = 20m, IsActive = true },
    ];

    private static Pay_PaySchedule Schedule(long id, int ppm, PayScheduleGroup group, DateOnly from, DateOnly? to = null)
        => new() { Id = id, CompanyId = "ADVD", Code = $"S{id}", Name = $"S{id}", PeriodsPerMonth = ppm, AppliesTo = group, EffectiveFrom = from, EffectiveTo = to, IsActive = true };

    [Fact]
    public void No_config_means_monthly_like_before()
    {
        var ctx = PayScheduleResolver.Resolve(1, PayScheduleGroup.MonthlySalaried, new DateOnly(2026, 9, 1), [], []);
        Assert.Equal(1, ctx.PeriodsPerMonth);
        Assert.Equal(4m, ctx.RemainingMonthsIncludingThis);
        Assert.Equal(4, ctx.RemainingPeriodsIncludingThis);
    }

    [Fact]
    public void Semi_monthly_counts_half_months_and_real_periods()
    {
        var schedules = new[] { Schedule(1, 2, PayScheduleGroup.DailyWage, new DateOnly(2026, 1, 1)) };

        var term1 = PayScheduleResolver.Resolve(1, PayScheduleGroup.DailyWage, new DateOnly(2026, 9, 1), schedules, []);
        Assert.Equal(1, term1.TermNo);
        Assert.Equal(0.5m, term1.MonthFraction);
        Assert.Equal(4m, term1.RemainingMonthsIncludingThis);     // ก.ย. ทั้งเดือน + ต.ค. พ.ย. ธ.ค.
        Assert.Equal(8, term1.RemainingPeriodsIncludingThis);     // 2 งวด × 4 เดือน

        var term2 = PayScheduleResolver.Resolve(1, PayScheduleGroup.DailyWage, new DateOnly(2026, 9, 16), schedules, []);
        Assert.Equal(2, term2.TermNo);
        Assert.Equal(3.5m, term2.RemainingMonthsIncludingThis);
        Assert.Equal(7, term2.RemainingPeriodsIncludingThis);
    }

    [Fact]
    public void Group_default_applies_only_to_that_group()
    {
        var schedules = new[] { Schedule(1, 2, PayScheduleGroup.DailyWage, new DateOnly(2026, 1, 1)) };
        var salaried = PayScheduleResolver.Resolve(1, PayScheduleGroup.MonthlySalaried, new DateOnly(2026, 9, 1), schedules, []);
        Assert.Equal(1, salaried.PeriodsPerMonth);
        Assert.Equal(4, salaried.RemainingPeriodsIncludingThis);
    }

    [Fact]
    public void Mid_year_change_is_read_from_the_calendar_of_each_remaining_month()
    {
        // รายเดือนถึง 30 ก.ย. แล้วเปลี่ยนเป็นครึ่งเดือนตั้งแต่ 1 ต.ค.
        var schedules = new[]
        {
            Schedule(1, 1, PayScheduleGroup.All, new DateOnly(2026, 1, 1), new DateOnly(2026, 9, 30)),
            Schedule(2, 2, PayScheduleGroup.All, new DateOnly(2026, 10, 1)),
        };
        var sep = PayScheduleResolver.Resolve(1, PayScheduleGroup.MonthlySalaried, new DateOnly(2026, 9, 1), schedules, []);
        Assert.Equal(1, sep.PeriodsPerMonth);
        Assert.Equal(4m, sep.RemainingMonthsIncludingThis);
        Assert.Equal(1 + 3 * 2, sep.RemainingPeriodsIncludingThis);

        var oct = PayScheduleResolver.Resolve(1, PayScheduleGroup.MonthlySalaried, new DateOnly(2026, 10, 1), schedules, []);
        Assert.Equal(2, oct.PeriodsPerMonth);
        Assert.Equal(3m, oct.RemainingMonthsIncludingThis);
        Assert.Equal(6, oct.RemainingPeriodsIncludingThis);
    }

    [Fact]
    public void Per_employee_override_wins_over_the_group_default()
    {
        var schedules = new[]
        {
            Schedule(1, 1, PayScheduleGroup.All, new DateOnly(2026, 1, 1)),
            Schedule(2, 2, PayScheduleGroup.DailyWage, new DateOnly(2026, 1, 1)),
        };
        var overrides = new[] { new Pay_EmployeePayScheduleOverride { HremployeeId = 7, PayScheduleId = 2, EffectiveFrom = new DateOnly(2026, 6, 1), IsActive = true } };

        var other = PayScheduleResolver.Resolve(8, PayScheduleGroup.MonthlySalaried, new DateOnly(2026, 9, 1), schedules, overrides);
        Assert.Equal(1, other.PeriodsPerMonth);

        var overridden = PayScheduleResolver.Resolve(7, PayScheduleGroup.MonthlySalaried, new DateOnly(2026, 9, 1), schedules, overrides);
        Assert.Equal(2, overridden.PeriodsPerMonth);
        Assert.Equal(8, overridden.RemainingPeriodsIncludingThis);
    }

    [Fact]
    public void Two_half_month_periods_withhold_the_same_as_one_monthly_period()
    {
        // เงินเดือน 50,000: รายเดือนงวด ม.ค. เทียบกับครึ่งเดือน 25,000 × 2 งวด — ภาษีทั้งปีที่ประมาณการต้องเท่ากัน
        var (monthly, annualMonthly) = TaxBracketCalculator.CalculatePeriodWithholding(
            0m, 50000m, 0m, 750m, 0.5m, 100000m,
            remainingMonthsIncludingThis: 12m, remainingPeriodsIncludingThis: 12, periodsPerMonth: 1,
            0m, Brackets(), annualFixedDeduction: 60000m);

        var (half1, annualHalf) = TaxBracketCalculator.CalculatePeriodWithholding(
            0m, 25000m, 0m, 375m, 0.5m, 100000m,
            remainingMonthsIncludingThis: 12m, remainingPeriodsIncludingThis: 24, periodsPerMonth: 2,
            0m, Brackets(), annualFixedDeduction: 60000m);

        Assert.Equal(annualMonthly.TotalAnnualTax, annualHalf.TotalAnnualTax);
        Assert.InRange(half1, monthly / 2m - 0.01m, monthly / 2m + 0.01m);   // ปัดเศษคนละจุด ต่างกันได้ 1 สตางค์
    }

    [Fact]
    public void Monthly_overload_is_unchanged()
    {
        var viaOld = TaxBracketCalculator.CalculateMonthlyWithholding(0m, 50000m, 0m, 750m, 0.5m, 100000m, 12, 0m, Brackets(), 60000m);
        var viaNew = TaxBracketCalculator.CalculatePeriodWithholding(0m, 50000m, 0m, 750m, 0.5m, 100000m, 12m, 12, 1, 0m, Brackets(), 60000m);
        Assert.Equal(viaOld.MonthlyWithholding, viaNew.MonthlyWithholding);
    }
}
