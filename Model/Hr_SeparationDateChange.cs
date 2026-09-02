using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

// History of "postpone / bring forward the separation effective date" requests
// for an already-approved Hr_SeparationRequest. Owner rule (2026-09-03): moving
// a resignation date is NOT a silent edit — each change is recorded here and
// routed through the same approval workflow (see
// Services/Hr/SeparationRequestService.RescheduleAsync). The live
// Hr_SeparationRequest.EffectiveDate / Hremployee.ResignDate only move once the
// change's job closes Approved (SyncDateChangeStatusFromJobAsync); every attempt
// (approved or rejected) stays here as the audit trail of how the date shifted.
[Table("Hr_SeparationDateChange")]
[Index(nameof(SeparationRequestId), nameof(Status))]
[Index(nameof(HremployeeId), nameof(Status))]
[Index(nameof(JobMasterId))]
public class Hr_SeparationDateChange
{
    [Key]
    public long Id { get; set; }

    public long SeparationRequestId { get; set; }

    public long HremployeeId { get; set; }

    [StringLength(20)]
    public string? EmpNo { get; set; } // snapshot at request time

    [StringLength(6)]
    public string CompanyId { get; set; } = null!;

    // The effective date this change moves FROM / TO.
    public DateTime OldEffectiveDate { get; set; }
    public DateTime NewEffectiveDate { get; set; }

    [StringLength(1000)]
    public string? Reason { get; set; }

    public SeparationRequestStatus Status { get; set; } = SeparationRequestStatus.PendingApproval;

    // Soft link to job_master.jobmasterid — null only between inserting this row
    // and StartJobAsync succeeding (same window/guarantee as Hr_SeparationRequest).
    public long? JobMasterId { get; set; }

    public long RequestedByUserId { get; set; }
    public DateTime RequestedDate { get; set; } = DateTime.Now;

    [ForeignKey(nameof(SeparationRequestId))]
    public virtual Hr_SeparationRequest SeparationRequest { get; set; } = null!;
}
