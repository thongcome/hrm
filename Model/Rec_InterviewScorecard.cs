using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// One scorecard per (interview, panelist) — the interviewer's overall
// verdict. Per-competency ratings live on Rec_ScorecardCompetencyRating.
[Table("Rec_InterviewScorecard")]
public class Rec_InterviewScorecard
{
    [Key]
    public long Id { get; set; }

    public long InterviewId { get; set; }
    public long HremployeeId { get; set; }

    public InterviewRecommendation? OverallRecommendation { get; set; }

    [StringLength(2000)]
    public string? Strengths { get; set; }
    [StringLength(2000)]
    public string? Concerns { get; set; }

    public DateTime? SubmittedDate { get; set; }
}
