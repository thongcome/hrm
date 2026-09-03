using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// คำอธิบายความหมายของแต้มคะแนนแต่ละระดับ (legacy: EvalDesc/EvalYearDesc) —
// ผูกกับ EvaluationPeriod เพื่อให้เปลี่ยนคำอธิบายปีต่อปีได้โดยไม่กระทบรอบเก่า
// เช่น ScorePoint=5 อาจอธิบายว่า "ดีเยี่ยม" ปีนี้ แต่ปีหน้าอาจเปลี่ยนคำก็ได้
[Table("Perf_RatingScaleDescription")]
public class Perf_RatingScaleDescription
{
    [Key]
    public long Id { get; set; }

    public long EvaluationPeriodId { get; set; }

    // Optional: scope this scale anchor to one evaluation TYPE, so different
    // groups can run different scales in the SAME period (AutoX #2). Null = the
    // period-wide default scale (backward compatible with existing rows).
    public long? EvaluationTypeId { get; set; }

    public int ScorePoint { get; set; }

    [Required, StringLength(300)]
    public string Description { get; set; } = null!;

    public bool IsActive { get; set; } = true;

    public virtual Perf_EvaluationPeriod EvaluationPeriod { get; set; } = null!;
}
