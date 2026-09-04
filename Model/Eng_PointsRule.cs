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

    public EngPointsSource Source { get; set; }

    public int Points { get; set; }

    [StringLength(200)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}
