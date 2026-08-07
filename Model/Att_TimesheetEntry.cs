using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// A single (project, date, hours) line within a weekly Att_TimesheetSubmission.
// Editable only while the parent submission is Draft — enforced in
// TimesheetService, not here.
[Table("Att_TimesheetEntry")]
public class Att_TimesheetEntry
{
    [Key]
    public long Id { get; set; }

    public long SubmissionId { get; set; }
    public long ProjectId { get; set; }

    public DateOnly WorkDate { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal Hours { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    public virtual Att_TimesheetSubmission Submission { get; set; } = null!;
    public virtual Att_Project Project { get; set; } = null!;
}
