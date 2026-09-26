using HRM.Models;
using Microsoft.Extensions.Configuration;

namespace HRM.Services.Pay;

// แยกหน้าที่ (audit H-19): คนที่คำนวณ/ส่งตรวจตัวเลขแล้ว ห้ามเป็นคนอนุมัติ/บันทึกบัญชี/ปล่อยไฟล์ธนาคาร/
// ยืนยันจ่ายเงินเอง — บังคับที่ชั้น service ไม่ใช่แค่ที่ UI
// "Payroll:RequireSeparateApprover": false ปิดได้เฉพาะ dev/demo ที่มีผู้ใช้คนเดียว ค่าเริ่มต้นเปิดเสมอ
public static class PayrollSeparationOfDuties
{
    public static bool IsRequired(IConfiguration? configuration) =>
        configuration?.GetValue<bool?>("Payroll:RequireSeparateApprover") ?? true;

    // ขั้นตอนหลังอนุมัติ — บันทึกบัญชี (Post) / ไฟล์ธนาคาร / ยืนยันจ่าย (CEO, 18 ก.ย. 2569; พอร์ตจาก Advance.Payroll):
    // บริษัทเล็กมีผู้ทำหนึ่งคน + ผู้อนุมัติหนึ่งคน ผู้ทำจึงปิดงานรอบที่อนุมัติแล้วเองได้ตามค่าตั้งต้น ไม่ต้องมีคนที่สาม
    // ตัวการอนุมัติยังแยกหน้าที่เหมือนเดิม (IsRequired) และการโอนจริงยังมี maker/authorizer ของธนาคารคุมอีกชั้น
    // ลูกค้าที่ต้องการให้การเงินเป็นคนปล่อยไฟล์ ตั้ง "Payroll:SeparateFinalizer": true
    public static bool IsRequiredAfterApproval(IConfiguration? configuration) =>
        IsRequired(configuration) && (configuration?.GetValue<bool?>("Payroll:SeparateFinalizer") ?? false);

    public static void EnsureNotPreparer(Pay_PayrollRun run, long actorUserId, string step, bool required)
    {
        if (required && (run.CalculatedByUserId == actorUserId || run.ReviewedByUserId == actorUserId))
            throw new InvalidOperationException(
                $"{step}ต้องทำโดยคนละคนกับผู้คำนวณ/ผู้ส่งตรวจ (แยกหน้าที่) — ให้ผู้มีสิทธิ์อีกคนเป็นผู้ดำเนินการ");
    }
}
