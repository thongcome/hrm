namespace HRM.Services.Att.Calculators;

// Pure, no DB access. The weekly OT cap check: given the hours already booked for the employee's week and the
// hours a new request would add, does the total pass the configured cap? The cap itself comes from Att_OtPolicy.
public static class OtWeeklyCap
{
    public record Result(decimal Cap, decimal ExistingHours, decimal NewHours, decimal TotalHours, bool Exceeds, decimal Excess);

    public static Result Evaluate(decimal cap, decimal existingHours, decimal newHours)
    {
        var total = existingHours + newHours;
        var excess = Math.Max(0m, total - cap);
        return new Result(cap, existingHours, newHours, total, excess > 0m, excess);
    }

    // Monday of the week the date falls in (the week runs Monday–Sunday, same as the OT-over-cap report).
    public static DateOnly WeekStart(DateOnly date)
    {
        var back = ((int)date.DayOfWeek + 6) % 7;   // Monday = 0 … Sunday = 6
        return date.AddDays(-back);
    }
}
