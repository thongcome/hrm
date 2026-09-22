namespace HRM.Models;

// หัวหน้า / ผู้อนุมัติของหน่วยงาน ผูกด้วย id ของพนักงาน (CEO, 21–22 ก.ย. 2569: ความสัมพันธ์ผูกด้วย id, code เป็นตัวตนที่ธุรกิจรู้)
//   approver_hremployee_id = ผู้อนุมัติตามผัง — ตัวที่ workflow ไต่สายอนุมัติ และที่ MSS ใช้ตัดสินว่าใครเป็นหัวหน้า
//   boss_hremployee_id     = หัวหน้าหน่วยงานตามตำแหน่ง (อาจต่างจากผู้อนุมัติเมื่อมีการมอบอำนาจ)
// approver_empid / boss_emp_id (EMP_NO) ยังอยู่ เป็นสำเนาให้คนอ่านและตัวนำเข้า Excel — HRMContext.OrgParent.cs ทำให้ตรงกับ id เสมอ
// FK → HREMPLOYEE(id) มาจาก migration 20260922100000_OrgHeadsById
public partial class com_organization
{
    public long? approver_hremployee_id { get; set; }

    public long? boss_hremployee_id { get; set; }
}
