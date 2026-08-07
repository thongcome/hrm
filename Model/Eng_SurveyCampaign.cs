using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// A single survey/pulse/eNPS run. Responses are fully anonymous by design
// (see Eng_SurveyResponse) — InvitedCount/ResponseCount are the only
// participation signal available, tracked as aggregate snapshots rather than
// per-employee, so reminder campaigns can only be broadcast group-wide, never
// targeted at specific non-responders.
[Table("Eng_SurveyCampaign")]
public class Eng_SurveyCampaign
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(6)]
    public string CompanyId { get; set; } = null!;

    [Required, StringLength(250)]
    public string Title { get; set; } = null!;

    [Column(TypeName = "nvarchar(max)")]
    public string? Description { get; set; }

    public Eng_CampaignType CampaignType { get; set; } = Eng_CampaignType.Survey;
    public Eng_CampaignStatus Status { get; set; } = Eng_CampaignStatus.Draft;

    public DateOnly? OpenDate { get; set; }
    public DateOnly? CloseDate { get; set; }

    // Set when relaunched from a previous pulse campaign, so HR can trace a
    // recurring pulse series without the questions being shared/live-linked.
    public long? RelaunchedFromCampaignId { get; set; }

    public int InvitedCount { get; set; }
    public int ResponseCount { get; set; }

    public long CreatedByUserId { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public virtual ICollection<Eng_CampaignQuestion> Questions { get; set; } = new List<Eng_CampaignQuestion>();
    public virtual ICollection<Eng_CampaignTarget> Targets { get; set; } = new List<Eng_CampaignTarget>();
}
