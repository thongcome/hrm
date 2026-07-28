using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Earning/deduction line, one per Pay_PayrollEmployee per pay item.
// SourceRefTable/SourceRefId trace back to the originating legacy record
// (e.g. "HRW_OT"/HrwOt.ID or "KPTEMPRECEIVEDET"/Kptempreceivedet.ID) so a
// calculated amount can always be audited back to its source row.
[Table("Pay_PayrollLineItem")]
public class Pay_PayrollLineItem
{
    [Key]
    public long Id { get; set; }

    public long PayrollEmployeeId { get; set; }
    public int PayItemTypeId { get; set; }

    public PayLineSourceType SourceType { get; set; }

    [StringLength(50)]
    public string? SourceRefTable { get; set; }
    public long? SourceRefId { get; set; }

    [Column(TypeName = "decimal(15,2)")]
    public decimal Amount { get; set; }

    // snapshotted from Pay_PayItemType.DefaultSignFlag at calc time
    public int SignFlag { get; set; }

    [StringLength(300)]
    public string? Description { get; set; }

    public int SeqNo { get; set; }

    public virtual Pay_PayrollEmployee Pay_PayrollEmployee { get; set; } = null!;
    public virtual Pay_PayItemType Pay_PayItemType { get; set; } = null!;
}
