using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// A single competency (e.g. "Financial Analysis", "Strategic Thinking") —
// belongs to one Comp_Category, reusable across many Job Profiles via
// Job_CompetencyRequirement.
[Table("Comp_Competency")]
public class Comp_Competency
{
    [Key]
    public long Id { get; set; }

    public long CategoryId { get; set; }

    [Required, StringLength(20)]
    public string Code { get; set; } = null!;
    [Required, StringLength(300)]
    public string Name { get; set; } = null!;
    [StringLength(300)]
    public string? NameEn { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual Comp_Category Category { get; set; } = null!;
}
