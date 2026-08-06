using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Competency category — one of the 3 standard groupings (Core/Leadership/
// Functional). Prefixed Comp_ (not Com_, which is already taken by
// company/org master data like Com_ChartOfAccount/Com_SectionType) to avoid
// visual confusion between "Company" and "Competency" tables.
[Table("Comp_Category")]
public class Comp_Category
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(6)]
    public string CompanyId { get; set; } = null!;

    [StringLength(20)]
    public string? Code { get; set; }

    [Required, StringLength(200)]
    public string Name { get; set; } = null!;
    [StringLength(200)]
    public string? NameEn { get; set; }

    public CompetencyCategoryType CategoryType { get; set; }

    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
