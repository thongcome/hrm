using System.ComponentModel.DataAnnotations;

namespace HRM.Models;

// A leadership-development plan for one employee — typically someone from
// Talent_PoolEntry, but not required to be (LeadershipDevelopmentAdmin.razor
// suggests Talent Pool members, doesn't enforce it). TargetPosExecTypeId is
// optional (a plan can exist without a specific target role in mind yet).
public class OrgDev_LeadershipPlan
{
    [Key]
    public long Id { get; set; }

    public long HremployeeId { get; set; }
    public long? TargetPosExecTypeId { get; set; }

    public LeadershipPlanStatus Status { get; set; } = LeadershipPlanStatus.NotStarted;

    public DateOnly? StartDate { get; set; }
    public DateOnly? TargetDate { get; set; }

    [StringLength(1000)]
    public string? Note { get; set; }

    public long CreatedByUserId { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;
}
