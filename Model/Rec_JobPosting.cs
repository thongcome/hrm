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
}
