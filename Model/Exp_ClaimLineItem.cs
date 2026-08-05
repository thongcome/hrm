using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

[Table("Exp_ClaimLineItem")]
public class Exp_ClaimLineItem
{
    [Key]
    public long Id { get; set; }

    public long ClaimHeaderId { get; set; }

    public DateOnly ExpenseDate { get; set; }

    public long CategoryId { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    [Column(TypeName = "decimal(15,2)")]
    public decimal Amount { get; set; }

    // Soft link to doc_center.id (doctypecode="EXP_RECEIPT", refid=this.Id)
    // — same reuse-doc_center convention as every other attachment feature
    // in this app (announcements, workflow attachments). Null = no receipt
    // uploaded yet; not all expense categories require one.
    public long? ReceiptDocCenterId { get; set; }

    public virtual Exp_ClaimHeader ClaimHeader { get; set; } = null!;
    public virtual Exp_ExpenseCategory Category { get; set; } = null!;
}
