using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

public enum AttCorrectionStatus
{
    Pending = 0,    // submitted, workflow job running
    Approved = 1,   // job COMPLETED and punches applied
    Rejected = 2,
    Cancelled = 3,
}

// An employee's request to fix a day's attendance — forgot to punch, punched
// late because of a customer visit, machine failure — approved by the
// supervisor through the generic workflow engine (ATT_CORRECTION). On approval
// the requested in/out become ManualEntry punches and the day is re-aggregated,
// so payroll's late/absence deductions see the corrected facts. (Customer spec
// REQ-051 manual correction, REQ-052/143 correction approval.)
[Table("Att_CorrectionRequest")]
public class Att_CorrectionRequest
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Required, StringLength(6)]
    public string CompanyId { get; set; } = null!;

    public long HremployeeId { get; set; }

    [StringLength(6)]
    public string? EmpNo { get; set; }

    public DateOnly WorkDate { get; set; }

    // Either may be null: "I forgot to punch out" only needs RequestedOut.
    public DateTime? RequestedIn { get; set; }
    public DateTime? RequestedOut { get; set; }

    [Required, StringLength(500)]
    public string Reason { get; set; } = null!;

    public AttCorrectionStatus Status { get; set; } = AttCorrectionStatus.Pending;

    public long? JobMasterId { get; set; }

    // Set once the approved punches have been written — the apply step is
    // idempotent because status is only read back lazily on page load.
    public bool IsApplied { get; set; }
    public DateTime? AppliedDate { get; set; }

    public long RequestedByUserId { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;
}
