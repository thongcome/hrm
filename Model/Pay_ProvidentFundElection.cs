using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Per-employee provident-fund rate override with effective dating.
// If an employee has no active election row, calculation falls back to
// Hremployee.ProvfEmprate / ProvfCorprate so existing employees work as-is.
[Table("Pay_ProvidentFundElection")]
public class Pay_ProvidentFundElection
{
    [Key]
    public long Id { get; set; }

    public long HremployeeId { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal EmployeeContributionRate { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal CompanyContributionRate { get; set; }

    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual Hremployee Hremployee { get; set; } = null!;
}
