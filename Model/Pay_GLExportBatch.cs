using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// GL/accounting journal export for a payroll run (requires Status >=
// Posted). Entries are aggregated per GL account across the whole run,
// not one row per employee. ExportFormatCode defaults to a generic
// balanced CSV journal for phase 1.
[Table("Pay_GLExportBatch")]
public class Pay_GLExportBatch
{
    [Key]
    public long Id { get; set; }

    public long PayrollRunId { get; set; }

    [Required, StringLength(30)]
    public string ExportFormatCode { get; set; } = "GENERIC_CSV";

    [Required, StringLength(500)]
    public string FilePath { get; set; } = null!;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalDebit { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalCredit { get; set; }

    public long GeneratedByUserId { get; set; }
    public DateTime GeneratedDate { get; set; } = DateTime.Now;

    public virtual Pay_PayrollRun Pay_PayrollRun { get; set; } = null!;
    public virtual ICollection<Pay_GLExportEntry> Pay_GLExportEntries { get; set; } = new List<Pay_GLExportEntry>();
}

[Table("Pay_GLExportEntry")]
public class Pay_GLExportEntry
{
    [Key]
    public long Id { get; set; }

    public long GLExportBatchId { get; set; }

    [Required, StringLength(20)]
    public string GLAccountCode { get; set; } = null!;
    [StringLength(20)]
    public string? CostCenterCode { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal DebitAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal CreditAmount { get; set; }

    [StringLength(200)]
    public string? Description { get; set; }

    public virtual Pay_GLExportBatch Pay_GLExportBatch { get; set; } = null!;
}
