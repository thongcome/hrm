using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Reusable question bank a campaign draws from. Campaign questions are
// snapshotted from these at launch time (Eng_CampaignQuestion) so later edits
// here never retroactively change a live/closed campaign.
[Table("Eng_QuestionTemplate")]
public class Eng_QuestionTemplate
{
    [Key]
    public long Id { get; set; }

    // Stable human-facing code for this record (audit: every master/document table needs one beyond the surrogate Id).
    [StringLength(30)]
    public string? Code { get; set; }

    [Required, StringLength(6)]
    public string CompanyId { get; set; } = null!;

    [Required, StringLength(500)]
    public string Text { get; set; } = null!;

    public Eng_QuestionType QuestionType { get; set; } = Eng_QuestionType.Rating;

    // CSV of choice labels, only meaningful when QuestionType = MultipleChoice
    [StringLength(1000)]
    public string? Options { get; set; }

    public bool IsActive { get; set; } = true;
}
