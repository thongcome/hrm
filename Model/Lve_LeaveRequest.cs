using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// A leave-of-absence request routed through the generic Workflow Approval
// Engine (Services/Workflow/WorkflowEngineService.cs) — mirrors
// emp_overtime_request's soft-link/JobMasterId convention exactly (see
// comments on that model): no FK constraints, HremployeeId/EmpNo/CompanyId
// are denormalized snapshots checked in application code, and JobMasterId
// is null while the request is still an editable draft and gets set once
// StartJobAsync(reftable: "Lve_LeaveRequest", refid: this.Id) is called.
[Table("Lve_LeaveRequest")]
public class Lve_LeaveRequest
{
    [Key]
    public long Id { get; set; }

    public long HremployeeId { get; set; }
    [Required, StringLength(6)]
    public string EmpNo { get; set; } = null!;
    [StringLength(6)]
    public string? CompanyId { get; set; }

    public LeaveType LeaveType { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    [Column(TypeName = "decimal(5,1)")]
    public decimal TotalDays { get; set; }

    [StringLength(500)]
    public string? Reason { get; set; }

    public DateTime RequestedDate { get; set; } = DateTime.Now;

    // job_master.jobmasterid of the approval job started for this request.
    // Null until submitted — the request stays editable/re-submittable in
    // that state, same convention as emp_overtime_request.jobmasterid.
    public long? JobMasterId { get; set; }

    public virtual Hremployee Hremployee { get; set; } = null!;
}
