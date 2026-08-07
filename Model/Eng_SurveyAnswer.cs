using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// One answer to one campaign question within an anonymous response session.
// Which value column is populated depends on the parent
// Eng_CampaignQuestion.QuestionType.
[Table("Eng_SurveyAnswer")]
public class Eng_SurveyAnswer
{
    public long Id { get; set; }

    public long ResponseId { get; set; }
    public long CampaignQuestionId { get; set; }

    public int? RatingValue { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? TextValue { get; set; }

    [StringLength(200)]
    public string? ChoiceValue { get; set; }

    public bool? YesNoValue { get; set; }

    [ForeignKey(nameof(ResponseId))]
    public virtual Eng_SurveyResponse Response { get; set; } = null!;

    [ForeignKey(nameof(CampaignQuestionId))]
    public virtual Eng_CampaignQuestion CampaignQuestion { get; set; } = null!;
}
