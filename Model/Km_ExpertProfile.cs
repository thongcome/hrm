using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Self-declared expertise only in this round — no endorsement/rating by
// others (see plan's "จงใจไม่ทำในรอบนี้"). CompetencyId is a soft-link to
// Comp_Competency.Id, same convention as Job_CompetencyRequirement.
[Table("Km_ExpertProfile")]
public class Km_ExpertProfile
{
    [Key]
    public long Id { get; set; }

    public long HremployeeId { get; set; }
    public long CompetencyId { get; set; }

    [StringLength(500)]
    public string? ProficiencyNote { get; set; }

    public bool IsActive { get; set; } = true;
}
