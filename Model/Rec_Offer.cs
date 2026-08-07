using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

[Table("Rec_Offer")]
public class Rec_Offer
{
    [Key]
    public long Id { get; set; }

    public long ApplicationId { get; set; }

    // Copied from the requisition — the seat this offer, if accepted, will fill.
    public long TargetPositionSlotId { get; set; }

    [Column(TypeName = "decimal(15,2)")]
    public decimal OfferedSalary { get; set; }

    public DateOnly StartDate { get; set; }

    public OfferStatus Status { get; set; } = OfferStatus.Draft;

    public long? JobMasterId { get; set; }

    public DateTime? SentDate { get; set; }
    public DateTime? RespondedDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }

    // Filled in only after HireAsync succeeds — lets callers trace an offer
    // forward to the Hremployee it produced.
    public long? HiredHremployeeId { get; set; }

    public long CreatedByUserId { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;
}
