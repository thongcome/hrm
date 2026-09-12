using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Advance.Payroll.Domain;

// รอบจ่ายเฉพาะรายคน — ทับค่าเริ่มต้นตามประเภทพนักงาน (CEO, 11 ก.ย. 2569: "ใช้ประเภทพนักงานเป็นค่าเริ่มต้น
// ทับรายคนได้") มีผลตั้งแต่-ถึงวันที่ เมื่อหมดช่วงกลับไปใช้ค่าเริ่มต้นเอง
[Table("Pay_EmployeePayScheduleOverride")]
public class Pay_EmployeePayScheduleOverride
{
    [Key]
    public long Id { get; set; }

    public long HremployeeId { get; set; }

    public long PayScheduleId { get; set; }

    public DateOnly EffectiveFrom { get; set; }

    public DateOnly? EffectiveTo { get; set; }

    public bool IsActive { get; set; } = true;

    [StringLength(500)]
    public string? Note { get; set; }

    public long EnteredByUserId { get; set; }
    public DateTime EnteredDate { get; set; } = DateTime.Now;

    // TODO(seam): Hremployee navigation removed - Advance.Payroll.Domain does not own the employee entity.
    // HremployeeId (scalar FK, kept above) is resolved against pay_employee via IEmployeeSource. See EXTRACTION-PLAN.md.
    // public virtual Hremployee Hremployee { get; set; } = null!;
    public virtual Pay_PaySchedule PaySchedule { get; set; } = null!;
}
