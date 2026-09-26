using System.Security.Cryptography;

namespace HRM.Services.Security;

// รหัสผ่านชั่วคราวสำหรับบัญชีที่เพิ่งสร้างหรือเพิ่งรีเซ็ต
//
// เดิมหน้าจัดการผู้ใช้ใช้ค่าคงที่ "Abcd@2025" ทั้งตอนสร้างและตอนรีเซ็ต — รหัสเดียวใช้ได้กับทุกบัญชีใหม่
// ของ **ทุกการติดตั้งทุกลูกค้า** ใครเห็นโค้ดหรือเคยถูกสร้างบัญชีสักครั้งก็เดาของคนอื่นได้ (ADP.AI แจ้ง 26 ก.ย. 2569)
// ตอนนี้สุ่มใหม่ทุกครั้ง แสดงบนหน้าจอครั้งเดียวให้ผู้ดูแลคัดลอกไปส่งต่อ แล้วบังคับเปลี่ยนตอน login แรก
// (ForcePasswordChangeMiddleware) — ไม่เก็บเป็นข้อความธรรมดาและไม่เขียนลง log ที่ไหน
//
// ตัวอักษรที่สับสนง่าย (I, l, O, 0, 1) ถูกตัดออก เพราะรหัสนี้ต้องอ่านออกเสียง/พิมพ์ต่อทางโทรศัพท์ได้จริง
public static class TemporaryPassword
{
    private const string Upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Lower = "abcdefghijkmnpqrstuvwxyz";
    private const string Digits = "23456789";
    private const string Symbols = "!@#$%*?";

    /// <summary>16 ตัวอักษร มีตัวใหญ่ ตัวเล็ก ตัวเลข และสัญลักษณ์อย่างละอย่างน้อยหนึ่งตัว (ตามนโยบายของ Identity)</summary>
    public static string New()
    {
        var all = Upper + Lower + Digits + Symbols;
        var chars = new List<char>
        {
            Upper[RandomNumberGenerator.GetInt32(Upper.Length)],
            Lower[RandomNumberGenerator.GetInt32(Lower.Length)],
            Digits[RandomNumberGenerator.GetInt32(Digits.Length)],
            Symbols[RandomNumberGenerator.GetInt32(Symbols.Length)],
        };
        while (chars.Count < 16) chars.Add(all[RandomNumberGenerator.GetInt32(all.Length)]);
        return new string(chars.OrderBy(_ => RandomNumberGenerator.GetInt32(int.MaxValue)).ToArray());
    }
}
