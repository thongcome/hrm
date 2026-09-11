using HRM.Models;

namespace HRM.Services.Pay;

// ผลการอ่านปฏิทินจ่ายของพนักงานหนึ่งคน ณ งวดหนึ่ง — ตัวเลขที่สูตรภาษีต้องใช้ (audit M8)
//   MonthFraction                 งวดนี้คิดเป็นกี่ส่วนของเดือน (1 = รายเดือน, 0.5 = ครึ่งเดือน)
//   RemainingMonthsIncludingThis  เหลืออีกกี่เดือน (รวมส่วนของเดือนนี้ที่ยังไม่จ่าย) จนสิ้นปี — ใช้ประมาณการเงินได้ทั้งปี
//   RemainingPeriodsIncludingThis เหลืออีกกี่งวดจริงตามปฏิทิน — ใช้แบ่งภาษีที่เหลือลงแต่ละงวด
public sealed record PayScheduleContext(
    string ScheduleCode,
    int PeriodsPerMonth,
    int TermNo,
    decimal MonthFraction,
    decimal RemainingMonthsIncludingThis,
    int RemainingPeriodsIncludingThis)
{
    public decimal RemainingMonthsAfterThis => Math.Max(0m, RemainingMonthsIncludingThis - MonthFraction);

    // ไม่มี config = เดือนละงวด (พฤติกรรมเดิมของระบบก่อนมีตารางรอบจ่าย)
    public static PayScheduleContext Monthly(int month) => new("MONTHLY", 1, 1, 1m, 13 - month, 13 - month);
}

public static class PayScheduleResolver
{
    // กติกาเดียวกับตัวคำนวณค่าจ้าง: มีค่าจ้างรายวันและไม่มีเงินเดือน = พนักงานรายวัน
    public static PayScheduleGroup GroupOf(Hremployee emp)
        => (emp.DailyWage ?? 0m) > 0m && (emp.SalaryAmt ?? 0m) <= 0m
            ? PayScheduleGroup.DailyWage
            : PayScheduleGroup.MonthlySalaried;

    // pure — ทดสอบได้โดยไม่ต้องมีฐานข้อมูล ผู้เรียก preload ตารางทั้งสองมาครั้งเดียวต่อรอบ
    public static PayScheduleContext Resolve(
        long hremployeeId, PayScheduleGroup group, DateOnly periodStart,
        IReadOnlyList<Pay_PaySchedule> schedules, IReadOnlyList<Pay_EmployeePayScheduleOverride> overrides)
    {
        Pay_PaySchedule? At(DateOnly d)
        {
            // ทับรายคนก่อน
            var ov = overrides
                .Where(o => o.HremployeeId == hremployeeId && o.IsActive && o.EffectiveFrom <= d && (o.EffectiveTo == null || o.EffectiveTo >= d))
                .OrderByDescending(o => o.EffectiveFrom).FirstOrDefault();
            if (ov is not null)
            {
                var s = schedules.FirstOrDefault(x => x.Id == ov.PayScheduleId);
                if (s is not null) return s;
            }
            // ค่าเริ่มต้นตามประเภทพนักงาน — แถวที่ระบุกลุ่มตรงชนะแถว "ทุกคน", แถวที่มีผลล่าสุดชนะ
            return schedules
                .Where(s => s.IsActive && s.EffectiveFrom <= d && (s.EffectiveTo == null || s.EffectiveTo >= d)
                            && (s.AppliesTo == group || s.AppliesTo == PayScheduleGroup.All))
                .OrderByDescending(s => s.AppliesTo == group)
                .ThenByDescending(s => s.EffectiveFrom)
                .FirstOrDefault();
        }

        var now = At(periodStart);
        if (now is null && schedules.Count == 0 && overrides.Count == 0)
            return PayScheduleContext.Monthly(periodStart.Month);

        var ppm = Math.Clamp(now?.PeriodsPerMonth ?? 1, 1, 2);
        var secondDay = Math.Clamp(now?.SecondTermStartDay ?? 16, 2, 28);
        var term = ppm == 2 && periodStart.Day >= secondDay ? 2 : 1;
        var fraction = 1m / ppm;

        // เดือนนี้: งวดนี้และงวดที่เหลือของเดือน
        var periods = ppm - term + 1;
        var months = periods * fraction;

        // เดือนถัดไปจนถึงธันวาคม: อ่านปฏิทินของแต่ละเดือน (ความถี่อาจเปลี่ยนกลางปี)
        for (var m = periodStart.Month + 1; m <= 12; m++)
        {
            var s = At(new DateOnly(periodStart.Year, m, 1));
            months += 1m;
            periods += Math.Clamp(s?.PeriodsPerMonth ?? 1, 1, 2);
        }

        return new PayScheduleContext(now?.Code ?? "MONTHLY", ppm, term, fraction, months, periods);
    }
}
