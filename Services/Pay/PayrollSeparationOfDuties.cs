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

    public static void EnsureNotPreparer(Pay_PayrollRun run, long actorUserId, string step, bool required)
    {
        if (required && (run.CalculatedByUserId == actorUserId || run.ReviewedByUserId == actorUserId))
            throw new InvalidOperationException(
                $"{step}ต้องทำโดยคนละคนกับผู้คำนวณ/ผู้ส่งตรวจ (แยกหน้าที่) — ให้ผู้มีสิทธิ์อีกคนเป็นผู้ดำเนินการ");
    }
}
