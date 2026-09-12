using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Advance.Payroll.Domain;

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

    // "Employee's Choice" investment policy selection — informational only,
    // does not affect the monthly deduction calculation.
    public long? InvestmentPolicyId { get; set; }

    // Mirrors Pay_EmployeeInsuranceEnrollment.EnrolledByUserId/EnrolledDate —
    // the original election table shipped without these, added here for the
    // same accountability trail (who elected/changed this, and when).
    public long ElectedByUserId { get; set; }
    public DateTime ElectedDate { get; set; } = DateTime.Now;

    // TODO(seam): Hremployee navigation removed - Advance.Payroll.Domain does not own the employee entity.
    // HremployeeId (scalar FK, kept above) is resolved against pay_employee via IEmployeeSource. See EXTRACTION-PLAN.md.
    // public virtual Hremployee Hremployee { get; set; } = null!;
    public virtual Pay_ProvidentFundInvestmentPolicy? InvestmentPolicy { get; set; }
}
