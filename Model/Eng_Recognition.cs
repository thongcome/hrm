using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Peer-to-peer kudos/praise — unlike survey responses, recognition is
// intentionally attributed (giver and receiver are both visible), so it
// carries real employee foreign keys rather than being anonymous.
[Table("Eng_Recognition")]
public class Eng_Recognition
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(6)]
    public string CompanyId { get; set; } = null!;

    public long FromHremployeeId { get; set; }
    public long ToHremployeeId { get; set; }

    [Required, StringLength(1000)]
    public string Message { get; set; } = null!;

    // Free-text tag (e.g. "Teamwork", "Innovation") — no separate master
    // table in v1, kept lightweight like other free-text tags in this codebase.
    [StringLength(50)]
    public string? CoreValueTag { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    // Soft-hide (e.g. reported as inappropriate) rather than hard delete.
    public bool IsActive { get; set; } = true;
}
