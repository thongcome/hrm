using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// One row per scheduled deduction period for a Pay_EmployeeLoan. Generated
// all at once when the loan is created (EmployeeLoanScheduleCalculator).
// ConsumedByPayrollRunId follows the same idempotent-recalculation shape as
// Pay_AdhocPayItem.ConsumedByPayrollRunId: PayrollCalculationService queries
// rows that are Pending OR already Consumed by the run being (re)calculated,
// so recalculating a Draft/Calculated run re-picks up the same installment
// instead of skipping it or double-deducting it.
[Table("Pay_EmployeeLoanInstallment")]
public class Pay_EmployeeLoanInstallment
{
    [Key]
    public long Id { get; set; }

    public long LoanId { get; set; }

    public int InstallmentNo { get; set; }

    // "YYYYMM", Gregorian — matches Pay_PayrollRun.PayrollPeriod
    [Required, StringLength(6)]
    public string Period { get; set; } = null!;

    [Column(TypeName = "decimal(15,2)")]
    public decimal Amount { get; set; }

    // principal remaining immediately after this installment is deducted
    [Column(TypeName = "decimal(15,2)")]
    public decimal BalanceAfter { get; set; }

    public Pay_LoanInstallmentStatus Status { get; set; } = Pay_LoanInstallmentStatus.Pending;

    public long? ConsumedByPayrollRunId { get; set; }

    public virtual Pay_EmployeeLoan Pay_EmployeeLoan { get; set; } = null!;
    public virtual Pay_PayrollRun? ConsumedByPayrollRun { get; set; }
}
