using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// "Block Leave" / mandatory consecutive leave — an internal-control policy
// (common at banks) requiring every employee to take a continuous stretch of
// leave each year, so any wrongdoing that depends on their continuous
// presence surfaces while someone else covers their duties. One row per
// company, ongoing (no year-versioning — unlike Pay_TaxBracket-style tables,
// this is a standing policy, not something that changes rate year to year).
// Report-only: BlockLeaveComplianceService just tells HR who has/hasn't
// satisfied it — nothing here blocks a leave request or any other action.
[Table("Lve_BlockLeavePolicy")]
public class Lve_BlockLeavePolicy
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(6)]
    public string CompanyId { get; set; } = null!;

    public int MinConsecutiveWorkingDays { get; set; } = 5;

    public bool IsEnabled { get; set; }
}
