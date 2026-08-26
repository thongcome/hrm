using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Tracks fund-membership tenure as a clock SEPARATE from employment tenure
// (Hremployee.WorkDate) — real fund regulations reset "years as a member"
// when someone quits the fund and later rejoins, even though their
// employment never stopped. Only consulted when a policy has
// Pay_ProvidentFundPolicy.UseFundMembershipYearsForVesting turned on;
// otherwise vesting/exit calculations use employment tenure as before this
// table existed. A null LeaveDate means the current/most-recent membership
// period is still open.
[Table("Pay_ProvidentFundMembershipPeriod")]
public class Pay_ProvidentFundMembershipPeriod
{
    [Key]
    public long Id { get; set; }

    public long HremployeeId { get; set; }

    public DateOnly JoinDate { get; set; }
    public DateOnly? LeaveDate { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    public virtual Hremployee Hremployee { get; set; } = null!;
}
