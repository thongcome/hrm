namespace HRM.Models;

// บัญชีผู้ใช้ผูกกับพนักงานด้วย id (CEO, 21–22 ก.ย. 2569: ความสัมพันธ์ผูกด้วย id, code เป็นตัวตนที่ธุรกิจรู้)
//   hremployee_id = พนักงานเจ้าของบัญชี — ESS/MSS, ชื่อผู้อนุมัติใน workflow, บทบาทอัตโนมัติ อ่านค่านี้
// empid (EMP_NO) ยังอยู่ เป็นสำเนาให้คนอ่าน/ค้นหา/ตัวนำเข้า — HRMContext.UserEmployee.cs ทำให้ตรงกับ id เสมอ
// ว่าง = บัญชีระบบที่ไม่ใช่พนักงาน (เช่น advadmin) · FK → HREMPLOYEE(id) มาจาก migration 20260922110000_UserEmployeeById
public partial class sc_user
{
    public long? hremployee_id { get; set; }
}
