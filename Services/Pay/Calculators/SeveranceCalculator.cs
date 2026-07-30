namespace HRM.Services.Pay.Calculators;

// Pure, no DB access. Statutory severance per Thai labor law (พ.ร.บ.คุ้มครอง
// แรงงาน มาตรา 118) — tiers by continuous service length. This does NOT
// decide whether severance is owed at all (มาตรา 119 has exceptions for
// termination for cause, e.g. serious misconduct) — that judgment call is
// left to HR before they ever call this; see SeveranceService.
public static class SeveranceCalculator
{
    public record SeveranceResult(int ContinuousServiceDays, string TierDescription, int EntitledDays, decimal DailyWage, decimal Amount);

    public static SeveranceResult Calculate(DateOnly hireDate, DateOnly lastWorkDate, decimal monthlyWage)
    {
        if (lastWorkDate < hireDate)
            throw new ArgumentException("lastWorkDate must not be before hireDate");
        if (monthlyWage <= 0)
            throw new ArgumentException("monthlyWage must be positive");

        var serviceDays = lastWorkDate.DayNumber - hireDate.DayNumber + 1;

        // Uses 365-day years as the tier boundary (a simplification — some
        // employers count calendar-anniversary years instead).
        var (entitledDays, tierDescription) = serviceDays switch
        {
            < 120 => (0, "ทำงานไม่ถึง 120 วัน — ไม่เข้าเงื่อนไขได้รับค่าชดเชย"),
            < 365 => (30, "ทำงานครบ 120 วัน แต่ไม่ถึง 1 ปี — ค่าชดเชย 30 วัน"),
            < 1095 => (90, "ทำงานครบ 1 ปี แต่ไม่ถึง 3 ปี — ค่าชดเชย 90 วัน"),
            < 2190 => (180, "ทำงานครบ 3 ปี แต่ไม่ถึง 6 ปี — ค่าชดเชย 180 วัน"),
            < 3650 => (240, "ทำงานครบ 6 ปี แต่ไม่ถึง 10 ปี — ค่าชดเชย 240 วัน"),
            < 7300 => (300, "ทำงานครบ 10 ปี แต่ไม่ถึง 20 ปี — ค่าชดเชย 300 วัน"),
            _ => (400, "ทำงานครบ 20 ปีขึ้นไป — ค่าชดเชย 400 วัน"),
        };

        var dailyWage = Math.Round(monthlyWage / 30m, 2, MidpointRounding.AwayFromZero);
        var amount = Math.Round(dailyWage * entitledDays, 2, MidpointRounding.AwayFromZero);

        return new SeveranceResult(serviceDays, tierDescription, entitledDays, dailyWage, amount);
    }
}
