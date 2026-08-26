using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Audit trail for every computed suggestion this module makes (rate-matrix
// lookup, exit vesting ruling) — the actual number that flows into payroll
// (Pay_ProvidentFundElection.CompanyContributionRate) or the exit case's
// ComputedVestingPercent is always the single final value; this table
// exists purely so HR/an auditor can later see WHY that number was what it
// was, without needing to recompute it by hand. Exactly one of
// RateChangeRequestId/ExitCaseId is set per row.
[Table("Pay_ProvidentFundCalculationDetail")]
public class Pay_ProvidentFundCalculationDetail
{
    [Key]
    public long Id { get; set; }

    public ProvidentFundCalculationType CalculationType { get; set; }

    public long? RateChangeRequestId { get; set; }
    public long? ExitCaseId { get; set; }

    [Required, StringLength(1000)]
    public string InputsSummary { get; set; } = null!;

    [StringLength(500)]
    public string? MatchedRuleDescription { get; set; }

    [Column(TypeName = "decimal(15,2)")]
    public decimal ResultValue { get; set; }

    public DateTime CalculatedDate { get; set; } = DateTime.Now;

    public virtual Pay_ProvidentFundRateChangeRequest? RateChangeRequest { get; set; }
    public virtual Pay_ProvidentFundExitCase? ExitCase { get; set; }
}
