using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// One row per (employee, evaluation period) — mutable/upserted by
// TalentGridService, not an append-only history log (HRMContext.Audit.cs
// already captures every change automatically). Tied to the existing
// Perf_EvaluationPeriod rather than a new "talent review cycle" concept, so
// the 9-box grid's Performance axis (from Perf_EvaluationInstance) and
// Potential axis (this table) always line up on the same period.
[Table("Talent_PotentialRating")]
public class Talent_PotentialRating
{
    [Key]
    public long Id { get; set; }

    public long HremployeeId { get; set; }

    public long EvaluationPeriodId { get; set; }

    public PotentialLevel Level { get; set; }

    public long RatedByUserId { get; set; }

    public DateTime RatedDate { get; set; } = DateTime.Now;

    [StringLength(500)]
    public string? Note { get; set; }
}
