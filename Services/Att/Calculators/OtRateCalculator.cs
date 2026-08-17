namespace HRM.Services.Att.Calculators;

// Pure, static — no DB access. Implements the day-type -> multiplier
// arithmetic behind มาตรา 61-63 พ.ร.บ.คุ้มครองแรงงาน: overtime pay is the
// employee's hourly wage times a multiplier that depends on whether the OT
// falls on a normal workday, a public/company holiday, or the weekly rest
// day. The multiplier itself is NOT hardcoded here — callers look it up
// from Att_OtRule (config, editable by HR) and pass it in. This class only
// knows how to turn (monthly wage, hours, multiplier) into a baht amount
// and how to classify a date into a day type.
public static class OtRateCalculator
{
    // Standard Thai payroll convention also used by WelfareFundCalculator:
    // 30 days/month. Hourly wage further divides the daily wage by the
    // standard 8-hour workday.
    public static decimal CalculateHourlyWage(decimal monthlySalary) =>
        Math.Round(monthlySalary / 30m / 8m, 2);

    public static decimal CalculateOtAmount(decimal monthlySalary, decimal hours, decimal multiplier) =>
        Math.Round(CalculateHourlyWage(monthlySalary) * multiplier * hours, 2);

    // isHoliday must be resolved by the caller against Lve_CompanyHoliday
    // (company-specific) — this method has no DB access. RestDay uses the
    // same Saturday/Sunday convention as LeaveDayCalculator; this codebase
    // has no per-company configurable weekly rest day yet.
    public static HRM.Models.OtDayType ClassifyDayType(DateOnly date, bool isHoliday)
    {
        if (isHoliday) return HRM.Models.OtDayType.Holiday;
        if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) return HRM.Models.OtDayType.RestDay;
        return HRM.Models.OtDayType.Workday;
    }
}
