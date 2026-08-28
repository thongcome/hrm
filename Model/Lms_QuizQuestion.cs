using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Multiple-choice only (4 options) — no essay/matching question types in v1.
[Table("Lms_QuizQuestion")]
public class Lms_QuizQuestion
{
    [Key]
    public long Id { get; set; }

    public long CourseId { get; set; }

    [Required, StringLength(1000)]
    public string QuestionText { get; set; } = null!;

    [Required, StringLength(500)]
    public string ChoiceA { get; set; } = null!;

    [Required, StringLength(500)]
    public string ChoiceB { get; set; } = null!;

    [Required, StringLength(500)]
    public string ChoiceC { get; set; } = null!;

    [Required, StringLength(500)]
    public string ChoiceD { get; set; } = null!;

    [Required, StringLength(1)]
    public string CorrectChoice { get; set; } = null!;

    public int SortOrder { get; set; }

    // Soft-delete only — Lms_QuizAnswer rows reference QuestionId from past
    // attempts, so hard-deleting a question would orphan historical answers
    // and break a student's scored quiz history.
    public bool IsActive { get; set; } = true;
}
