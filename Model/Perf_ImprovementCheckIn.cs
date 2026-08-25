using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Append-only progress log for an active Perf_ImprovementPlan — same
// append-only stance as Perf_GoalCheckIn/Okr_KeyResultCheckIn elsewhere in
// this codebase (no edit/delete; the history itself is the record).
[Table("Perf_ImprovementCheckIn")]
public class Perf_ImprovementCheckIn
{
    [Key]
    public long Id { get; set; }

    public long PlanId { get; set; }

    public DateOnly CheckInDate { get; set; }

    public PipCheckInRating Rating { get; set; }

    [Required, StringLength(2000)]
    public string Note { get; set; } = null!;

    public long RecordedByUserId { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public virtual Perf_ImprovementPlan Plan { get; set; } = null!;
}
