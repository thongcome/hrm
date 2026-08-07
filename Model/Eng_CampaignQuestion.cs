using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Snapshotted from Eng_QuestionTemplate at campaign-launch time so later
// template edits never retroactively change a live/closed campaign's
// wording — same snapshot-on-launch pattern as Hrd_LifecycleTaskInstance.Title.
[Table("Eng_CampaignQuestion")]
public class Eng_CampaignQuestion
{
    [Key]
    public long Id { get; set; }

    public long CampaignId { get; set; }

    // Soft-link -> Eng_QuestionTemplate.Id. Null for ad-hoc questions added
    // directly to this campaign without coming from the shared bank.
    public long? SourceTemplateId { get; set; }

    [Required, StringLength(500)]
    public string Text { get; set; } = null!;

    public Eng_QuestionType QuestionType { get; set; }

    [StringLength(1000)]
    public string? Options { get; set; }

    public int SortOrder { get; set; }

    [ForeignKey(nameof(CampaignId))]
    public virtual Eng_SurveyCampaign Campaign { get; set; } = null!;
}
