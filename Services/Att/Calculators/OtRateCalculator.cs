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

    // ── เงินที่จ่ายจริงตามกฎหมาย (ม.61–63, ม.68) ──────────────────────────────────────────────
    // ค่าจ้างรายชั่วโมง: รายเดือน = เงินเดือน ÷ 30 ÷ ชั่วโมงทำงานปกติต่อวัน · รายวัน = ค่าจ้างรายวัน ÷ ชั่วโมงทำงานปกติ
    //  · วันทำงาน — ล่วงเวลา ไม่น้อยกว่า 1.5 เท่า (ม.61)
    //  · วันหยุด/วันหยุดประจำสัปดาห์ ภายในชั่วโมงทำงานปกติ — รายเดือนได้ค่าจ้างวันหยุดอยู่แล้วจึงได้เพิ่มอีก 1 เท่า,
    //    รายวันไม่ได้ค่าจ้างวันนั้นจึงได้ 2 เท่า (ม.62)
    //  · วันหยุด เกินชั่วโมงทำงานปกติ — ไม่น้อยกว่า 3 เท่า (ม.63)
    // ตัวคูณที่ตั้งใน Att_OtRule ใช้ได้เมื่อ "สูงกว่า" ขั้นต่ำตามกฎหมายเท่านั้น
    public const decimal WorkdayOtMinimum = 1.5m;
    public const decimal HolidayOtMinimum = 3m;

    public sealed record OtPay(decimal Amount, decimal HourlyWage, string Note);

    public static OtPay CalculatePay(decimal? monthlySalary, decimal? dailyWage, decimal hours, HRM.Models.OtDayType dayType,
        decimal? configuredMultiplier, decimal normalHoursPerDay = 8m)
    {
        var normal = normalHoursPerDay > 0 ? normalHoursPerDay : 8m;
        var isDaily = (dailyWage ?? 0m) > 0m && (monthlySalary ?? 0m) <= 0m;
        var hourly = isDaily ? dailyWage!.Value / normal : (monthlySalary ?? 0m) / 30m / normal;
        if (hourly <= 0m || hours <= 0m) return new OtPay(0m, 0m, "ไม่มีค่าจ้างในข้อมูลพนักงาน หรือไม่มีชั่วโมง");

        if (dayType == HRM.Models.OtDayType.Workday)
        {
            var m = Math.Max(WorkdayOtMinimum, configuredMultiplier ?? WorkdayOtMinimum);
            var amt = Math.Round(hours * hourly * m, 2, MidpointRounding.AwayFromZero);
            return new OtPay(amt, hourly, $"ล่วงเวลาวันทำงาน {hours:0.##} ชม. × {hourly:N2} × {m:0.##} เท่า (ม.61) = {amt:N2}");
        }

        var normalPart = Math.Min(hours, normal);
        var otPart = hours - normalPart;
        var workMult = isDaily ? 2m : 1m;
        var otMult = Math.Max(HolidayOtMinimum, configuredMultiplier ?? HolidayOtMinimum);
        var amount = Math.Round(normalPart * hourly * workMult + otPart * hourly * otMult, 2, MidpointRounding.AwayFromZero);
        var note = $"ทำงานวันหยุด {normalPart:0.##} ชม. × {hourly:N2} × {workMult:0.##} เท่า (ม.62{(isDaily ? " รายวัน" : " รายเดือน")})"
                 + (otPart > 0 ? $" + ล่วงเวลาวันหยุด {otPart:0.##} ชม. × {otMult:0.##} เท่า (ม.63)" : "")
                 + $" = {amount:N2}";
        return new OtPay(amount, hourly, note);
    }

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
