using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

// Behavioral-anchor description of what a given 1-5 level looks like for
// ONE specific competency (BARS — Behaviorally Anchored Rating Scale) —
// deliberately per-competency, not one system-wide description of "level 3"
// in general, because what "level 3 Financial Analysis" looks like and what
// "level 3 Strategic Thinking" looks like are not comparable. This is what
// makes the required-level number on a Job Profile mean something concrete
// instead of being an arbitrary digit.
//
// Effective-dated (CEO-standard master-data pattern this codebase already
// uses for Pay_MinimumWage/Att_OtPolicy/etc — see Model/Pay_MinimumWage.cs):
// when HR revises what "level 3" means for a competency, that is a NEW row
// with today's EffectiveFrom, never an edit of the old row's Description —
// an evaluation scored last year should still show the anchor text the
// rater actually saw, not today's rewritten wording. "Current" is always
// resolved by query (MAX(EffectiveFrom) <= asOf among IsActive rows), the
// same idiom as every other effective-dated table — no stored IsCurrent
// flag. See Services/Shared/ProficiencyLevelResolver.cs for the query.
[Table("Comp_ProficiencyLevel")]
[Index(nameof(CompetencyId), nameof(Level), nameof(IsActive))]
public class Comp_ProficiencyLevel
{
    [Key]
    public long Id { get; set; }

    public long CompetencyId { get; set; }

    [Range(1, 5)]
    public int Level { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string Description { get; set; } = null!;

    public DateOnly EffectiveFrom { get; set; }

    public DateOnly? EffectiveTo { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual Comp_Competency Competency { get; set; } = null!;
}
