using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Career level within one of the two ladders (e.g. IC1-IC6, M1-M5). A
// deliberately separate concept from Pay_SalaryGrade (compensation band) —
// companies often correlate the two but don't auto-link them; that's a
// compensation-review decision, not something this module assumes.
[Table("Job_Level")]
public class Job_Level
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(6)]
    public string CompanyId { get; set; } = null!;

    public CareerTrack CareerTrack { get; set; }

    // Used to order/compare levels within (and, loosely, across) tracks —
    // e.g. IC1..IC6 = 1..6, M1..M5 = 1..5.
    public int LevelNumber { get; set; }

    [StringLength(20)]
    public string? Code { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = null!;
    [StringLength(100)]
    public string? NameEn { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}
