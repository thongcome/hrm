using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// One candidate's application to one posting — the row the pipeline stage
// (Applied -> Screening -> Interview -> Offer -> Hired/Rejected/Withdrawn)
// lives on.
[Table("Rec_Application")]
public class Rec_Application
{
    [Key]
    public long Id { get; set; }

    // Stable human-facing code for this record (audit: every master/document table needs one beyond the surrogate Id).
    [StringLength(30)]
    public string? ApplicationCode { get; set; }

    public long CandidateId { get; set; }
    public long JobPostingId { get; set; }

    public ApplicationStage Stage { get; set; } = ApplicationStage.Applied;

    public DateTime AppliedDate { get; set; } = DateTime.Now;

    [StringLength(1000)]
    public string? RejectedReason { get; set; }
    public DateTime? RejectedDate { get; set; }

    public long? LastUpdatedByUserId { get; set; }
}
