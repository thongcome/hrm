using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// A checklist item within an Eng_ActionPlan — same shape as
// OrgDev_ChangeMilestone, kept as its own table per this codebase's
// convention of one child table per parent.
public class Eng_ActionPlanMilestone
{
    [Key]
    public long Id { get; set; }

    public long ActionPlanId { get; set; }

    [Required, StringLength(250)]
    public string Title { get; set; } = null!;

    public DateOnly? TargetDate { get; set; }
    public Eng_MilestoneStatus Status { get; set; } = Eng_MilestoneStatus.Pending;
    public DateTime? CompletedDate { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    [ForeignKey(nameof(ActionPlanId))]
    public virtual Eng_ActionPlan ActionPlan { get; set; } = null!;
}
