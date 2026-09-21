using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// ประเภทพนักงาน (employee-type lookup for the Position module) — ported from
// legacy PIS "employeetype" table (Position/EmployeeType/EmployeeTypeCreate.jsp).
// Distinct from Hremployee.EmptypeCode (a free-text code already in use for
// payroll) — this is the Position module's own classification used to scope
// position slots and position titles (พนักงานประจำ/สัญญาจ้าง/ชั่วคราว/กรรมการ).
[Table("Pos_EmployeeType")]
public class Pos_EmployeeType
{
    [Key]
    public long Id { get; set; }

    // Stable human-facing code for this record (audit: every master/document table needs one beyond the surrogate Id).
    [StringLength(30)]
    public string? Code { get; set; }

    [Required, StringLength(6)]
    public string CompanyId { get; set; } = null!;

    [Required, StringLength(200)]
    public string Name { get; set; } = null!;

    [StringLength(1000)]
    public string? Remark { get; set; }

    public bool IsActive { get; set; } = true;

    // "รับเงินเดือนผ่านระบบ" (CEO, 18 ก.ย. 2569): whether people of this type are paid by the
    // payroll run at all. Matched to Hremployee.EMPTYPE_CODE by Code within the same CompanyId.
    // False for e.g. directors who are on the employee register but not on payroll, so the
    // engine skips them with a stated reason instead of paying them 0 and raising "net pay 0".
    public bool IsPaidByPayroll { get; set; } = true;

    // วิธีนับวันจ่ายค่าจ้างรายวันของประเภทนี้ (ว่าง = ใช้ของบริษัทใน Pay_AttendanceDeductionPolicy) — PST, 21 ก.ย. 2569:
    // "รายวันแบบประจำ" คิด 22 วันตายตัว ขณะที่รายวันทั่วไปคิดตามวันทำงานจริงเชื่อมปฏิทินวันหยุดบริษัท
    public PayDailyWageDaysMode? DailyWageMode { get; set; }
    public int? DailyWageFixedDays { get; set; }
}
