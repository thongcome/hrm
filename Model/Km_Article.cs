using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// ProjectId is a soft-link to Att_Project.Id — set only when this article is
// a "lesson learned" tied to a specific project; null for general knowledge
// articles. Tags is deliberately a free-text comma-separated string (same
// precedent as Eng_Recognition.CoreValueTag) rather than a separate tag
// master table — see plan's "จงใจไม่ทำในรอบนี้" section.
[Table("Km_Article")]
public class Km_Article
{
    [Key]
    public long Id { get; set; }

    // Stable human-facing code for this record (audit: every master/document table needs one beyond the surrogate Id).
    [StringLength(30)]
    public string? Code { get; set; }

    [Required, StringLength(6)]
    public string CompanyId { get; set; } = null!;

    public long? CategoryId { get; set; }

    [Required, StringLength(250)]
    public string Title { get; set; } = null!;

    [Column(TypeName = "nvarchar(max)")]
    public string Content { get; set; } = null!;

    [StringLength(500)]
    public string? Tags { get; set; }

    public ArticleStatus Status { get; set; } = ArticleStatus.Draft;

    public long? ProjectId { get; set; }

    public long AuthorHremployeeId { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime? ModifiedDate { get; set; }

    public int ViewCount { get; set; }
}
