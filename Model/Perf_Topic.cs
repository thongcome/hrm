using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// กลุ่มหัวข้อการประเมิน ชั้นบนสุดของโครงสร้าง 2 ชั้น (legacy: Topic) เช่น
// "ด้านผลงาน (KPI)", "ด้านสมรรถนะ (Competency)", "ด้านจริยธรรม" — Weight ของ
// ทุก Topic ภายใต้ EvaluationType เดียวกันต้องรวมกัน = 100% (ตรวจใน service
// layer ตอนบันทึก ไม่ใช้ DB constraint ตาม house style)
[Table("Perf_Topic")]
public class Perf_Topic
{
    [Key]
    public long Id { get; set; }

    public long EvaluationTypeId { get; set; }

    [Required, StringLength(20)]
    public string Code { get; set; } = null!;
    [Required, StringLength(300)]
    public string Name { get; set; } = null!;

    [Column(TypeName = "decimal(5,2)")]
    public decimal Weight { get; set; }

    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual Perf_EvaluationType EvaluationType { get; set; } = null!;
}
