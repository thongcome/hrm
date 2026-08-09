using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// One row per instance HR actually adjusted during a calibration session —
// not every instance in the session gets a row, only overridden ones.
// Applied immediately on creation (unlike PerfMeritService's separate
// preview/apply — this only rewrites Perf_EvaluationInstance's own score
// fields, which is easy to reason about/reverse, not an external payroll
// write). Keeps the original values for audit ("why was this changed").
[Table("Perf_CalibrationAdjustment")]
public class Perf_CalibrationAdjustment
{
    [Key]
    public long Id { get; set; }

    public long SessionId { get; set; }
    public long InstanceId { get; set; }

    [Column(TypeName = "decimal(6,2)")]
    public decimal? OriginalScorePercent { get; set; }
    [StringLength(10)]
    public string? OriginalGrade { get; set; }

    [Column(TypeName = "decimal(6,2)")]
    public decimal AdjustedScorePercent { get; set; }
    [StringLength(10)]
    public string? AdjustedGrade { get; set; }

    [Required, StringLength(1000)]
    public string Justification { get; set; } = null!;

    public long AdjustedByUserId { get; set; }
    public DateTime AdjustedDate { get; set; } = DateTime.Now;

    public virtual Perf_CalibrationSession Session { get; set; } = null!;
}
