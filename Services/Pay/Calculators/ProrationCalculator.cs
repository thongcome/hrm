namespace HRM.Services.Pay.Calculators;

// Pure, no DB access. The legacy engine (PayrollProcess.razor CalculatePayroll)
// always used the full monthly salary regardless of join/resign date — this
// never existed before.
public static class ProrationCalculator
{
    // ProrationFactor is rounded to 4 decimals for display/storage only. Money must be computed
    // from ExactFactor — 30,000 × 0.3333 = 9,999 instead of 10,000 (audit L-02).
    public record ProrationResult(decimal ProrationFactor, int WorkingDaysInPeriod, int ActualWorkingDays, decimal ExactFactor);

    // daysPerMonthDivisor: ถ้าส่งมา (audit M7) สัดส่วน = วันที่ทำงานจริง ÷ ตัวหาร (เช่น 30) ไม่เกิน 1 — สูตรเดียวกับ
    // ที่ใช้หักขาดงาน คนเข้า/ออกกลางเดือนกับคนขาดงานจึงได้ "ค่าจ้างต่อวัน" ตัวเลขเดียวกัน
    // ไม่ส่งมา = สัดส่วนตามจำนวนวันจริงของงวด (พฤติกรรมเดิม)
    //
    // monthFraction: ส่วนของเดือนที่งวดนี้ครอบคลุม (งวดครึ่งเดือน = ½) ตัวหารรายเดือนต้องย่อตามงวด
    // (30 × ½ = 15) เพราะเงินเดือนของงวดถูกคูณ ½ อยู่แล้ว — เดิมคิดซ้อนสองชั้น เข้างาน 6 ม.ค. เงินเดือน 30,000
    // รอบ 1–15 ม.ค. ได้ 4,999.50 แทน 10,000 (audit H-06)
    public static ProrationResult Calculate(DateOnly periodStart, DateOnly periodEnd, DateOnly? joinDate, DateOnly? resignDate,
        int? daysPerMonthDivisor = null, decimal monthFraction = 1m)
    {
        if (periodEnd < periodStart)
            throw new ArgumentException("periodEnd must not be before periodStart");
        if (monthFraction <= 0m || monthFraction > 1m)
            throw new ArgumentOutOfRangeException(nameof(monthFraction), "monthFraction must be in (0, 1]");

        var totalDays = periodEnd.DayNumber - periodStart.DayNumber + 1;
        decimal divisor = daysPerMonthDivisor is int d && d > 0 ? d * monthFraction : totalDays;

        var effectiveStart = periodStart;
        if (joinDate.HasValue && joinDate.Value > effectiveStart)
            effectiveStart = joinDate.Value;

        var effectiveEnd = periodEnd;
        if (resignDate.HasValue && resignDate.Value < effectiveEnd)
            effectiveEnd = resignDate.Value;

        if (effectiveEnd < effectiveStart)
            return new ProrationResult(0m, totalDays, 0, 0m);

        var actualDays = effectiveEnd.DayNumber - effectiveStart.DayNumber + 1;
        // ทำงานครบทั้งงวด = เงินเดือนเต็มเสมอ ไม่ว่าตัวหารจะเป็น 30 และเดือนนั้นมี 31 วัน
        var exact = actualDays >= totalDays ? 1m : Math.Min(1m, actualDays / divisor);

        return new ProrationResult(Math.Round(exact, 4, MidpointRounding.AwayFromZero), totalDays, actualDays, exact);
    }
}
