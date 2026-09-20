namespace HRM.Services.Pay;

using HRM.Models;

// The only payroll run types this product has: Regular and Bonus.
//
// There is no reversal ("กลับรายการ") and no whole-period adjustment run (CEO, 17 ก.ย. 2569):
// a closed period is never re-opened or negated as a whole. When an employee's source data
// was wrong or arrived late, that employee gets a one-off earning/deduction
// (Pay_AdhocPayItem) in the next period.
//
// PayrollRunType.Adjustment/Reversal still exist in the enum only because the database may
// hold such historical rows; every service refuses to act on them.
public static class PayrollRunTypes
{
    public const string UnsupportedMessage =
        "ประเภทรอบนี้ไม่มีในระบบ — มีเฉพาะรอบปกติ รอบโบนัส และรอบจ่ายคนออก ถ้าข้อมูลของพนักงานคนใดผิด ให้บันทึกเงินได้/เงินหักรายครั้งในงวดถัดไป";

    public static bool IsSupported(PayrollRunType type) =>
        type is PayrollRunType.Regular or PayrollRunType.Bonus or PayrollRunType.FinalPay;

    public static string Label(PayrollRunType type) => type switch
    {
        PayrollRunType.Bonus => "โบนัส",
        PayrollRunType.FinalPay => "จ่ายคนออก",
        _ => "ปกติ",
    };
}
