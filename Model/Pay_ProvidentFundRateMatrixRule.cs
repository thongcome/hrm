using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Company-configurable table for auto-suggesting the employer contribution
// rate from (years of service x employee's own chosen rate band) — matches
// real fund regulations that key the employer match off both dimensions at
// once (e.g. "<=20 years service + employee contributes 2-10% -> employer
// pays a flat 10%"; "<=20 years + employee contributes 11-15% -> employer
// matches the employee's rate exactly"). This only produces a SUGGESTION —
// HR still reviews/approves the actual request (Pay_ProvidentFundRateChangeRequest),
// it does not silently determine payroll deductions on its own.
[Table("Pay_ProvidentFundRateMatrixRule")]
public class Pay_ProvidentFundRateMatrixRule
{
    [Key]
    public long Id { get; set; }

    public long PolicyId { get; set; }

    public int MinYearsOfService { get; set; }
    public int? MaxYearsOfService { get; set; } // null = no upper bound

    [Column(TypeName = "decimal(5,2)")]
    public decimal EmployeeRateMin { get; set; }
    [Column(TypeName = "decimal(5,2)")]
    public decimal EmployeeRateMax { get; set; }

    public ProvidentFundMatrixResultType ResultType { get; set; }

    // Used only when ResultType == Fixed; null when ResultType == MatchEmployeeRate.
    [Column(TypeName = "decimal(5,2)")]
    public decimal? FixedCompanyRate { get; set; }

    public int SortOrder { get; set; }

    public virtual Pay_ProvidentFundPolicy Policy { get; set; } = null!;
}
