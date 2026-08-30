using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Per-company, per-leave-type annual entitlement — used to compute remaining
// balance (Entitlement - approved days used this calendar year) shown when
// submitting a Lve_LeaveRequest. Pro-ration for mid-year hires and carry-over
// tracking both live in LeaveBalanceService, driven by the fields below.
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

    public int LeaveTypeId { get; set; }
    public virtual Lve_LeaveType Lve_LeaveType { get; set; } = null!;

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

    // Carried-over days from the prior year are only usable through this many
    // months into the target year (e.g. 3 = must be used by March 31) — null
    // means no expiry, only the MaxCarryOverDays cap applies (today's
    // behavior). Checked in LeaveBalanceService.ComputeCarryOverAsync against
    // DateTime.Today, so it only affects the balance shown going forward —
    // never retroactively invalidates days already used before expiry.
    public int? CarryOverExpiryMonths { get; set; }

    // Employee must have at least this many months of service (Hremployee.WorkDate)
    // before this leave type has any entitlement at all — null means no
    // minimum (every policy's default, backward compatible). See
    // Services/Shared/TenureHelper.cs and LeaveBalanceService.GetBalancesAsync.
    public int? MinServiceMonths { get; set; }

    public bool IsActive { get; set; } = true;
}
