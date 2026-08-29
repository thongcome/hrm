using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// รอบการประเมิน (legacy: evalperiod + evalsubperiodtype ยุบรวมเป็น PeriodType
// เดียว) — เป็นรากของ config ทั้งชุด (Topic/SubTopic/Indicator/GradeBand/
// RatingScaleDescription ผูกกับรอบนี้โดยตรงหรือโดยอ้อมผ่าน EvaluationType)
[Table("Perf_EvaluationPeriod")]
public class Perf_EvaluationPeriod
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

    public PerfPeriodType PeriodType { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateOnly? ScoreDueDate { get; set; }

    public bool IsActive { get; set; } = true;

    // ล็อกรอบแล้ว — ห้ามแก้ config (Topic/SubTopic/Indicator/GradeBand) อีก
    // เพื่อไม่ให้กระทบผลประเมินที่ทำไปแล้วบางส่วน (เทียบเท่า legacy year-lock)
    public bool IsLocked { get; set; }
}
