using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// One scheduled round of a course. Location is used for Classroom,
// OnlineLink for Online — a Hybrid course may set both. ActualCost is
// filled in by HR after the round wraps up, compared against
// Lms_TrainingBudget via LmsTrainingBudgetService.
[Table("Lms_CourseSession")]
public class Lms_CourseSession
{
    [Key]
    public long Id { get; set; }

    public long CourseId { get; set; }

    [Required, StringLength(50)]
    public string SessionCode { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    [StringLength(250)]
    public string? Location { get; set; }

    [StringLength(500)]
    public string? OnlineLink { get; set; }

    public int? MaxSeats { get; set; }

    [Column(TypeName = "decimal(15,2)")]
    public decimal? ActualCost { get; set; }

    public CourseSessionStatus Status { get; set; } = CourseSessionStatus.Scheduled;
}
