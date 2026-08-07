using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// An organizational-change effort HR/leadership is tracking (restructuring,
// system rollout, merger integration, etc.) — status + milestone checklist,
// deliberately lightweight (no workflow approval; this is a tracking tool,
// not a document that needs sign-off).
[Table("OrgDev_ChangeInitiative")]
public class OrgDev_ChangeInitiative
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(6)]
    public string CompanyId { get; set; } = null!;

    [Required, StringLength(250)]
    public string Title { get; set; } = null!;

    [Column(TypeName = "nvarchar(max)")]
    public string? Description { get; set; }

    public ChangeInitiativeStatus Status { get; set; } = ChangeInitiativeStatus.Planned;

    public long SponsorUserId { get; set; }
    public long? ImpactedOrganizationId { get; set; }

    public DateOnly? StartDate { get; set; }
    public DateOnly? TargetCompletionDate { get; set; }

    [StringLength(1000)]
    public string? Note { get; set; }

    public long CreatedByUserId { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;
}
