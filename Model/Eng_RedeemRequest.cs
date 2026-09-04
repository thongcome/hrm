using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

// An employee's request to redeem points for a reward. Goes through the shared
// workflow engine (JobMasterId) for approval, exactly like leave/OT/IDP — the
// module doesn't hardcode approval logic. PointsSpent is snapshotted at request
// time so a later change to the item's cost doesn't rewrite history. Status is
// read back from the job via the lazy apply-on-read pattern the other modules use.
[Table("Eng_RedeemRequest")]
[Index(nameof(CompanyId))]
[Index(nameof(HremployeeId))]
public class Eng_RedeemRequest
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(20)]
    public string CompanyId { get; set; } = null!;

    public long HremployeeId { get; set; }
    public long RedeemItemId { get; set; }

    [StringLength(150)]
    public string? SnapshotItemName { get; set; }

    public int PointsSpent { get; set; }

    public EngRedeemStatus Status { get; set; } = EngRedeemStatus.Draft;

    // Set once the request enters the workflow engine; null while still a draft.
    public long? JobMasterId { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    public long RequestedByUserId { get; set; }
    public DateTime RequestedDate { get; set; } = DateTime.Now;
    public DateTime? FulfilledDate { get; set; }
    public bool IsActive { get; set; } = true;
}
