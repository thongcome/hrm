using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Status is not stored here — mirrors Exp_ClaimHeader/Lve_LeaveRequest: JobMasterId
// null = still-editable draft; once submitted, current status is derived live by
// joining job_masters.status via JobMasterId (see DisciplinaryActionService.GetJobStatusesAsync).
public class Hr_DisciplinaryCase
{
    public long Id { get; set; }

    public long HremployeeId { get; set; }
    [StringLength(6)]
    public string? EmpNo { get; set; }
    [StringLength(6)]
    public string CompanyId { get; set; } = null!;

    public DisciplinaryActionType ActionType { get; set; }
    public DateOnly IncidentDate { get; set; }
    [Column(TypeName = "nvarchar(max)")]
    public string Description { get; set; } = null!;
    [StringLength(500)]
    public string? RuleViolated { get; set; }
    // Only meaningful when ActionType = Suspension.
    public int? SuspensionDays { get; set; }
    // The date the action takes effect once approved — HR sets this, not auto-derived.
    public DateOnly? EffectiveDate { get; set; }

    public long? JobMasterId { get; set; }

    public long CreatedByUserId { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public virtual Hremployee Hremployee { get; set; } = null!;
}
