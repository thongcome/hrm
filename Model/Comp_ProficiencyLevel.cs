using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Behavioral-anchor description of what a given 1-5 level looks like for
// ONE specific competency (BARS — Behaviorally Anchored Rating Scale) —
// deliberately per-competency, not one system-wide description of "level 3"
// in general, because what "level 3 Financial Analysis" looks like and what
// "level 3 Strategic Thinking" looks like are not comparable. This is what
// makes the required-level number on a Job Profile mean something concrete
// instead of being an arbitrary digit.
[Table("Comp_ProficiencyLevel")]
public class Comp_ProficiencyLevel
{
    [Key]
    public long Id { get; set; }

    public long CompetencyId { get; set; }

    [Range(1, 5)]
    public int Level { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string Description { get; set; } = null!;

    public virtual Comp_Competency Competency { get; set; } = null!;
}
