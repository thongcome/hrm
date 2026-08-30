using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// An employee's elected use of one Pay_TaxDeductionType category — same
// HremployeeId/IsActive/elected-by-audit-trail shape as
// Pay_ProvidentFundElection, with two additions: AnnualAmount (how much they
// elect, validated against the type's MaxAmountPerYear) and ApplyMonthly.
//
// ApplyMonthly is the direct answer to "จ่ายก่อนขอคืน vs จ่ายให้น้อยสุด":
//   true  = "จ่ายให้น้อยสุด" — factored into PayrollCalculationService's
//           monthly withholding calculation right away (AnnualAmount/12 added
//           to that period's flat deduction), so less tax withheld starting
//           this period.
//   false = "จ่ายก่อนขอคืน" — recorded here for the employee's own records
//           but NOT included in the monthly withholding calc; the employee
//           reclaims it themselves when they file their own annual ภ.ง.ด.90/91.
[Table("Pay_EmployeeTaxDeductionElection")]
public class Pay_EmployeeTaxDeductionElection
{
    [Key]
    public long Id { get; set; }

    public long HremployeeId { get; set; }

    public int DeductionTypeId { get; set; }

    [Column(TypeName = "decimal(15,2)")]
    public decimal AnnualAmount { get; set; }

    public bool ApplyMonthly { get; set; }

    public bool IsActive { get; set; } = true;

    [StringLength(500)]
    public string? Note { get; set; }

    public long ElectedByUserId { get; set; }
    public DateTime ElectedDate { get; set; } = DateTime.Now;

    public virtual Hremployee Hremployee { get; set; } = null!;
    public virtual Pay_TaxDeductionType Pay_TaxDeductionType { get; set; } = null!;
}
