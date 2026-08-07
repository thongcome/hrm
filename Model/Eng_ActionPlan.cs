using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// A lightweight tracker for follow-up actions after a survey/pulse closes —
// same shape as OrgDev_ChangeInitiative (status + milestone checklist, no
// workflow approval; this is a tracking tool, not a document needing sign-off).
[Table("Eng_ActionPlan")]
public class Eng_ActionPlan
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(6)]
    public string CompanyId { get; set; } = null!;

    // Soft-link -> Eng_SurveyCampaign.Id. Null when HR tracks an engagement
    // action plan not tied to any specific survey result.
    public long? CampaignId { get; set; }

    [Required, StringLength(250)]
    public string Title { get; set; } = null!;

    [Column(TypeName = "nvarchar(max)")]
    public string? Description { get; set; }

    public Eng_ActionPlanStatus Status { get; set; } = Eng_ActionPlanStatus.Planned;

    public long OwnerUserId { get; set; }
    public long? ImpactedOrganizationId { get; set; }

    public DateOnly? StartDate { get; set; }
    public DateOnly? TargetCompletionDate { get; set; }

    [StringLength(1000)]
    public string? Note { get; set; }

    public long CreatedByUserId { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public virtual ICollection<Eng_ActionPlanMilestone> Milestones { get; set; } = new List<Eng_ActionPlanMilestone>();
}
