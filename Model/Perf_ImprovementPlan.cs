using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

// Performance Improvement Plan header — mirrors Idp_Plan's shape (Draft ->
// PendingApproval -> Approved/Rejected via the generic Workflow Engine,
// lazy apply-on-read status sync, no scheduler exists anywhere in this
// codebase) but "Approved" here means "Active" (the plan is now in force
// and being tracked), with its own terminal outcomes (Passed/Extended/
// Failed) a plain IDP plan doesn't need.
[Table("Perf_ImprovementPlan")]
[Index(nameof(HremployeeId))]
public class Perf_ImprovementPlan
{
    [Key]
    public long Id { get; set; }

    // Soft link to Hremployee.id — same convention as Perf_EvaluationInstance
    // (no DB FK, avoids a second cascade path).
    public long HremployeeId { get; set; }

    // Optional soft-link to the evaluation that triggered this PIP. Nullable
    // because a PIP can also start from an observed performance/conduct
    // issue outside the formal review cycle.
    public long? SourceEvaluationInstanceId { get; set; }

    [Required, StringLength(1000)]
    public string Reason { get; set; } = null!;

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    public PipStatus Status { get; set; } = PipStatus.Draft;

    public long? JobMasterId { get; set; }

    // The manager expected to record check-ins and own the outcome —
    // separate from CreatedByUserId, which may be HR setting the plan up on
    // the manager's behalf.
    public long? ManagerUserId { get; set; }

    // Soft-link to the prior Perf_ImprovementPlan when this one exists
    // because the previous round ended in PipStatus.Extended.
    public long? PreviousPlanId { get; set; }

    public DateTime? OutcomeDate { get; set; }
    [StringLength(1000)]
    public string? OutcomeNote { get; set; }

    public long CreatedByUserId { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public virtual ICollection<Perf_ImprovementGoal> Goals { get; set; } = new List<Perf_ImprovementGoal>();
    public virtual ICollection<Perf_ImprovementCheckIn> CheckIns { get; set; } = new List<Perf_ImprovementCheckIn>();
}
