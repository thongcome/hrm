using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Per-company, per-leave-type annual entitlement — used to compute remaining
// balance (Entitlement - approved days used this calendar year) shown when
// submitting a Lve_LeaveRequest. Deliberately simple for this MVP: no
// pro-ration for mid-year hires and no carry-over tracking yet.
[Table("Lve_LeavePolicy")]
public class Lve_LeavePolicy
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(6)]
    public string CompanyId { get; set; } = null!;

    public LeaveType LeaveType { get; set; }

    [Column(TypeName = "decimal(5,1)")]
    public decimal EntitlementDaysPerYear { get; set; }

    public bool IsActive { get; set; } = true;
}
