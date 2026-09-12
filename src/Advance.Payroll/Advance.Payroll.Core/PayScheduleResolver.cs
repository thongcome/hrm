namespace Advance.Payroll.Core;

// Ported from HRM Services/Pay/PayScheduleResolver.cs, with one deliberate adaptation:
// GroupOf originally took a full `Hremployee` and read two of its ~108 columns
// (DailyWage, SalaryAmt). To keep this project at zero dependency on the employee
// entity (Advance.Payroll doesn't own Hremployee — see pay_employee sync design in
// EXTRACTION-PLAN.md), it now takes those two values directly. Advance.Payroll.Engine's
// caller passes pay_employee.DailyWage / pay_employee.SalaryAmt.
public sealed record PayScheduleContext(
    string ScheduleCode,
    int PeriodsPerMonth,
    int TermNo,
    decimal MonthFraction,
    decimal RemainingMonthsIncludingThis,
    int RemainingPeriodsIncludingThis)
{
    public decimal RemainingMonthsAfterThis => Math.Max(0m, RemainingMonthsIncludingThis - MonthFraction);

    public static PayScheduleContext Monthly(int month) => new("MONTHLY", 1, 1, 1m, 13 - month, 13 - month);
}

public static class PayScheduleResolver
{
    // เดิม: GroupOf(Hremployee emp) — ดู header comment ด้านบน
    public static PayScheduleGroup GroupOf(decimal? dailyWage, decimal? salaryAmt)
        => (dailyWage ?? 0m) > 0m && (salaryAmt ?? 0m) <= 0m
            ? PayScheduleGroup.DailyWage
            : PayScheduleGroup.MonthlySalaried;

    // pure — ทดสอบได้โดยไม่ต้องมีฐานข้อมูล ผู้เรียก preload ตารางทั้งสองมาครั้งเดียวต่อรอบ
    public static PayScheduleContext Resolve(
        long hremployeeId, PayScheduleGroup group, DateOnly periodStart,
        IReadOnlyList<PaySchedule> schedules, IReadOnlyList<EmployeePayScheduleOverride> overrides)
    {
        PaySchedule? At(DateOnly d)
        {
            var ov = overrides
                .Where(o => o.HremployeeId == hremployeeId && o.IsActive && o.EffectiveFrom <= d && (o.EffectiveTo == null || o.EffectiveTo >= d))
                .OrderByDescending(o => o.EffectiveFrom).FirstOrDefault();
            if (ov is not null)
            {
                var s = schedules.FirstOrDefault(x => x.Id == ov.PayScheduleId);
                if (s is not null) return s;
            }
            return schedules
                .Where(s => s.IsActive && s.EffectiveFrom <= d && (s.EffectiveTo == null || s.EffectiveTo >= d)
                            && (s.AppliesTo == group || s.AppliesTo == PayScheduleGroup.All))
                .OrderByDescending(s => s.AppliesTo == group)
                .ThenByDescending(s => s.EffectiveFrom)
                .FirstOrDefault();
        }

        var now = At(periodStart);
        if (now is null && schedules.Count == 0 && overrides.Count == 0)
            return PayScheduleContext.Monthly(periodStart.Month);

        var ppm = Math.Clamp(now?.PeriodsPerMonth ?? 1, 1, 2);
        var secondDay = Math.Clamp(now?.SecondTermStartDay ?? 16, 2, 28);
        var term = ppm == 2 && periodStart.Day >= secondDay ? 2 : 1;
        var fraction = 1m / ppm;

        var periods = ppm - term + 1;
        var months = periods * fraction;

        for (var m = periodStart.Month + 1; m <= 12; m++)
        {
            var s = At(new DateOnly(periodStart.Year, m, 1));
            months += 1m;
            periods += Math.Clamp(s?.PeriodsPerMonth ?? 1, 1, 2);
        }

        return new PayScheduleContext(now?.Code ?? "MONTHLY", ppm, term, fraction, months, periods);
    }
}
