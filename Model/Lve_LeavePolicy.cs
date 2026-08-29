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

    // Stable human-facing code for this record (audit: every master/document table needs one beyond the surrogate Id).
    [StringLength(30)]
    public string? Code { get; set; }

    [Required, StringLength(6)]
    public string CompanyId { get; set; } = null!;

    public LeaveType LeaveType { get; set; }

    [Column(TypeName = "decimal(5,1)")]
    public decimal EntitlementDaysPerYear { get; set; }

    // false = this leave type deducts pay — LeaveRequestService.PushUnpaidToPayrollAsync
    // creates a Pay_AdhocPayItem deduction once a request of this type is approved.
    public bool IsPaid { get; set; } = true;

    // Whether unused EntitlementDaysPerYear carries into next year — company
    // + leave-type specific (e.g. vacation carries, personal doesn't).
    // Capped uses MaxCarryOverDays; only the immediately prior year is
    // considered (no multi-year chaining). See LeaveBalanceService.
    public LeaveCarryOverMode CarryOverMode { get; set; } = LeaveCarryOverMode.None;
    [Column(TypeName = "decimal(5,1)")]
    public decimal? MaxCarryOverDays { get; set; }

    public bool IsActive { get; set; } = true;
}
