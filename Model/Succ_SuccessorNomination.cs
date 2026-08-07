using System.ComponentModel.DataAnnotations;

namespace HRM.Models;

// A person nominated as a potential successor for a Succ_KeyPosition.
// Multiple nominations per key position are expected (bench strength =
// counting these). Removed via soft-delete (IsActive=false) — same
// convention as Talent_PoolEntry — so nomination history survives even after
// someone is taken off the slate.
public class Succ_SuccessorNomination
{
    [Key]
    public long Id { get; set; }

    public long KeyPositionId { get; set; }
    public long HremployeeId { get; set; }

    public ReadinessLevel ReadinessLevel { get; set; }

    public long NominatedByUserId { get; set; }
    public DateTime NominatedDate { get; set; } = DateTime.Now;

    [StringLength(500)]
    public string? Note { get; set; }

    public bool IsActive { get; set; } = true;
}
