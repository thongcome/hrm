using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// One SMART improvement objective within a Perf_ImprovementPlan — mirrors
// Idp_DevelopmentAction's shape (title + free-text detail + status), but
// SuccessCriteria replaces Idp's CompetencyId link since a PIP goal is
// judged against a written bar the manager and employee agreed on, not a
// competency-library rating.
[Table("Perf_ImprovementGoal")]
public class Perf_ImprovementGoal
{
    [Key]
    public long Id { get; set; }

    public long PlanId { get; set; }

    [Required, StringLength(300)]
    public string Title { get; set; } = null!;

    [StringLength(1000)]
    public string? SuccessCriteria { get; set; }

    public PipGoalStatus Status { get; set; } = PipGoalStatus.NotStarted;

    public int SortOrder { get; set; }

    public virtual Perf_ImprovementPlan Plan { get; set; } = null!;
}
