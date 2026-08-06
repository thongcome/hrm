using System.ComponentModel.DataAnnotations;

namespace HRM.Models;

// Join row: "this Job (Pos_ExecType) requires this Competency at this level."
// PosExecTypeId is a soft link (matches Pos_ExecType's own EmployeeTypeId
// convention — no [ForeignKey]/nav property). RequiredLevel must match an
// existing Comp_ProficiencyLevel.Level for the same CompetencyId — checked
// in the service layer, not a DB constraint (same convention as weight-sum
// checks elsewhere in this codebase).
public class Job_CompetencyRequirement
{
    [Key]
    public long Id { get; set; }

    public long PosExecTypeId { get; set; }
    public long CompetencyId { get; set; }

    [Range(1, 5)]
    public int RequiredLevel { get; set; }

    // Must-have vs nice-to-have — a standard distinction in real job profiles.
    public bool IsCritical { get; set; }

    public int SortOrder { get; set; }

    public virtual Comp_Competency Competency { get; set; } = null!;
}
