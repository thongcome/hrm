using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

// One employee's payroll for one Pay_PayrollRun.
// GrossEarnings/TotalDeductions/NetPay are denormalized summaries computed
// from Pay_PayrollLineItem rows, kept here for fast list/grid rendering.
[Table("Pay_PayrollEmployee")]
[Index(nameof(PayrollRunId), nameof(HremployeeId), IsUnique = true)]
public class Pay_PayrollEmployee
{
    [Key]
    public long Id { get; set; }

    public long PayrollRunId { get; set; }
    public long HremployeeId { get; set; }

    // denormalized for convenience/reporting; source of truth is Hremployee
    [StringLength(6)]
    public string? EmpNo { get; set; }
    [StringLength(6)]
    public string? CompanyId { get; set; }

    [Column(TypeName = "decimal(6,4)")]
    public decimal ProrationFactor { get; set; } = 1.0m;
    public int WorkingDaysInPeriod { get; set; }
    public int ActualWorkingDays { get; set; }

    [Column(TypeName = "decimal(15,2)")]
    public decimal GrossEarnings { get; set; }
    [Column(TypeName = "decimal(15,2)")]
    public decimal TotalDeductions { get; set; }
    [Column(TypeName = "decimal(15,2)")]
    public decimal NetPay { get; set; }

    [Column(TypeName = "decimal(15,2)")]
    public decimal TaxAmount { get; set; }
    [Column(TypeName = "decimal(15,2)")]
    public decimal SocialSecurityAmount { get; set; }
    [Column(TypeName = "decimal(15,2)")]
    public decimal ProvidentFundEmployeeAmount { get; set; }
    [Column(TypeName = "decimal(15,2)")]
    public decimal ProvidentFundCompanyAmount { get; set; }

    // set by NetPayGuardService when calculated net pay would be negative;
    // ApproveAsync blocks the run while any employee has this set
    public bool IsNegativeNetPayFlag { get; set; }

    public bool IsExcluded { get; set; }
    [StringLength(500)]
    public string? ExcludeReason { get; set; }

    // snapshotted at calculation time so later changes to the employee's
    // bank details never retroactively change an already-calculated run
    [StringLength(3)]
    public string? BankCode { get; set; }
    [StringLength(10)]
    public string? BankBranchCode { get; set; }
    [StringLength(20)]
    public string? BankAccountNo { get; set; }

    [StringLength(500)]
    public string? Remark { get; set; }

    public virtual Pay_PayrollRun Pay_PayrollRun { get; set; } = null!;
    public virtual Hremployee Hremployee { get; set; } = null!;
    public virtual ICollection<Pay_PayrollLineItem> Pay_PayrollLineItems { get; set; } = new List<Pay_PayrollLineItem>();
    public virtual Pay_Payslip? Pay_Payslip { get; set; }
    public virtual ICollection<Pay_PayrollAuditLog> Pay_PayrollAuditLogs { get; set; } = new List<Pay_PayrollAuditLog>();
    public virtual ICollection<Pay_BankFileExportLine> Pay_BankFileExportLines { get; set; } = new List<Pay_BankFileExportLine>();
}
