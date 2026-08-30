using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// A leave-of-absence request routed through the generic Workflow Approval
// Engine (Services/Workflow/WorkflowEngineService.cs) — mirrors
// emp_overtime_request's soft-link/JobMasterId convention for the
// HremployeeId/EmpNo/CompanyId snapshot fields (checked in application code,
// no FK), though HremployeeId and LeaveTypeId do have real FK constraints
// configured in HRMContext.OnModelCreating. JobMasterId is null while the
// request is still an editable draft and gets set once
// StartJobAsync(reftable: "Lve_LeaveRequest", refid: this.Id) is called.
[Table("Lve_LeaveRequest")]
public class Lve_LeaveRequest
{
    [Key]
    public long Id { get; set; }

    // Stable human-facing code for this record (audit: every master/document table needs one beyond the surrogate Id).
    [StringLength(30)]
    public string? RequestNo { get; set; }

    public long HremployeeId { get; set; }
    [Required, StringLength(6)]
    public string EmpNo { get; set; } = null!;
    [StringLength(6)]
    public string? CompanyId { get; set; }

    public int LeaveTypeId { get; set; }
    public virtual Lve_LeaveType Lve_LeaveType { get; set; } = null!;

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    [Column(TypeName = "decimal(5,1)")]
    public decimal TotalDays { get; set; }

    // Only meaningful when StartDate == EndDate — the UI forces EndDate back
    // to StartDate and TotalDays to 0.5 when this is set, rather than
    // running the working-day calculator.
    public bool IsHalfDay { get; set; }
    public HalfDayPeriod? HalfDayPeriod { get; set; }

    [StringLength(500)]
    public string? Reason { get; set; }

    public DateTime RequestedDate { get; set; } = DateTime.Now;

    // job_master.jobmasterid of the approval job started for this request.
    // Null until submitted — the request stays editable/re-submittable in
    // that state, same convention as emp_overtime_request.jobmasterid.
    public long? JobMasterId { get; set; }

    // Set once LeaveRequestService.PushUnpaidToPayrollAsync creates a real
    // Pay_AdhocPayItem deduction for this request (only relevant when the
    // Lve_LeavePolicy for this LeaveType has IsPaid=false) — mirrors
    // emp_overtime_request.hrwOtId's "stamp back to prevent double-push"
    // convention.
    public long? AdhocPayItemId { get; set; }

    // Optional attachment (sick leave), doc_center row keyed
    // doctypecode="LEAVE_MEDCERT" — see Endpoints/LeaveFileEndpoints.cs.
    public long? MedCertDocCenterId { get; set; }

    public virtual Hremployee Hremployee { get; set; } = null!;
}
