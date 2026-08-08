using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// A course "template" — one course can open multiple Lms_CourseSession
// rounds. InstructorName is free text (not a user account) because
// instructors are frequently external contractors, not employees.
// PassingScorePercent == null means the course has no quiz at all (not
// "quiz optional but always graded").
[Table("Lms_Course")]
public class Lms_Course
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(6)]
    public string CompanyId { get; set; } = null!;

    public long? CategoryId { get; set; }

    [Required, StringLength(30)]
    public string Code { get; set; } = null!;

    [Required, StringLength(250)]
    public string Title { get; set; } = null!;

    [Column(TypeName = "nvarchar(max)")]
    public string? Description { get; set; }

    public CourseDeliveryType DeliveryType { get; set; } = CourseDeliveryType.Classroom;

    [Column(TypeName = "decimal(5,1)")]
    public decimal DurationHours { get; set; }

    [StringLength(200)]
    public string? InstructorName { get; set; }

    public bool RequiresApproval { get; set; }

    public int? PassingScorePercent { get; set; }

    public bool IsActive { get; set; } = true;
}
