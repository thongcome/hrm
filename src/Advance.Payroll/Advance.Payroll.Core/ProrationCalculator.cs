namespace Advance.Payroll.Core;

// Ported verbatim from HRM Services/Pay/Calculators/ProrationCalculator.cs — pure, no changes.
public static class ProrationCalculator
{
    public record ProrationResult(decimal ProrationFactor, int WorkingDaysInPeriod, int ActualWorkingDays);

    public static ProrationResult Calculate(DateOnly periodStart, DateOnly periodEnd, DateOnly? joinDate, DateOnly? resignDate,
        int? daysPerMonthDivisor = null)
    {
        if (periodEnd < periodStart)
            throw new ArgumentException("periodEnd must not be before periodStart");

        var totalDays = periodEnd.DayNumber - periodStart.DayNumber + 1;
        var divisor = daysPerMonthDivisor is int d && d > 0 ? d : totalDays;

        var effectiveStart = periodStart;
        if (joinDate.HasValue && joinDate.Value > effectiveStart)
            effectiveStart = joinDate.Value;

        var effectiveEnd = periodEnd;
        if (resignDate.HasValue && resignDate.Value < effectiveEnd)
            effectiveEnd = resignDate.Value;

        if (effectiveEnd < effectiveStart)
            return new ProrationResult(0m, totalDays, 0);

        var actualDays = effectiveEnd.DayNumber - effectiveStart.DayNumber + 1;
        var factor = actualDays >= totalDays
            ? 1m
            : Math.Round(Math.Min(1m, (decimal)actualDays / divisor), 4, MidpointRounding.AwayFromZero);

        return new ProrationResult(factor, totalDays, actualDays);
    }
}
