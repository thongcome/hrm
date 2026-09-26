namespace HRM.Services.Security;

using System.Security.Cryptography;

// รหัสผ่านชั่วคราวตอนสร้างบัญชี / ผู้ดูแลรีเซ็ต — สุ่มใหม่ทุกครั้ง แสดงให้ผู้ดูแลครั้งเดียว และบังคับเปลี่ยนตอนเข้าครั้งแรก
// (isforcechanged ผ่าน PasswordPolicyService). แทนค่าคงที่ "Abcd@2025" ที่เคยใช้กับทุกบัญชี — ใครรู้ค่านั้น
// (อยู่ในซอร์สโค้ดและขึ้นบนหน้าจอ) เข้าบัญชีที่เพิ่งสร้าง/เพิ่งรีเซ็ตได้ก่อนเจ้าของตัวจริง
//
// ยกจากต้นฉบับหลัก ADP.AI Advance.SecurityCore (Services/TemporaryPassword.cs, 2b4abb5) — CEO 26 ก.ย. 2569
// ผ่านกฎรหัสผ่าน Identity ของ HRM เสมอ (≥ 8 · ตัวใหญ่ · ตัวเล็ก · ตัวเลข · อักขระพิเศษ) · ตัดอักษรที่อ่านสับสน
// (0/O · 1/l/I) เพราะผู้ดูแลต้องอ่านบอกผู้ใช้ทางโทรศัพท์
public static class TemporaryPassword
{
    private const string Upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Lower = "abcdefghijkmnopqrstuvwxyz";
    private const string Digits = "23456789";
    private const string Symbols = "@#$%&*?";

    public static string Generate(int length = 12)
    {
        if (length < 8) throw new ArgumentOutOfRangeException(nameof(length), "อย่างน้อย 8 ตัว");
        var all = Upper + Lower + Digits + Symbols;
        var chars = new char[length];
        chars[0] = Pick(Upper);
        chars[1] = Pick(Lower);
        chars[2] = Pick(Digits);
        chars[3] = Pick(Symbols);
        for (var i = 4; i < length; i++) chars[i] = Pick(all);
        RandomNumberGenerator.Shuffle(chars.AsSpan());
        return new string(chars);
    }

    private static char Pick(string set) => set[RandomNumberGenerator.GetInt32(set.Length)];
}
