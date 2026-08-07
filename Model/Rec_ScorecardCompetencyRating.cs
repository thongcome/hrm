using System.ComponentModel.DataAnnotations;

namespace HRM.Models;

// Detail row per competency on a scorecard — mirrors
// Idp_CompetencyAssessment's shape (CompetencyId + 1-5 RatedLevel + Note).
public class Rec_ScorecardCompetencyRating
{
    [Key]
    public long Id { get; set; }

    public long ScorecardId { get; set; }
    public long CompetencyId { get; set; }

    [Range(1, 5)]
    public int RatedLevel { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }
}
