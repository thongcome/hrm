using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// A published listing, created from an approved Rec_Requisition. Title is
// independent of Pos_ExecType.Name — the public-facing job title often
// differs from the internal role name.
[Table("Rec_JobPosting")]
public class Rec_JobPosting
{
    [Key]
    public long Id { get; set; }

    // Stable human-facing code for this record (audit: every master/document table needs one beyond the surrogate Id).
    [StringLength(30)]
    public string? PostingCode { get; set; }

    public long RequisitionId { get; set; }

    [Required, StringLength(300)]
    public string Title { get; set; } = null!;

    [Column(TypeName = "nvarchar(max)")]
    public string? Description { get; set; }

    public PostingStatus Status { get; set; } = PostingStatus.Draft;

    public DateOnly? OpenDate { get; set; }
    public DateOnly? CloseDate { get; set; }

    public long CreatedByUserId { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    // Career Management: internal-only postings never appear on the public
    // /careers site — only on the authenticated /career/internal-jobs ESS
    // page — and are applied to via RecApplicationService.ApplyInternalAsync
    // instead of the public apply form.
    public bool IsInternal { get; set; }
}
