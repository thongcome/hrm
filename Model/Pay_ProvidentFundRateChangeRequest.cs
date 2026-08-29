using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// A rate-change request that must clear the Workflow Approval Engine before
// it ever becomes a real Pay_ProvidentFundElection row — either the
// employee submits this themselves (ESS, only when a
// Pay_ProvidentFundRateChangeWindow is currently open) or HR submits it on
// their behalf. RequestedCompanyRate starts as whatever
// Pay_ProvidentFundRateMatrixRule suggested (SuggestedCompanyRate,
// read-only for reference) but HR can override it before approval — the
// suggestion is a convenience, not a silent auto-decision.
[Table("Pay_ProvidentFundRateChangeRequest")]
public class Pay_ProvidentFundRateChangeRequest
{
    [Key]
    public long Id { get; set; }

    // Stable human-facing code for this record (audit: every master/document table needs one beyond the surrogate Id).
    [StringLength(30)]
    public string? RequestNo { get; set; }

    public long HremployeeId { get; set; }
    public long PolicyId { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal RequestedEmployeeRate { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? SuggestedCompanyRate { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal RequestedCompanyRate { get; set; }

    public long? WindowId { get; set; }

    public DateOnly RequestedEffectiveFrom { get; set; }

    public ProvidentFundRequestStatus Status { get; set; } = ProvidentFundRequestStatus.PendingApproval;

    public long? JobMasterId { get; set; }

    public long RequestedByUserId { get; set; }
    public DateTime RequestedDate { get; set; } = DateTime.Now;

    public bool IsEmployeeInitiated { get; set; }

    public virtual Hremployee Hremployee { get; set; } = null!;
    public virtual Pay_ProvidentFundPolicy Policy { get; set; } = null!;
}
