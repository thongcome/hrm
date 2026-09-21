using HRM.Models;
using HRM.Services.Pay.Calculators;
using Xunit;

namespace HRM.Tests.Pay;

// กติกา "เวลาทำงาน → เงิน" แบบตั้งค่าได้ (ลูกค้า PST, 21 ก.ย. 2569) — เทสสูตรล้วน ไม่แตะฐานข้อมูล
public class AttendanceRuleCalculatorTests
{
    private static AttendanceRuleCalculator.Facts Late(params int[] minutes) => new(minutes, 0);
    private static AttendanceRuleCalculator.Facts Absent(int days) => new(Array.Empty<int>(), days);

    private static Pay_AttendanceRule Rule(PayAttendanceTrigger trigger, PayAttendanceFormula formula, decimal value = 0m, int grace = 0, int threshold = 1)
        => new() { Name = "เบี้ยขยัน", Trigger = trigger, Formula = formula, Value = value, GraceMinutes = grace, ThresholdCount = threshold };

    [Fact]
    public void Pst_one_minute_late_once_forfeits_the_whole_diligence_allowance()
    {
        var rule = Rule(PayAttendanceTrigger.Late, PayAttendanceFormula.ForfeitAll);
        var outcome = AttendanceRuleCalculator.Apply(rule, Late(1), 500m, 500m);
        Assert.Equal(500m, outcome.Deduction);
        Assert.Contains("ไม่จ่ายทั้งก้อน", outcome.Note);
    }

    [Fact]
    public void Nobody_late_nothing_deducted()
    {
        var rule = Rule(PayAttendanceTrigger.Late, PayAttendanceFormula.ForfeitAll);
        Assert.Equal(0m, AttendanceRuleCalculator.Apply(rule, Late(), 500m, 500m).Deduction);
    }

    [Fact]
    public void Grace_minutes_and_threshold_let_another_customer_be_more_lenient()
    {
        // ที่อื่น: ผ่อนผัน 5 นาที และสายครบ 3 ครั้งจึงตัด
        var rule = Rule(PayAttendanceTrigger.Late, PayAttendanceFormula.ForfeitAll, grace: 5, threshold: 3);
        Assert.Equal(0m, AttendanceRuleCalculator.Apply(rule, Late(3, 10, 20), 500m, 500m).Deduction);      // นับได้ 2 ครั้ง (3 นาทีอยู่ในผ่อนผัน)
        Assert.Equal(500m, AttendanceRuleCalculator.Apply(rule, Late(6, 10, 20), 500m, 500m).Deduction);    // ครบ 3 ครั้ง
    }

    [Theory]
    [InlineData(PayAttendanceFormula.AmountPerMinute, 0.5, 15.00)]        // 30 นาที × 0.50 บาท
    [InlineData(PayAttendanceFormula.PercentPerMinute, 1, 150.00)]         // 30 นาที × 1% ของ 500
    [InlineData(PayAttendanceFormula.AmountPerOccurrence, 50, 100.00)]     // 2 ครั้ง × 50
    [InlineData(PayAttendanceFormula.PercentPerOccurrence, 25, 250.00)]    // 2 ครั้ง × 25% ของ 500
    public void Each_formula_computes_from_minutes_or_occurrences(PayAttendanceFormula formula, double value, double expected)
    {
        var rule = Rule(PayAttendanceTrigger.Late, formula, (decimal)value);
        Assert.Equal((decimal)expected, AttendanceRuleCalculator.Apply(rule, Late(10, 20), 500m, 500m).Deduction);
    }

    [Fact]
    public void Deduction_never_exceeds_what_is_left_of_the_target()
    {
        var rule = Rule(PayAttendanceTrigger.Late, PayAttendanceFormula.AmountPerMinute, 100m);
        Assert.Equal(120m, AttendanceRuleCalculator.Apply(rule, Late(30), 500m, remainingTarget: 120m).Deduction);
        Assert.Equal(0m, AttendanceRuleCalculator.Apply(rule, Late(30), 500m, remainingTarget: 0m).Deduction);
    }

    [Fact]
    public void Absence_rule_counts_absent_days_not_lateness()
    {
        var rule = Rule(PayAttendanceTrigger.Absent, PayAttendanceFormula.ForfeitAll, threshold: 2);
        Assert.Equal(0m, AttendanceRuleCalculator.Apply(rule, Absent(1), 500m, 500m).Deduction);
        Assert.Equal(500m, AttendanceRuleCalculator.Apply(rule, Absent(2), 500m, 500m).Deduction);
        Assert.Equal(0m, AttendanceRuleCalculator.Apply(rule, Late(60, 60, 60), 500m, 500m).Deduction);
    }

    [Fact]
    public void Attendance_window_is_the_period_itself_unless_a_cutoff_day_is_set()
    {
        var start = new DateOnly(2026, 9, 1); var end = new DateOnly(2026, 9, 30);
        Assert.Equal((start, end), AttendanceRuleCalculator.AttendanceWindow(start, end, null));
        // PST: ตัดวันที่ 25 → 26 ส.ค. – 25 ก.ย.
        Assert.Equal((new DateOnly(2026, 8, 26), new DateOnly(2026, 9, 25)), AttendanceRuleCalculator.AttendanceWindow(start, end, 25));
        // ข้ามปี: งวด ม.ค. → 26 ธ.ค. ปีก่อน – 25 ม.ค.
        Assert.Equal((new DateOnly(2025, 12, 26), new DateOnly(2026, 1, 25)),
            AttendanceRuleCalculator.AttendanceWindow(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), 25));
        // ตัดวันที่ 30 ในเดือน ก.พ. → ใช้วันสุดท้ายของเดือน และเริ่มถัดจากวันตัดของ ม.ค.
        Assert.Equal((new DateOnly(2026, 1, 31), new DateOnly(2026, 2, 28)),
            AttendanceRuleCalculator.AttendanceWindow(new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 28), 30));
    }
}
