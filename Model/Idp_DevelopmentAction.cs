using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// One development action within an Idp_Plan. CompetencyId is nullable — an
// action may target a specific competency gap, or be a general
// career-development action (e.g. "attend general leadership training") not
// tied to any single competency.
[Table("Idp_DevelopmentAction")]
public class Idp_DevelopmentAction
{
    [Key]
    public long Id { get; set; }

    public long PlanId { get; set; }

    public long? CompetencyId { get; set; }

    [Required, StringLength(250)]
    public string Title { get; set; } = null!;

    [StringLength(1000)]
    public string? Description { get; set; }

    public DateOnly? TargetDate { get; set; }

    public IdpActionStatus Status { get; set; } = IdpActionStatus.NotStarted;

    // 70-20-10 classification. Nullable — rows created before this field
    // existed, and quick-created succession-gap actions (KeyPositionDetail),
    // have no method; "ยังไม่ระบุ" is a valid state, not an error.
    public IdpDevelopmentMethod? Method { get; set; }

    public DateTime? CompletedDate { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    public int SortOrder { get; set; }

    public virtual Idp_Plan Plan { get; set; } = null!;
    public virtual Comp_Competency? Competency { get; set; }
}
