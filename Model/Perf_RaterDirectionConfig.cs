using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// ทิศทางการประเมิน (legacy: EvalDirection) — กำหนดว่าการประเมินชุดหนึ่งจะมี
// ใครเป็นผู้ประเมินบ้าง (self / สายบังคับบัญชาขึ้นกี่ชั้น / สายลูกน้องลงกี่ชั้น /
// เพื่อนร่วมระดับ) และแต่ละทิศทางมีน้ำหนักเท่าไหร่ในคะแนนรวมสุดท้าย —
// WeightSelf+WeightSuperior+WeightSubordinate+WeightPeer ต้องรวม = 100%
// (ตรวจใน service layer)
[Table("Perf_RaterDirectionConfig")]
public class Perf_RaterDirectionConfig
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(6)]
    public string CompanyId { get; set; } = null!;

    [Required, StringLength(200)]
    public string Name { get; set; } = null!;

    public bool AllowSelf { get; set; }
    public int SuperiorLevels { get; set; }    // 0-3
    public int SubordinateLevels { get; set; } // 0-3
    public bool IncludePeers { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal WeightSelf { get; set; }
    // น้ำหนักรวมของสายบังคับบัญชาทั้งหมด (ถ้ามีหลายชั้น จะถัวเฉลี่ยกันเองภายใน
    // สัดส่วนนี้ตอนคำนวณ ไม่ใช่แยกน้ำหนักต่อชั้น)
    [Column(TypeName = "decimal(5,2)")]
    public decimal WeightSuperior { get; set; }
    [Column(TypeName = "decimal(5,2)")]
    public decimal WeightSubordinate { get; set; }
    [Column(TypeName = "decimal(5,2)")]
    public decimal WeightPeer { get; set; }

    public bool IsActive { get; set; } = true;
}
