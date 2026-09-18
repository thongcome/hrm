namespace HRM.Services.Pay.Calculators;

// Pure, no DB access. Rate/cap come from ISocialSecurityRateProvider.
// grossWage is the MONTHLY wage base (the engine passes month-to-date base).
public static class SocialSecurityCalculator
{
    // พ.ร.บ.ประกันสังคม: ค่าจ้างที่ใช้คำนวณเงินสมทบต่ำสุดเดือนละ 1,650 บาท (audit H-04)
    public const decimal MinimumMonthlyWageBase = 1650m;

    public static decimal Calculate(decimal grossWage, decimal ratePercent, decimal wageCap)
    {
        // A missing ceiling must stop the run, not silently charge SSO on the full salary (audit H-04).
        if (wageCap <= 0)
            throw new InvalidOperationException(
                "ยังไม่ได้ตั้งเพดานค่าจ้างประกันสังคม (อัตราประกันสังคม รหัส 01) — ตั้งค่าที่ Super Master ก่อนคำนวณ");
        // No wage paid this month → no contribution; otherwise the base is floored at 1,650.
        if (grossWage <= 0) return 0m;
        var wageBase = Math.Min(Math.Max(grossWage, MinimumMonthlyWageBase), wageCap);
        return Math.Round(wageBase * ratePercent / 100m, 2, MidpointRounding.AwayFromZero);
    }
}
