using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// สถานะผู้ประกันตน ม.33 ที่ HR กำหนดเองรายคน — ทับค่าเริ่มต้นที่ระบบคิดจากอายุวันเริ่มงาน (SsoCoverage)
// เช่น พนักงานเข้าใหม่อายุเกิน 60 ที่เคยเป็นผู้ประกันตน ม.33 มาก่อน (ยังต้องหัก) หรือลูกจ้างที่ได้รับยกเว้นตาม ม.4 (ไม่หัก)
// มีผลตั้งแต่-ถึงวันที่ แบบเดียวกับ Pay_EmployeePayScheduleOverride; ไม่ลบ — ปิดใช้งานแทน
[Table("Pay_EmployeeSsoCoverage")]
public class Pay_EmployeeSsoCoverage
{
    [Key]
    public long Id { get; set; }

    public long HremployeeId { get; set; }

    public bool IsInsured { get; set; }

    public DateOnly EffectiveFrom { get; set; }

    public DateOnly? EffectiveTo { get; set; }

    [StringLength(200)]
    public string? Reason { get; set; }

    public bool IsActive { get; set; } = true;

    [StringLength(500)]
    public string? Note { get; set; }

    public long EnteredByUserId { get; set; }
    public DateTime EnteredDate { get; set; } = DateTime.Now;

    public virtual Hremployee Hremployee { get; set; } = null!;
}
