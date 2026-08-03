using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Company-defined group insurance plan (health/life/accident/other) —
// master data for Pay_EmployeeInsuranceEnrollment. Unlike Pay_ProvidentFundElection
// (which needs no plan master since there's only one PF scheme per company,
// just a contribution-rate override), insurance needs a real plan catalog
// because premiums vary by provider/coverage tier, not just by employee.
// Default*Amount are flat monthly amounts (not rates like PF) — matches how
// group insurance is actually billed: a fixed premium per enrolled person,
// not a percentage of salary.
[Table("Pay_InsurancePlan")]
public class Pay_InsurancePlan
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(6)]
    public string CompanyId { get; set; } = null!;

    [Required, StringLength(20)]
    public string PlanCode { get; set; } = null!;

    [Required, StringLength(200)]
    public string PlanName { get; set; } = null!;

    public Pay_InsurancePlanType PlanType { get; set; } = Pay_InsurancePlanType.Health;

    [StringLength(200)]
    public string? Provider { get; set; }

    // Group policy number the insurer issues to the whole company for this plan
    // (เลขกรมธรรม์กลุ่ม) — distinct from Pay_EmployeeInsuranceEnrollment.MemberCertificateNumber,
    // which is the per-employee certificate number under this same group policy.
    [StringLength(50)]
    public string? GroupPolicyNumber { get; set; }

    [Column(TypeName = "decimal(15,2)")]
    public decimal? CoverageAmount { get; set; }

    [Column(TypeName = "decimal(15,2)")]
    public decimal DefaultEmployeeAmount { get; set; }
    [Column(TypeName = "decimal(15,2)")]
    public decimal DefaultCompanyAmount { get; set; }

    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    // Auto-enroll eligibility criteria — null/empty means "unrestricted" for that dimension.
    // CSV of Hremployee.EmptypeCode values (e.g. "01,02") — see EmployeeCoreFieldsForm.razor for the code list.
    [StringLength(100)]
    public string? EligibleEmploymentTypes { get; set; }
    public int? MinTenureDays { get; set; }
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }

    // Master switch — the "Sync สิทธิ์ตอนนี้" button only works on plans with this on.
    public bool AutoEnrollEnabled { get; set; }

    public virtual ICollection<Pay_EmployeeInsuranceEnrollment> Pay_EmployeeInsuranceEnrollments { get; set; } = new List<Pay_EmployeeInsuranceEnrollment>();
}
