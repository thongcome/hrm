using System.ComponentModel.DataAnnotations;

namespace HRM.Models;

// A checklist item within an OrgDev_ChangeInitiative — same shape as
// OrgDev_LeadershipMilestone, kept as a separate table (not shared) because
// the two milestone lists belong to unrelated parent entities and this
// codebase's convention is one child table per parent, not a polymorphic
// shared milestone table.
public class OrgDev_ChangeMilestone
{
    [Key]
    public long Id { get; set; }

    public long InitiativeId { get; set; }

    [Required, StringLength(250)]
    public string Title { get; set; } = null!;

    public DateOnly? TargetDate { get; set; }
    public OrgDevMilestoneStatus Status { get; set; } = OrgDevMilestoneStatus.Pending;
    public DateTime? CompletedDate { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }
}
