using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// An employee-submitted expense/travel claim, routed through the generic
// Workflow Approval Engine (Services/Workflow/WorkflowEngineService.cs) —
// mirrors Lve_LeaveRequest's convention exactly: no FK constraints,
// HremployeeId/EmpNo/CompanyId are denormalized snapshots checked in
// application code, status is NOT a stored enum but derived live from
// JobMasterId (null = still-editable draft) joined against job_master.status
// once submitted (see ExpenseClaimService.GetStatusTextAsync / the same
// pattern LeaveRequestList.razor uses).
[Table("Exp_ClaimHeader")]
public class Exp_ClaimHeader
{
    [Key]
    public long Id { get; set; }

    // Stable human-facing claim number for this record (audit: every master/document table needs one beyond the surrogate Id).
    [StringLength(30)]
    public string? ClaimNo { get; set; }

    public long HremployeeId { get; set; }
    [Required, StringLength(6)]
    public string EmpNo { get; set; } = null!;
    [StringLength(6)]
    public string? CompanyId { get; set; }

    [StringLength(500)]
    public string? Reason { get; set; }

    // Recomputed from Exp_ClaimLineItem.Amount sum every time a line is
    // added/removed and again right before submit (never trust a stale
    // client-side total).
    [Column(TypeName = "decimal(15,2)")]
    public decimal TotalAmount { get; set; }

    public DateTime RequestedDate { get; set; } = DateTime.Now;

    // job_master.jobmasterid of the approval job started for this claim.
    // Null while still an editable draft (lines can be added/removed) —
    // set once StartJobAsync(reftable: "Exp_ClaimHeader", refid: this.Id)
    // is called and the claim becomes read-only.
    public long? JobMasterId { get; set; }

    // Pay_AdhocPayItem.Id created once HR pushes an approved claim into the
    // reimbursement payroll pipeline. Null = not yet pushed — this is the
    // idempotency guard against double-pushing the same claim, same pattern
    // as SeveranceService's duplicate-submission check.
    public long? AdhocPayItemId { get; set; }

    public virtual Hremployee Hremployee { get; set; } = null!;
    public virtual ICollection<Exp_ClaimLineItem> Exp_ClaimLineItems { get; set; } = new List<Exp_ClaimLineItem>();
}
