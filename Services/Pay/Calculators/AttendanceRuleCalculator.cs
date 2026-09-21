using HRM.Models;

namespace HRM.Services.Pay.Calculators;

// กติกา "เวลาทำงาน → เงิน" (Pay_AttendanceRule) คิดแบบบริสุทธิ์ ไม่แตะฐานข้อมูล — ให้เครื่องคำนวณเงินเดือนเรียก
// และให้เทสยืนยันสูตรได้ทีละกรณี (PST, 21 ก.ย. 2569: สาย 1 นาทีในรอบ 26–25 เบี้ยขยัน = 0)
public static class AttendanceRuleCalculator
{
    /// <summary>ข้อเท็จจริงเวลาทำงานของพนักงานหนึ่งคนในรอบตัดเวลา</summary>
    public sealed record Facts(IReadOnlyList<int> LateMinutesPerLateDay, int AbsentDays);

    public sealed record Outcome(decimal Deduction, string Note);

    /// <summary>
    /// ช่วงวันที่ที่ใช้อ่านเวลาทำงานของงวด: ไม่ตั้งวันตัด = ช่วงของงวดเอง · ตั้งวันตัด N = วันที่ N+1 ของเดือนก่อน ถึงวันที่ N ของเดือนที่งวดสิ้นสุด
    /// (N เกินจำนวนวันของเดือน → ใช้วันสุดท้ายของเดือนนั้น เช่น ตัด 30 ในเดือน ก.พ.)
    /// </summary>
    public static (DateOnly From, DateOnly To) AttendanceWindow(DateOnly periodStart, DateOnly periodEnd, int? cutoffDay)
    {
        if (cutoffDay is not int n || n < 1 || n >= 31) return (periodStart, periodEnd);
        var to = ClampDay(periodEnd.Year, periodEnd.Month, n);
        var prev = new DateOnly(periodEnd.Year, periodEnd.Month, 1).AddMonths(-1);
        var from = ClampDay(prev.Year, prev.Month, n).AddDays(1);
        return (from, to);
    }

    private static DateOnly ClampDay(int year, int month, int day)
        => new(year, month, Math.Min(day, DateTime.DaysInMonth(year, month)));

    /// <summary>
    /// ยอดที่หักจากเป้าหมายตามกติกาเดียว — ไม่เกินยอดคงเหลือของเป้าหมาย (remainingTarget) และไม่ติดลบ
    /// </summary>
    public static Outcome Apply(Pay_AttendanceRule rule, Facts facts, decimal targetAmount, decimal remainingTarget)
    {
        if (remainingTarget <= 0m || targetAmount <= 0m) return new Outcome(0m, "");

        int occurrences;
        int minutes = 0;
        string what;
        if (rule.Trigger == PayAttendanceTrigger.Late)
        {
            var counted = facts.LateMinutesPerLateDay.Where(m => m > rule.GraceMinutes).ToList();
            occurrences = counted.Count;
            minutes = counted.Sum(m => m - rule.GraceMinutes);
            what = $"มาสาย {occurrences} วัน รวม {minutes} นาที" + (rule.GraceMinutes > 0 ? $" (หลังผ่อนผัน {rule.GraceMinutes} นาที/วัน)" : "");
        }
        else
        {
            occurrences = facts.AbsentDays;
            what = $"ขาดงาน {occurrences} วัน";
        }

        var threshold = Math.Max(1, rule.ThresholdCount);
        if (occurrences < threshold) return new Outcome(0m, "");

        decimal raw;
        string how;
        switch (rule.Formula)
        {
            case PayAttendanceFormula.ForfeitAll:
                raw = targetAmount;
                how = threshold > 1 ? $"ครบ {threshold} ครั้ง → ไม่จ่ายทั้งก้อน" : "→ ไม่จ่ายทั้งก้อน";
                break;
            case PayAttendanceFormula.AmountPerMinute:
                raw = minutes * rule.Value;
                how = $"× {rule.Value:0.####} บาท/นาที";
                break;
            case PayAttendanceFormula.PercentPerMinute:
                raw = minutes * rule.Value / 100m * targetAmount;
                how = $"× {rule.Value:0.####}% ของ {targetAmount:N2} ต่อนาที";
                break;
            case PayAttendanceFormula.AmountPerOccurrence:
                raw = occurrences * rule.Value;
                how = $"× {rule.Value:N2} บาท/ครั้ง";
                break;
            case PayAttendanceFormula.PercentPerOccurrence:
                raw = occurrences * rule.Value / 100m * targetAmount;
                how = $"× {rule.Value:0.####}% ของ {targetAmount:N2} ต่อครั้ง";
                break;
            default:
                return new Outcome(0m, "");
        }

        var deduction = Math.Min(remainingTarget, Math.Round(Math.Max(0m, raw), 2, MidpointRounding.AwayFromZero));
        return deduction <= 0m ? new Outcome(0m, "") : new Outcome(deduction, $"{rule.Name}: {what} {how} = −{deduction:N2}");
    }
}
