using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Audit trail for each punch-log file import — every device brand exports a
// slightly different CSV/TXT shape, so this also anchors troubleshooting when
// rows fail to match an employee.
[Table("Att_ImportBatch")]
public class Att_ImportBatch
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(50)]
    public string CompanyId { get; set; } = null!;

    public long? DeviceId { get; set; }

    [Required, StringLength(260)]
    public string FileName { get; set; } = null!;

    public long ImportedByUserId { get; set; }
    public DateTime ImportedDate { get; set; } = DateTime.Now;

    public int TotalRows { get; set; }
    public int ImportedRows { get; set; }
    public int SkippedRows { get; set; }

    [StringLength(2000)]
    public string? ErrorSummary { get; set; }

    public virtual Att_Device? Device { get; set; }
}
