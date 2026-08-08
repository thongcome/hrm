using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

[Table("Km_ArticleComment")]
public class Km_ArticleComment
{
    [Key]
    public long Id { get; set; }

    public long ArticleId { get; set; }
    public long HremployeeId { get; set; }

    [Required, Column(TypeName = "nvarchar(max)")]
    public string Comment { get; set; } = null!;

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    // Soft-delete flag — comments are never hard-deleted, matching the
    // soft-delete convention used everywhere else in this codebase.
    public bool IsActive { get; set; } = true;
}
