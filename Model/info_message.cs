using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Legacy JSP-era table (originally scaffolded with 0 rows referencing it) — Id
// is a real IDENTITY column (verified via sys.columns.is_identity), so the
// normal entity.Id==0 -> Add pattern works; no NextIdAsync needed.
[Table("info_message")]
public class info_message
{
    [Key]
    public long Id { get; set; }

    // Stable human-facing code for this record (audit: every master/document table needs one beyond the surrogate Id).
    [StringLength(20)]
    public string? code { get; set; }

    [StringLength(200)]
    public string? subject { get; set; }

    public DateOnly? startdate { get; set; }

    public DateOnly? enddate { get; set; }

    public bool? isactive { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? message { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [StringLength(6)]
    public string CompanyId { get; set; } = null!;

    public InfoMessagePriority PriorityLevel { get; set; } = InfoMessagePriority.Normal;

    // HR's editorial choice to feature this on the home-page widget —
    // deliberately separate from PriorityLevel (priority tells the reader
    // how urgent it is; pinned is HR choosing what to spotlight).
    public bool IsPinned { get; set; }

    public long CreatedByUserId { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public long? ModifiedByUserId { get; set; }
}
