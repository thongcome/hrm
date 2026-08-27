using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// The claim itself is still self-declared, but Km_ExpertEndorsement now lets
// colleagues vouch for it — see that file. CompetencyId is a soft-link to
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
