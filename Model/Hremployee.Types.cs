namespace HRM.Models;

// ตำแหน่ง (ระดับ) และประเภทพนักงาน ผูกด้วย id (CEO, 21–22 ก.ย. 2569: ความสัมพันธ์ผูกด้วย id, code เป็นตัวตนที่ธุรกิจรู้)
//   PosExecTypeId  → Pos_ExecType     (ระดับตำแหน่ง เช่น A03 หัวหน้างาน)
//   EmployeeTypeId → Pos_EmployeeType (ประเภทพนักงาน — ตัดสินว่ารับเงินเดือนผ่านระบบไหม, ค่าจ้างรายวันแบบไหน)
// POS_CODE / EMPTYPE_CODE ยังอยู่ เป็นสำเนาให้คนอ่าน/ค้นหา/ตัวนำเข้า Excel — HRMContext.EmployeeTypes.cs ทำให้ตรงกับ id เสมอ
// FK มาจาก migration 20260922120000_EmployeeTypesById
public partial class Hremployee
{
    public long? PosExecTypeId { get; set; }

    public long? EmployeeTypeId { get; set; }
}
