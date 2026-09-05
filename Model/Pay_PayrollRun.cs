using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

// Batch/period header carrying the workflow state machine:
// Draft -> Calculated -> Reviewed -> Approved -> Posted -> Paid (or Cancelled).
// Replaces the old Hrpayroll.PayrollStatus field, which was hard-coded to 0
// and never transitioned anywhere in the legacy Payroll pages.
[Table("Pay_PayrollRun")]
[Index(nameof(CompanyId), nameof(PayrollPeriod), nameof(RunType), IsUnique = true)]
public class Pay_PayrollRun
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(6)]
    public string CompanyId { get; set; } = null!;

    // "YYYYMM", Gregorian
    [Required, StringLength(6)]
    public string PayrollPeriod { get; set; } = null!;

    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public DateOnly PayDate { get; set; }

    public PayrollRunType RunType { get; set; } = PayrollRunType.Regular;
    public PayrollRunStatus Status { get; set; } = PayrollRunStatus.Draft;

    // set only when RunType == Adjustment
    public long? AdjustmentOfRunId { get; set; }

    public long CreatedByUserId { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public long? CalculatedByUserId { get; set; }
    public DateTime? CalculatedDate { get; set; }

    // Background-calculation state (Phase A). The calculation runs as a
    // detached server job so it survives the HR user closing the page; these
    // columns let any page/browser see that a job is in flight and let a
    // startup check detect a job that was interrupted by a server restart
    // (IsCalculating=true with no live job in PayrollCalcJobRegistry).
    public bool IsCalculating { get; set; }
    public DateTime? CalcStartedAt { get; set; }
    [StringLength(1000)]
    public string? CalcError { get; set; }

    public long? ReviewedByUserId { get; set; }
    public DateTime? ReviewedDate { get; set; }

    public long? ApprovedByUserId { get; set; }
    public DateTime? ApprovedDate { get; set; }

    public long? PostedByUserId { get; set; }
    public DateTime? PostedDate { get; set; }

    public long? PaidByUserId { get; set; }
    public DateTime? PaidDate { get; set; }

    [StringLength(500)]
    public string? Remark { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public virtual Pay_PayrollRun? AdjustmentOfRun { get; set; }
    public virtual ICollection<Pay_PayrollEmployee> Pay_PayrollEmployees { get; set; } = new List<Pay_PayrollEmployee>();
    public virtual ICollection<Pay_PayrollAuditLog> Pay_PayrollAuditLogs { get; set; } = new List<Pay_PayrollAuditLog>();
    public virtual ICollection<Pay_BankFileExportBatch> Pay_BankFileExportBatches { get; set; } = new List<Pay_BankFileExportBatch>();
    public virtual ICollection<Pay_GLExportBatch> Pay_GLExportBatches { get; set; } = new List<Pay_GLExportBatch>();
}
