using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Master list of projects employees can log timesheet hours against. Deliberately
// flat (code/name only) — no task breakdown, no budget/cost fields, and not
// linked to Com_ChartOfAccount cost centers in this round (see
// Att_TimesheetSubmission comment for why).
[Table("Att_Project")]
public class Att_Project
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(50)]
    public string CompanyId { get; set; } = null!;

    [Required, StringLength(30)]
    public string Code { get; set; } = null!;

    [Required, StringLength(200)]
    public string Name { get; set; } = null!;

    [StringLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}
