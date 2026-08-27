using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Append-only renewal history for CT_Contract — the gap that made the
// contracts module feel like a flat record instead of something with a real
// lifecycle: extending a contract used to mean silently overwriting
// expired_date with no trace of what the date used to be or who changed it.
[Table("CT_ContractRenewal")]
public class CT_ContractRenewal
{
    [Key]
    public long Id { get; set; }

    public long ContractId { get; set; }

    public DateOnly? OldExpiredDate { get; set; }
    public DateOnly NewExpiredDate { get; set; }

    [StringLength(1000)]
    public string? Note { get; set; }

    public long RenewedByUserId { get; set; }
    public DateTime RenewedDate { get; set; } = DateTime.Now;
}
