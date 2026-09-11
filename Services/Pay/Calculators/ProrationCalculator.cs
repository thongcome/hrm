namespace HRM.Services.Pay.Calculators;

// Pure, no DB access. The legacy engine (PayrollProcess.razor CalculatePayroll)
// always used the full monthly salary regardless of join/resign date — this
// never existed before.
public static class ProrationCalculator
{
    public record ProrationResult(decimal ProrationFactor, int WorkingDaysInPeriod, int ActualWorkingDays);

    // daysPerMonthDivisor: ถ้าส่งมา (audit M7) สัดส่วน = วันที่ทำงานจริง ÷ ตัวหาร (เช่น 30) ไม่เกิน 1 — สูตรเดียวกับ
    // ที่ใช้หักขาดงาน คนเข้า/ออกกลางเดือนกับคนขาดงานจึงได้ "ค่าจ้างต่อวัน" ตัวเลขเดียวกัน
    // ไม่ส่งมา = สัดส่วนตามจำนวนวันจริงของงวด (พฤติกรรมเดิม)
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
        // ทำงานครบทั้งงวด = เงินเดือนเต็มเสมอ ไม่ว่าตัวหารจะเป็น 30 และเดือนนั้นมี 31 วัน
        var factor = actualDays >= totalDays
            ? 1m
            : Math.Round(Math.Min(1m, (decimal)actualDays / divisor), 4, MidpointRounding.AwayFromZero);

        return new ProrationResult(factor, totalDays, actualDays);
    }
}
