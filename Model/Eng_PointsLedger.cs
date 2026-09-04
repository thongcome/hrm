using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

// One credited points-earning event from an activity source (not kudos —
// those live on Eng_Recognition). RefTable+RefId is the idempotency key so a
// given source event (an LMS completion, a tenure year) is only ever awarded
// once, no matter how often the sync runs. The employee's balance adds the sum
// of these to their kudos points (see EngagementService.GetBalanceAsync).
[Table("Eng_PointsLedger")]
[Index(nameof(HremployeeId))]
[Index(nameof(CompanyId))]
public class Eng_PointsLedger
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(20)]
    public string CompanyId { get; set; } = null!;

    public long HremployeeId { get; set; }

    public EngPointsSource Source { get; set; }

    public int Points { get; set; }

    // Idempotency key for auto-credited sources (e.g. "Lms_Enrollment"+id,
    // "TenureAnniversary"+"<empId>:<year>"). Null for manual awards.
    [StringLength(50)]
    public string? RefTable { get; set; }
    [StringLength(50)]
    public string? RefId { get; set; }

    [StringLength(300)]
    public string? Note { get; set; }

    public long? AwardedByUserId { get; set; }
    public DateTime EarnedDate { get; set; } = DateTime.Now;
    public bool IsActive { get; set; } = true;
}
