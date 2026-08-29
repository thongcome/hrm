using System.ComponentModel.DataAnnotations;

namespace HRM.Models;

// A person nominated as a potential successor for a Succ_KeyPosition.
// Multiple nominations per key position are expected (bench strength =
// counting these). Removed via soft-delete (IsActive=false) — same
// convention as Talent_PoolEntry — so nomination history survives even after
// someone is taken off the slate.
//
// Gated by the generic Workflow Engine (JobMasterId) — standard succession
// governance expects leadership sign-off before someone is formally on a
// slate, unlike Eng_ActionPlan/OrgDev_ChangeInitiative which are deliberately
// un-gated tracking tools. Status is only synced from the job lazily, on
// read (SuccessionService.SyncStatusFromJobAsync), mirroring
// IdpPlanService/LmsEnrollmentService exactly (no scheduler exists anywhere
// in this codebase).
public class Succ_SuccessorNomination
{
    [Key]
    public long Id { get; set; }

    // Stable human-facing code for this record (audit: every master/document table needs one beyond the surrogate Id).
    [StringLength(30)]
    public string? NominationCode { get; set; }

    public long KeyPositionId { get; set; }
    public long HremployeeId { get; set; }

    public ReadinessLevel ReadinessLevel { get; set; }

    public long NominatedByUserId { get; set; }
    public DateTime NominatedDate { get; set; } = DateTime.Now;

    [StringLength(500)]
    public string? Note { get; set; }

    public bool IsActive { get; set; } = true;

    public SuccessionNominationStatus Status { get; set; } = SuccessionNominationStatus.PendingApproval;
    public long? JobMasterId { get; set; }
}
