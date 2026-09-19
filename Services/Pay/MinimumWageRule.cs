using HRM.Models;

namespace HRM.Services.Pay;

// The one definition of "what is the legal daily minimum for this employee on this date" and
// "is this pay below it" — used by the pre-flight check so payroll cannot quietly pay under it.
public static class MinimumWageRule
{
    // ลูกจ้างรายเดือนเทียบค่าจ้างขั้นต่ำด้วยเงินเดือน ÷ 30 (วิธีมาตรฐานที่ใช้แปลงเงินเดือนเป็นรายวัน)
    public const decimal DaysPerMonth = 30m;

    // แถวของจังหวัดที่ตรงกันชนะแถว "ทุกจังหวัด" เสมอถ้ามีแถวที่มีผลอยู่ ณ วันนั้น
    public static decimal? DailyMinimum(IEnumerable<Pay_MinimumWage> rows, string? province, DateOnly asOf)
    {
        var inForce = rows.Where(r => r.IsActive && r.EffectiveFrom <= asOf).ToList();
        var key = province?.Trim();
        if (!string.IsNullOrEmpty(key))
        {
            var local = inForce
                .Where(r => string.Equals(r.ProvinceName?.Trim(), key, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(r => r.EffectiveFrom)
                .FirstOrDefault();
            if (local is not null) return local.DailyAmount;
        }
        return inForce
            .Where(r => string.IsNullOrWhiteSpace(r.ProvinceName))
            .OrderByDescending(r => r.EffectiveFrom)
            .FirstOrDefault()?.DailyAmount;
    }

    // คืน null = ไม่มีข้อมูลเงินเดือน/ค่าจ้างให้เทียบ (เรื่องนั้นมีตัวตรวจ NO_SALARY อยู่แล้ว)
    public static decimal? DailyEquivalent(decimal? salaryAmt, decimal? dailyWage)
    {
        if ((dailyWage ?? 0m) > 0m) return dailyWage;
        if ((salaryAmt ?? 0m) > 0m) return salaryAmt / DaysPerMonth;
        return null;
    }

    public static bool IsBelow(decimal? salaryAmt, decimal? dailyWage, decimal? dailyMinimum)
    {
        if (dailyMinimum is null) return false;
        var daily = DailyEquivalent(salaryAmt, dailyWage);
        return daily is not null && daily < dailyMinimum;
    }
}
