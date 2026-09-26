namespace HRM.Services.Pay.Calculators;

// ใครเป็นผู้ประกันตน ม.33 ในงวดนี้ — pure, no DB.
//
// พ.ร.บ.ประกันสังคม ม.33: ลูกจ้างอายุไม่ต่ำกว่า 15 และไม่เกิน 60 ปีบริบูรณ์เป็นผู้ประกันตน และคนที่เป็นผู้ประกันตนอยู่แล้ว
// ในวันที่อายุครบ 60 ยังเป็นต่อไป — ค่าเริ่มต้นจึงดูจากอายุ "วันเริ่มงาน" ไม่ใช่อายุวันนี้: เริ่มงานหลังวันครบอายุ MaxEntryAge
// (config ที่แถวอัตราประกันสังคม ค่าเริ่มต้น 60) = ไม่เป็นผู้ประกันตน ไม่หักทั้งฝั่งลูกจ้างและนายจ้าง
//
// ระบบรู้ไม่ครบทุกกรณี (เช่น เคยเป็นผู้ประกันตน ม.33 มาก่อนอายุ 60 แล้วกลับมาทำงาน, หรือข้อยกเว้นตาม ม.4)
// HR จึงทับรายคนได้ที่ Pay_EmployeeSsoCoverage — แถวที่ HR กำหนดชนะค่าเริ่มต้นเสมอ
public static class SsoCoverage
{
    public const int DefaultMaxEntryAge = 60;

    public sealed record Override(long Id, bool IsInsured, DateOnly EffectiveFrom, DateOnly? EffectiveTo, string? Reason);

    public sealed record Result(bool IsInsured, bool FromOverride, string Reason);

    /// <summary>The override in force on <paramref name="asOf"/>: latest EffectiveFrom wins, then the newest row.</summary>
    public static Override? InForce(IEnumerable<Override> overrides, DateOnly asOf) =>
        overrides.Where(o => o.EffectiveFrom <= asOf && (o.EffectiveTo is null || o.EffectiveTo >= asOf))
            .OrderByDescending(o => o.EffectiveFrom).ThenByDescending(o => o.Id)
            .FirstOrDefault();

    /// <summary>
    /// true when the employee started work after their <paramref name="maxEntryAge"/>th birthday. Starting ON the
    /// birthday is still "ไม่เกิน 60 ปีบริบูรณ์". Unknown birth or start date = not over age (the system cannot tell).
    /// </summary>
    public static bool StartedOverAge(DateTime? birthDate, DateTime? workDate, int maxEntryAge) =>
        maxEntryAge > 0 && birthDate is DateTime b && workDate is DateTime w && w.Date > b.Date.AddYears(maxEntryAge);

    public static Result Resolve(DateTime? birthDate, DateTime? workDate, int maxEntryAge, IEnumerable<Override> overrides, DateOnly asOf)
    {
        if (InForce(overrides, asOf) is { } o)
            return new Result(o.IsInsured, true,
                $"HR กำหนดให้{(o.IsInsured ? "เป็น" : "ไม่เป็น")}ผู้ประกันตน ตั้งแต่ {o.EffectiveFrom:yyyy-MM-dd}"
                + (string.IsNullOrWhiteSpace(o.Reason) ? "" : $" — {o.Reason.Trim()}"));

        if (StartedOverAge(birthDate, workDate, maxEntryAge))
            return new Result(false, false,
                $"เริ่มงาน {workDate:yyyy-MM-dd} อายุเกิน {maxEntryAge} ปีบริบูรณ์แล้ว (ครบ {birthDate!.Value.AddYears(maxEntryAge):yyyy-MM-dd}) "
                + "ไม่เป็นผู้ประกันตน ม.33 — ถ้าเคยเป็นผู้ประกันตนมาก่อน ให้ HR กำหนดที่หน้าสถานะผู้ประกันตน");

        return new Result(true, false, "ผู้ประกันตน ม.33");
    }
}
