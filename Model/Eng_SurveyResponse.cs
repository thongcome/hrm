using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// A single survey submission session. Deliberately carries NO employee
// foreign key anywhere — genuine anonymity, confirmed with the user
// (accepting the tradeoff that individual non-responders can't be tracked,
// only group-level participation via Eng_SurveyCampaign.ResponseCount).
// No IAuditLogger call is made for submissions either, for the same reason.
[Table("Eng_SurveyResponse")]
public class Eng_SurveyResponse
{
    public long Id { get; set; }

    public long CampaignId { get; set; }

    public DateTime SubmittedDate { get; set; } = DateTime.Now;

    // 0-10 promoter/detractor score, only set for CampaignType = ENPS.
    public int? NpsScore { get; set; }

    [ForeignKey(nameof(CampaignId))]
    public virtual Eng_SurveyCampaign Campaign { get; set; } = null!;

    public virtual ICollection<Eng_SurveyAnswer> Answers { get; set; } = new List<Eng_SurveyAnswer>();
}
