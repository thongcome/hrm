using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// A membership-termination case that goes through the Workflow Approval
// Engine before its vesting ruling is finalized. ComputedVestingPercent is
// what the system decides (Pay_ProvidentFundExitReasonRule override, or
// falling back to Pay_ProvidentFundVestingTier) once approved — the three
// Amount fields are deliberately nullable, manually-entered by HR from the
// fund manager's actual statement, because HRM has no running balance or
// investment-gain ledger of its own (that lives with the fund manager, not
// here); the system's job is to decide and document the PERCENTAGE/rule
// outcome, not to be the source of truth for the real baht amounts.
[Table("Pay_ProvidentFundExitCase")]
public class Pay_ProvidentFundExitCase
{
    [Key]
    public long Id { get; set; }

    public long HremployeeId { get; set; }
    public long PolicyId { get; set; }
    public long ExitReasonRuleId { get; set; }

    public DateOnly ExitDate { get; set; }

    public ProvidentFundRequestStatus Status { get; set; } = ProvidentFundRequestStatus.PendingApproval;

    public long? JobMasterId { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? ComputedVestingPercent { get; set; }

    [Column(TypeName = "decimal(15,2)")]
    public decimal? EmployeeContributionAmount { get; set; }
    [Column(TypeName = "decimal(15,2)")]
    public decimal? CompanyAmountToEmployee { get; set; }
    [Column(TypeName = "decimal(15,2)")]
    public decimal? CompanyAmountReturnedToEmployer { get; set; }

    [StringLength(1000)]
    public string? Note { get; set; }

    public long RequestedByUserId { get; set; }
    public DateTime RequestedDate { get; set; } = DateTime.Now;

    public virtual Hremployee Hremployee { get; set; } = null!;
    public virtual Pay_ProvidentFundPolicy Policy { get; set; } = null!;
    public virtual Pay_ProvidentFundExitReasonRule ExitReasonRule { get; set; } = null!;
}
