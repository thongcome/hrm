using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Every workflow status transition, plus a per-employee tax-bracket
// calculation breakdown (DetailJson), is written here so both compliance
// audit trail and "why is this number what it is" tracing are possible.
// The legacy engine built this kind of breakdown (HREmpTaxRateDet) but
// discarded it in-memory without ever persisting it.
[Table("Pay_PayrollAuditLog")]
public class Pay_PayrollAuditLog
{
    [Key]
    public long Id { get; set; }

    public long PayrollRunId { get; set; }
    public long? PayrollEmployeeId { get; set; }

    public PayAuditEventType EventType { get; set; }

    public PayrollRunStatus? FromStatus { get; set; }
    public PayrollRunStatus? ToStatus { get; set; }

    public long ActorUserId { get; set; }
    public DateTime EventDate { get; set; } = DateTime.Now;

    [Column(TypeName = "nvarchar(max)")]
    public string? DetailJson { get; set; }

    [StringLength(500)]
    public string? Comment { get; set; }

    public virtual Pay_PayrollRun Pay_PayrollRun { get; set; } = null!;
    public virtual Pay_PayrollEmployee? Pay_PayrollEmployee { get; set; }
}
