using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// An employee HR has explicitly flagged as part of the talent pool.
// Deliberately NOT auto-populated from any 9-box cell — always an explicit
// HR action (AddToPoolAsync), so nobody gets silently labeled "star
// performer" by an algorithm. Removed via soft-delete (IsActive=false), same
// convention as the rest of the system, so the entry's history survives.
[Table("Talent_PoolEntry")]
public class Talent_PoolEntry
{
    [Key]
    public long Id { get; set; }

    public long HremployeeId { get; set; }

    public DateTime AddedDate { get; set; } = DateTime.Now;

    public long AddedByUserId { get; set; }

    [StringLength(500)]
    public string? Reason { get; set; }

    public bool IsActive { get; set; } = true;
}
