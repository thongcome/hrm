using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

public enum PaySalaryAdvanceStatus
{
    Pending = 0,    // requested, not yet approved
    Approved = 1,   // approved, will be deducted from the target period
    Paid = 2,       // cash/transfer handed to the employee (still to be deducted)
    Deducted = 3,   // consumed by a payroll run
    Cancelled = 4,
    Rejected = 5,
}

// Salary advance (เบิกเงินเดือนล่วงหน้า): money paid to the employee before pay
// day and recovered in full from the TargetPeriod payroll as one deduction line
// (pay item SAL_ADVANCE). Unlike Pay_EmployeeLoan there is no schedule — one
// advance, one recovery. The engine only consumes Approved/Paid rows; Pending
// rows surface on the run's pre-flight checklist so HR approves them first.
[Table("Pay_SalaryAdvance")]
public class Pay_SalaryAdvance
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    // Plain id on purpose (Hremployee carries a global query filter); resolved by join where displayed.
    public long HremployeeId { get; set; }

    [StringLength(6)]
    public string? EmpNo { get; set; }

    [Required, StringLength(6)]
    public string CompanyId { get; set; } = null!;

    [Column(TypeName = "decimal(15,2)")]
    public decimal Amount { get; set; }

    public DateTime RequestDate { get; set; } = DateTime.Now;

    // When the money was/will be handed over — informational.
    public DateOnly? AdvanceDate { get; set; }

    // yyyyMM of the payroll run that recovers it.
    [Required, StringLength(6)]
    public string TargetPeriod { get; set; } = null!;

    [Required, StringLength(500)]
    public string Reason { get; set; } = null!;

    public PaySalaryAdvanceStatus Status { get; set; } = PaySalaryAdvanceStatus.Pending;

    public long RequestedByUserId { get; set; }
    public long? ApprovedByUserId { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public DateTime? PaidDate { get; set; }

    public long? ConsumedByPayrollRunId { get; set; }

    [StringLength(500)]
    public string? Remark { get; set; }
}
