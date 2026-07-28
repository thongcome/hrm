using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Bank transfer file export batch for a payroll run (requires Status >=
// Approved). BankFormatCode defaults to a generic CSV layout for phase 1;
// a real bank's fixed-width/CSV spec can be added later as a new format
// code handled by BankFileExportService without changing this schema.
[Table("Pay_BankFileExportBatch")]
public class Pay_BankFileExportBatch
{
    [Key]
    public long Id { get; set; }

    public long PayrollRunId { get; set; }

    [Required, StringLength(30)]
    public string BankFormatCode { get; set; } = "GENERIC_CSV";

    [Required, StringLength(500)]
    public string FilePath { get; set; } = null!;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }
    public int TotalRecordCount { get; set; }

    public BankFileExportStatus Status { get; set; } = BankFileExportStatus.Generated;

    public long GeneratedByUserId { get; set; }
    public DateTime GeneratedDate { get; set; } = DateTime.Now;

    public virtual Pay_PayrollRun Pay_PayrollRun { get; set; } = null!;
    public virtual ICollection<Pay_BankFileExportLine> Pay_BankFileExportLines { get; set; } = new List<Pay_BankFileExportLine>();
}

[Table("Pay_BankFileExportLine")]
public class Pay_BankFileExportLine
{
    [Key]
    public long Id { get; set; }

    public long BankFileExportBatchId { get; set; }
    public long PayrollEmployeeId { get; set; }

    [StringLength(3)]
    public string? BankCode { get; set; }
    [StringLength(10)]
    public string? BankBranchCode { get; set; }
    [StringLength(20)]
    public string? BankAccountNo { get; set; }

    [Column(TypeName = "decimal(15,2)")]
    public decimal Amount { get; set; }

    public virtual Pay_BankFileExportBatch Pay_BankFileExportBatch { get; set; } = null!;
    public virtual Pay_PayrollEmployee Pay_PayrollEmployee { get; set; } = null!;
}
