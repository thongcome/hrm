using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Who is invited to a given campaign. Mirrors Info_MessageTarget's shape
// (kept as its own table per this codebase's convention of not sharing
// target tables across modules). A campaign with zero target rows is
// treated as "everyone" — same deliberate fallback as InfoMessageService.
[Table("Eng_CampaignTarget")]
public class Eng_CampaignTarget
{
    public long Id { get; set; }

    public long CampaignId { get; set; }

    public Eng_TargetType TargetType { get; set; }

    // Soft-link -> com_organization.id. Matching includes the org's subtree
    // (Hremployee.orgcodefull starting with the target org's orgcodefull).
    public long? TargetOrganizationId { get; set; }

    // Soft-link -> Hremployee.id
    public long? TargetHremployeeId { get; set; }

    [NotMapped]
    public string? TargetOrgCache { get; set; }

    [NotMapped]
    public string? TargetEmpCache { get; set; }

    [ForeignKey(nameof(CampaignId))]
    public virtual Eng_SurveyCampaign Campaign { get; set; } = null!;
}
