using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// ช่วงคะแนน -> เกรด -> % ขึ้นเงินเดือน/โบนัส (legacy: EvalGrade/SetGrade —
// "เปอร์เซ็นการขึ้นเงินเดือน"/"เปอร์เซ็นการขึ้นโบนัส" ต่อเกรด) ผูกกับ
// EvaluationPeriod เพราะเกณฑ์เกรด/สัดส่วนอาจเปลี่ยนทุกปีตามนโยบายบริษัท
[Table("Perf_GradeBand")]
public class Perf_GradeBand
{
    [Key]
    public long Id { get; set; }

    public long EvaluationPeriodId { get; set; }

    [Required, StringLength(10)]
    public string Grade { get; set; } = null!; // เช่น A+, A, B, C, D

    [Column(TypeName = "decimal(5,2)")]
    public decimal MinPercent { get; set; }
    [Column(TypeName = "decimal(5,2)")]
    public decimal MaxPercent { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal SalaryIncreasePercent { get; set; }
    [Column(TypeName = "decimal(5,2)")]
    public decimal BonusPercent { get; set; }

    // Bell-curve / forced-distribution TARGET for this grade (AutoX #4; legacy
    // evalgrade.distribution) — the % of people that SHOULD land in this grade,
    // e.g. A=15, B=75, C-D=10. Null = no target set. Calibration compares actual
    // vs this to recommend adjustments to supervisors, from a dept up to company.
    [Column(TypeName = "decimal(5,2)")]
    public decimal? TargetDistributionPercent { get; set; }

    // จาก EvalSumEdit.jsp เดิม: เกรดสุดโต่ง (ต่ำมาก/สูงมาก) บังคับให้ผู้ประเมิน
    // ชี้แจงคุณภาพ/ปริมาณงาน/ประสิทธิภาพเป็นลายลักษณ์อักษร กันคะแนนเฟ้อ/กดคะแนน
    // ตามอำเภอใจ — ตั้งค่า true ให้เฉพาะแบนด์ปลายบนสุด/ล่างสุดตามนโยบายบริษัท
    public bool RequiresJustification { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual Perf_EvaluationPeriod EvaluationPeriod { get; set; } = null!;
}
