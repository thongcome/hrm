using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// One row per confirmed employee-import (Services/Hr/EmployeeImport/EmployeeImportService). The file's
// SHA-256 lets the page warn "this file was already imported on … by …" before a second upload —
// re-importing is safe anyway (rows are matched by company + employee code and updated, never
// duplicated), the warning just stops someone from doing it by accident.
[Table("Hr_EmployeeImportBatch")]
public class Hr_EmployeeImportBatch
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(50)]
    public string CompanyId { get; set; } = null!;

    [StringLength(260)]
    public string? FileName { get; set; }

    [Required, StringLength(64)]
    public string FileSha256 { get; set; } = null!;

    public int OrgsAdded { get; set; }
    public int OrgsUpdated { get; set; }
    public int EmployeesAdded { get; set; }
    public int EmployeesUpdated { get; set; }
    public int LoginsCreated { get; set; }
    public int OpeningBalancesImported { get; set; }

    [StringLength(2000)]
    public string? Warnings { get; set; }

    public long ImportedByUserId { get; set; }
    public DateTime ImportedAt { get; set; } = DateTime.Now;
}
