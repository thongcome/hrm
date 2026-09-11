using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// ร่องรอยการเปลี่ยนรอบจ่าย (CEO, 12 ก.ย. 2569: "ตั้งรอบนี่ต้องแก้ยากหน่อย") — ทุกการเพิ่ม/แก้/ปิด/ทับรายคน
// ต้องมีเหตุผล เก็บใครทำ เมื่อไร ก่อน-หลังเป็นอะไร แสดงในหน้ารอบจ่ายเอง (AuditLog กลางยังบันทึกอัตโนมัติเช่นเดิม)
[Table("Pay_PayScheduleChangeLog")]
public class Pay_PayScheduleChangeLog
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(50)]
    public string CompanyId { get; set; } = null!;

    public long? PayScheduleId { get; set; }

    public long? OverrideId { get; set; }

    // Add / Edit / Close / OverrideAdd / OverrideDeactivate
    [Required, StringLength(30)]
    public string Action { get; set; } = null!;

    [Required, StringLength(500)]
    public string Reason { get; set; } = null!;

    // สรุปสั้น ๆ ว่าอะไรเปลี่ยน (ก่อน → หลัง)
    [StringLength(1000)]
    public string? Detail { get; set; }

    public long ChangedByUserId { get; set; }

    public DateTime ChangedDate { get; set; } = DateTime.Now;
}
