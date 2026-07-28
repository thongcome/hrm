namespace HRM.Services.Pay.Calculators;

// Pure, no DB access. The legacy engine (PayrollProcess.razor CalculatePayroll)
// always used the full monthly salary regardless of join/resign date — this
// never existed before.
public static class ProrationCalculator
{
    public record ProrationResult(decimal ProrationFactor, int WorkingDaysInPeriod, int ActualWorkingDays);

    public static ProrationResult Calculate(DateOnly periodStart, DateOnly periodEnd, DateOnly? joinDate, DateOnly? resignDate)
    {
        if (periodEnd < periodStart)
            throw new ArgumentException("periodEnd must not be before periodStart");

        var totalDays = periodEnd.DayNumber - periodStart.DayNumber + 1;

        var effectiveStart = periodStart;
        if (joinDate.HasValue && joinDate.Value > effectiveStart)
            effectiveStart = joinDate.Value;

        var effectiveEnd = periodEnd;
        if (resignDate.HasValue && resignDate.Value < effectiveEnd)
            effectiveEnd = resignDate.Value;

        if (effectiveEnd < effectiveStart)
            return new ProrationResult(0m, totalDays, 0);

        var actualDays = effectiveEnd.DayNumber - effectiveStart.DayNumber + 1;
        var factor = totalDays == 0
            ? 0m
            : Math.Round((decimal)actualDays / totalDays, 4, MidpointRounding.AwayFromZero);

        return new ProrationResult(factor, totalDays, actualDays);
    }
}
