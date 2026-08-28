using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

// Workflow-gated request to end an employee's employment — routes through
// WorkflowEngineService for approval before Hremployee.ResignDate/
// SeparationType are ever written. Mirrors Services/Org/OrgChangeRequestService's
// submit -> job -> lazy-apply-on-read pattern (see
// Services/Hr/SeparationRequestService.cs), simplified: no scheduler exists
// anywhere in this app, and unlike an org-chart change there's no future
// "effective date" gate to wait on separately from approval — once the job
// closes Approved, the request applies immediately.
[Table("Hr_SeparationRequest")]
[Index(nameof(HremployeeId), nameof(Status))]
[Index(nameof(JobMasterId))]
public class Hr_SeparationRequest
{
    [Key]
    public long Id { get; set; }

    public long HremployeeId { get; set; }

    [StringLength(20)]
    public string? EmpNo { get; set; } // snapshot at request time

    [StringLength(6)]
    public string CompanyId { get; set; } = null!;

    public SeparationType SeparationType { get; set; }

    // Proposed Hremployee.ResignDate — written for real only once approved.
    public DateTime EffectiveDate { get; set; }

    [StringLength(1000)]
    public string? Reason { get; set; }

    public SeparationRequestStatus Status { get; set; } = SeparationRequestStatus.PendingApproval;

    // Soft link to job_master.jobmasterid — null only in the brief window
    // between inserting this row and StartJobAsync succeeding. If
    // StartJobAsync throws, SubmitAsync's whole operation fails and nothing
    // is left half-committed (see SeparationRequestService.SubmitAsync).
    public long? JobMasterId { get; set; }

    public long RequestedByUserId { get; set; }
    public DateTime RequestedDate { get; set; } = DateTime.Now;

    [ForeignKey(nameof(HremployeeId))]
    public virtual Hremployee Hremployee { get; set; } = null!;
}
