using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

// คะแนนต่อ 1 ตัวชี้วัด ที่ผู้ประเมิน 1 คนให้ (legacy: EvalScore)
[Table("Perf_Score")]
[Index(nameof(RaterAssignmentId), nameof(IndicatorId), IsUnique = true)]
public class Perf_Score
{
    [Key]
    public long Id { get; set; }

    public long RaterAssignmentId { get; set; }
    public long IndicatorId { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal ScorePoint { get; set; }

    [StringLength(1000)]
    public string? Comment { get; set; }

    public virtual Perf_RaterAssignment RaterAssignment { get; set; } = null!;
    public virtual Perf_Indicator Indicator { get; set; } = null!;
}
