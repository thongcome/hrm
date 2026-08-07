using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Per-company retention preference — used only to flag candidate records
// nearing the retention window in the UI. Deliberately no auto-delete job
// (matches this codebase's "never silently decide for HR" convention).
[Table("Rec_CandidateDataRetentionSettings")]
public class Rec_CandidateDataRetentionSettings
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(6)]
    public string CompanyId { get; set; } = null!;

    public int RetentionMonths { get; set; } = 12;
}
