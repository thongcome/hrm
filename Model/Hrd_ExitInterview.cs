using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Not unique per employee — an employee could be rehired and resign again.
// Callers needing "the" exit interview should order by CreatedDate desc.
[Table("Hrd_ExitInterview")]
public class Hrd_ExitInterview
{
    [Key]
    public long Id { get; set; }

    public long HremployeeId { get; set; }

    public DateTime InterviewDate { get; set; }
    public long ConductedByUserId { get; set; }

    public ExitReasonCode ReasonCode { get; set; }

    [StringLength(500)]
    public string? ReasonNote { get; set; }

    public bool? WouldRecommendCompany { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? Feedback { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public long CreatedByUserId { get; set; }
}
