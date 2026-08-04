using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// เป้าหมายแบบ OKR-cascade (บริษัท -> หน่วยงาน -> บุคคล) — ไม่มีอยู่ในระบบ JSP
// เดิม เพิ่มใหม่ตามที่ผู้ใช้ยืนยันให้รวมเข้า v1 — ParentGoalId ไล่ทิศทางเดียว
// Company -> Organization -> Employee เท่านั้น (ตรวจใน PerfGoalService ไม่ใช้
// DB constraint) ผูกกับ Perf_Indicator.OkrGoalId ได้เพื่อให้ตัวชี้วัดหนึ่งๆ
// ติดตามความคืบหน้าของเป้านี้โดยตรง
[Table("Perf_Goal")]
public class Perf_Goal
{
    [Key]
    public long Id { get; set; }

    public long EvaluationPeriodId { get; set; }

    public PerfGoalOwnerType OwnerType { get; set; }
    // ใช้เฉพาะเมื่อ OwnerType = Organization
    public long? OwnerOrganizationId { get; set; }
    // ใช้เฉพาะเมื่อ OwnerType = Employee
    public long? OwnerHremployeeId { get; set; }

    public long? ParentGoalId { get; set; }

    [Required, StringLength(300)]
    public string Title { get; set; } = null!;
    [StringLength(2000)]
    public string? Description { get; set; }

    [Column(TypeName = "decimal(15,2)")]
    public decimal? TargetValue { get; set; }
    [StringLength(50)]
    public string? TargetUnit { get; set; } // เช่น "%", "บาท", "ครั้ง"
    [Column(TypeName = "decimal(15,2)")]
    public decimal? CurrentValue { get; set; }

    public PerfGoalStatus Status { get; set; } = PerfGoalStatus.NotStarted;

    public long CreatedByUserId { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public virtual Perf_EvaluationPeriod EvaluationPeriod { get; set; } = null!;
    public virtual Perf_Goal? ParentGoal { get; set; }
}
