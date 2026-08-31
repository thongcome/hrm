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

    // Competency this course develops (nullable soft-link to
    // Comp_Competency, same idiom as Perf_Indicator.CompetencyId) — lets
    // the ESS catalog surface "หลักสูตรนี้ช่วยปิด gap ของคุณ" against the
    // viewer's own computed competency gaps (IdpAssessmentService), instead
    // of the catalog being a flat undifferentiated list. One competency per
    // course by design: a course genuinely targeting several competencies
    // is rare enough in practice that a join table isn't worth its weight
    // yet — extract one when a real second-competency case shows up.
    public long? CompetencyId { get; set; }

    public bool IsActive { get; set; } = true;
}
