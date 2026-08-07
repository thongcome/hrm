using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

[Table("Rec_Interview")]
public class Rec_Interview
{
    [Key]
    public long Id { get; set; }

    public long ApplicationId { get; set; }

    // Free text: "Phone Screen" / "Technical" / "Final" etc.
    [Required, StringLength(100)]
    public string Round { get; set; } = null!;

    public DateTime ScheduledDate { get; set; }

    [StringLength(300)]
    public string? Location { get; set; }

    public InterviewStatus Status { get; set; } = InterviewStatus.Scheduled;

    public long CreatedByUserId { get; set; }
}
