using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

[Table("Lms_QuizAnswer")]
public class Lms_QuizAnswer
{
    [Key]
    public long Id { get; set; }

    public long QuizAttemptId { get; set; }

    public long QuestionId { get; set; }

    [Required, StringLength(1)]
    public string SelectedChoice { get; set; } = null!;

    public bool IsCorrect { get; set; }
}
