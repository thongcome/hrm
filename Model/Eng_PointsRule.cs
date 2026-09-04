using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

// Config: how many engagement points each activity source awards, per company.
// Config-first — HR turns a source on/off and sets its points; EngPointsService
// only credits sources that have an active rule. One row per (company, source).
[Table("Eng_PointsRule")]
[Index(nameof(CompanyId))]
public class Eng_PointsRule
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(20)]
    public string CompanyId { get; set; } = null!;

    // The pluggable activity this rule enrols (IPointEarningActivity.Code).
    // A rule = "this company gives N points for this activity". Name is a
    // snapshot of the provider's display name at enrol time.
    [Required, StringLength(40)]
    public string ActivityCode { get; set; } = null!;

    [StringLength(100)]
    public string? ActivityName { get; set; }

    public int Points { get; set; }

    [StringLength(200)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}
