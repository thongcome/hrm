using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// HR-entered ad-hoc income/deduction request (bonus, commission, uniform
// deduction, damage compensation, etc.) targeting a specific pay period.
// Pending -> Approved (or Rejected/Cancelled) -> Consumed once a payroll
// calculation for TargetPeriod actually pulls it in as a line item.
//
// v1 scope: HR enters and approves on behalf of the employee (no employee
// self-service submission yet — that's a later ESS phase) and each row
// targets exactly one period (no installment/split-across-periods support
// yet; add an InstallmentGroupId later if needed without breaking this).
[Table("Pay_AdhocPayItem")]
public class Pay_AdhocPayItem
{
    [Key]
    public long Id { get; set; }

    // Stable human-facing code for this record (audit: every master/document table needs one beyond the surrogate Id).
    [StringLength(30)]
    public string? RequestCode { get; set; }

    public long HremployeeId { get; set; }

    public int PayItemTypeId { get; set; }

    // "YYYYMM", Gregorian — matches Pay_PayrollRun.PayrollPeriod
    [Required, StringLength(6)]
    public string TargetPeriod { get; set; } = null!;

    [Column(TypeName = "decimal(15,2)")]
    public decimal Amount { get; set; }

    // whether this amount counts toward taxable income for withholding tax
    // purposes (e.g. a reimbursement is typically not taxable, a bonus is)
    public bool IsTaxable { get; set; } = true;

    [Required, StringLength(500)]
    public string Reason { get; set; } = null!;

    [StringLength(500)]
    public string? Remark { get; set; }

    public PayAdhocItemStatus Status { get; set; } = PayAdhocItemStatus.Pending;

    public long RequestedByUserId { get; set; }
    public DateTime RequestedDate { get; set; } = DateTime.Now;

    public long? ApprovedByUserId { get; set; }
    public DateTime? ApprovedDate { get; set; }

    // set once a Pay_PayrollRun calculation actually consumes this item, so
    // it isn't pulled into a second run for the same period
    public long? ConsumedByPayrollRunId { get; set; }

    public virtual Hremployee Hremployee { get; set; } = null!;
    public virtual Pay_PayItemType Pay_PayItemType { get; set; } = null!;
    public virtual Pay_PayrollRun? ConsumedByPayrollRun { get; set; }
}
