using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// One row per employee per calendar week (Mon-Sun). Entries accumulate while
// Draft; submitting snapshots TotalHours and routes through the generic
// Workflow Engine (TIMESHEET_APPROVAL) exactly like Idp_Plan/Perf_EvaluationInstance
// do — SyncStatusFromJobAsync is the only place Status ever changes after
// submission, called lazily on read (no scheduler exists in this codebase).
// Deliberately not wired into payroll cost allocation this round — that's a
// separate decision requiring Att_Project<->Com_ChartOfAccount linkage design.
[Table("Att_TimesheetSubmission")]
public class Att_TimesheetSubmission
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(50)]
    public string CompanyId { get; set; } = null!;

    public long HremployeeId { get; set; }

    public DateOnly WeekStartDate { get; set; }
    public DateOnly WeekEndDate { get; set; }

    [Column(TypeName = "decimal(6,2)")]
    public decimal TotalHours { get; set; }

    public Att_TimesheetStatus Status { get; set; } = Att_TimesheetStatus.Draft;

    public long? JobMasterId { get; set; }
    public DateTime? SubmittedDate { get; set; }
    public DateTime? ApprovedDate { get; set; }

    public long CreatedByUserId { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public virtual Hremployee Hremployee { get; set; } = null!;
    public virtual ICollection<Att_TimesheetEntry> Entries { get; set; } = new List<Att_TimesheetEntry>();
}
