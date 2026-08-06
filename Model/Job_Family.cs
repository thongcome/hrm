using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Job Family — a group of related jobs (e.g. "Finance", "Engineering") that
// Pos_ExecType (the reusable Job/title concept) attaches to. Company-scoped
// like every other Pos_* master table.
[Table("Job_Family")]
public class Job_Family
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

    [StringLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}
