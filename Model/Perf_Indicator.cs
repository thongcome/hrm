using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// ตัวชี้วัดจริงที่ให้คะแนน (legacy: รวม "SubTopicPoint" + "EvaluationIndicator"
// เป็นตัวเดียว — ของเดิมมีสองแนวคิดที่ทับซ้อนกัน อันนี้คือ leaf-level item ที่
// ผู้ประเมินกรอกคะแนนจริง) Weight รวมของ Indicator ทุกตัวภายใต้ SubTopic
// เดียวกันต้อง = 100%
[Table("Perf_Indicator")]
public class Perf_Indicator
{
    [Key]
    public long Id { get; set; }

    public long SubTopicId { get; set; }

    [Required, StringLength(20)]
    public string Code { get; set; } = null!;
    [Required, StringLength(300)]
    public string Name { get; set; } = null!;
    [StringLength(300)]
    public string? NameEn { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal Weight { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }
    // อธิบายว่า "คะแนนเต็ม/ดีเยี่ยม" ของตัวชี้วัดนี้หน้าตาเป็นอย่างไร — ช่วยให้
    // ผู้ประเมินหลายคนตัดสินใกล้เคียงกัน (ไม่ใช่ RatingScaleDescription ซึ่งอธิบาย
    // ความหมายของแต้มคะแนน 1-5 แบบรวมทั้งระบบ อันนี้เจาะจงตัวชี้วัดนี้ตัวเดียว)
    [StringLength(1000)]
    public string? TargetDescription { get; set; }

    // ผูกกับเป้า OKR ตัวใดตัวหนึ่ง (nullable — ไม่บังคับทุกตัวชี้วัดต้องผูก OKR)
    // เมื่อผูกแล้ว หน้าให้คะแนนจะโชว์ความคืบหน้าของเป้านั้นประกอบการให้คะแนนได้
    public long? OkrGoalId { get; set; }

    // ผูกกับ competency ในไลบรารีกลาง (nullable, ทรงเดียวกับ OkrGoalId ข้างบน)
    // — สะพานเชื่อม Performance เข้ากับ Competency Library ที่ IDP/Career/
    // Succession/Recruitment ใช้ร่วมกันอยู่แล้ว เมื่อผูกแล้วหน้าให้คะแนน
    // (PerfScore.razor) จะแสดงคำอธิบายพฤติกรรมรายระดับ (BARS จาก
    // Comp_ProficiencyLevel) ของ competency นั้นให้ผู้ประเมินยึดเป็นหลัก
    // แทนที่จะตัดสินจากชื่อตัวชี้วัดลอยๆ
    public long? CompetencyId { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual Perf_SubTopic SubTopic { get; set; } = null!;
    public virtual Perf_Goal? OkrGoal { get; set; }
    public virtual Comp_Competency? Competency { get; set; }
}
