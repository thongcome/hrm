using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Per-employee enrollment into a Pay_InsurancePlan — same effective-dated
// override shape as Pay_ProvidentFundElection, but amounts are snapshotted
// from the plan's defaults at enrollment time (not re-read live) so a later
// change to the plan's premium doesn't retroactively change what an already
// enrolled employee is being charged — matches the snapshot convention used
// for bank details/cost center on Pay_PayrollEmployee elsewhere in this
// codebase. HR can still override the amounts per employee at enrollment
// (e.g. a dependent-coverage tier costs more) before saving.
[Table("Pay_EmployeeInsuranceEnrollment")]
public class Pay_EmployeeInsuranceEnrollment
{
    [Key]
    public long Id { get; set; }

    public long HremployeeId { get; set; }
    public long PlanId { get; set; }

    [Column(TypeName = "decimal(15,2)")]
    public decimal EmployeeAmount { get; set; }
    [Column(TypeName = "decimal(15,2)")]
    public decimal CompanyAmount { get; set; }

    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }

    public bool IsActive { get; set; } = true;

    public long EnrolledByUserId { get; set; }
    public DateTime EnrolledDate { get; set; } = DateTime.Now;

    // True only for rows created by the auto-enroll "Sync" button — HR hasn't
    // looked at this specific enrollment yet. Deliberately separate from IsActive:
    // this row is already live/deducting payroll the moment it's created, this
    // flag is purely an HR triage aid, never read by PayrollCalculationService.
    public bool NeedsReview { get; set; }

    // Per-employee member certificate number under the plan's GroupPolicyNumber.
    [StringLength(50)]
    public string? MemberCertificateNumber { get; set; }

    [StringLength(200)]
    public string? BeneficiaryName { get; set; }
    [StringLength(100)]
    public string? BeneficiaryRelationship { get; set; }

    public DateOnly? CardIssueDate { get; set; }
    public DateOnly? CardExpiryDate { get; set; }

    [StringLength(1000)]
    public string? Remark { get; set; }

    public virtual Hremployee Hremployee { get; set; } = null!;
    public virtual Pay_InsurancePlan Pay_InsurancePlan { get; set; } = null!;
}
