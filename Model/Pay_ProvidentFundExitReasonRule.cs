using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Company-configurable catalog of "reason membership ended" categories,
// each carrying its own effect on the employer-contribution vesting outcome
// — real fund regulations don't vest purely on tenure: fraud/serious
// misconduct forfeits everything regardless of years served, death/
// retirement pays in full regardless of years served, and "quit the fund
// without quitting the job" forfeits everything UNLESS the member meets an
// age+fund-membership-tenure exception. RequiresAgeAndMembershipCheck +
// the two Min* fields capture that last case; when OverrideType isn't that
// exception-bearing case, they're simply unused (null/false).
[Table("Pay_ProvidentFundExitReasonRule")]
public class Pay_ProvidentFundExitReasonRule
{
    [Key]
    public long Id { get; set; }

    public long PolicyId { get; set; }

    [Required, StringLength(20)]
    public string Code { get; set; } = null!;

    [Required, StringLength(200)]
    public string Name { get; set; } = null!;

    public ProvidentFundExitVestingOverride OverrideType { get; set; }

    // When true (only meaningful paired with OverrideType == ForceZero),
    // a member who meets both Min* thresholds gets treated as ForceFull
    // instead — e.g. "quit fund without quitting job" normally forfeits
    // everything, except for someone >=50 years old with >=20 years of
    // fund membership, who still gets paid in full.
    public bool RequiresAgeAndMembershipCheck { get; set; }
    public int? MinAgeForException { get; set; }
    public int? MinMembershipYearsForException { get; set; }

    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    public virtual Pay_ProvidentFundPolicy Policy { get; set; } = null!;
}
