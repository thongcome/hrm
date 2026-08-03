using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// ลักษณะของหน่วยงาน (nature-of-unit lookup) — ported from legacy PIS
// "subsectiontype" table. Simple flat lookup (e.g. คณะกรรมการ/Front/Back/Support),
// unlike Com_SectionType which is a hierarchical level classification.
[Table("Com_SubSectionType")]
public class Com_SubSectionType
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(6)]
    public string CompanyId { get; set; } = null!;

    [Required, StringLength(200)]
    public string Name { get; set; } = null!;

    [StringLength(1000)]
    public string? Remark { get; set; }

    public bool IsActive { get; set; } = true;
}
