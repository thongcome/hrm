using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

[Table("Rec_Offer")]
public class Rec_Offer
{
    [Key]
    public long Id { get; set; }

    // Stable human-facing code for this record (audit: every master/document table needs one beyond the surrogate Id).
    [StringLength(30)]
    public string? OfferCode { get; set; }

    public long ApplicationId { get; set; }

    // Copied from the requisition — the seat this offer, if accepted, will fill.
    public long TargetPositionSlotId { get; set; }

    [Column(TypeName = "decimal(15,2)")]
    public decimal OfferedSalary { get; set; }

    public DateOnly StartDate { get; set; }

    public OfferStatus Status { get; set; } = OfferStatus.Draft;

    public long? JobMasterId { get; set; }

    // Second, separate approval gate — starts only once the candidate has
    // accepted (Status -> PendingHireApproval), gates ConfirmHireAsync (the
    // step that actually creates the Hremployee row). Distinct from
    // JobMasterId above, which only ever gated the offer's own terms.
    public long? HireJobMasterId { get; set; }

    public DateTime? SentDate { get; set; }
    public DateTime? RespondedDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }

    // Filled in only after HireAsync succeeds — lets callers trace an offer
    // forward to the Hremployee it produced.
    public long? HiredHremployeeId { get; set; }

    public long CreatedByUserId { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;
}
