using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

[Table("Lms_QuizAttempt")]
public class Lms_QuizAttempt
{
    [Key]
    public long Id { get; set; }

    public long EnrollmentId { get; set; }

    public DateTime AttemptDate { get; set; } = DateTime.Now;

    [Column(TypeName = "decimal(5,2)")]
    public decimal ScorePercent { get; set; }

    public bool IsPassed { get; set; }
}
