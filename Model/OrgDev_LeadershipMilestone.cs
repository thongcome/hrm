using System.ComponentModel.DataAnnotations;

namespace HRM.Models;

// A checklist item within an OrgDev_LeadershipPlan — mirrors
// Hrd_LifecycleTaskInstance's shape (Title snapshot, Status, CompletedDate).
public class OrgDev_LeadershipMilestone
{
    [Key]
    public long Id { get; set; }

    public long PlanId { get; set; }

    [Required, StringLength(250)]
    public string Title { get; set; } = null!;

    public DateOnly? TargetDate { get; set; }
    public OrgDevMilestoneStatus Status { get; set; } = OrgDevMilestoneStatus.Pending;
    public DateTime? CompletedDate { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }
}
