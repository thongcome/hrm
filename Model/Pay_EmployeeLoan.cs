using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// HR-proxy company loan (เงินกู้พนักงาน) — HR enters the loan on the
// employee's behalf and the system auto-deducts equal installments each
// payroll period. Deliberately a NEW table rather than reusing
// KPTEMPRECEIVE/KPTEMPRECEIVEDET (the legacy cooperative
// savings/shares/loan system used by LoanDeductionCalculator's other
// pathway) — that schema is overloaded with cooperative-specific fields
// (shares, interest arrears, posting status) and links to Hremployee only
// via a soft string match (RefMembno), not a real FK. No interest is
// charged here; splitting evenly across TotalInstallments matches how
// severance/adhoc items in this codebase start simple and stay that way
// until a real requirement for interest appears.
[Table("Pay_EmployeeLoan")]
public class Pay_EmployeeLoan
{
    [Key]
    public long Id { get; set; }

    public long HremployeeId { get; set; }

    [StringLength(6)]
    public string? EmpNo { get; set; }
    [StringLength(6)]
    public string? CompanyId { get; set; }

    [Column(TypeName = "decimal(15,2)")]
    public decimal PrincipalAmount { get; set; }

    [Column(TypeName = "decimal(15,2)")]
    public decimal InstallmentAmount { get; set; }

    public int TotalInstallments { get; set; }

    // snapshot of principal remaining — updated as installments are
    // consumed by payroll calculation; PrincipalAmount itself never changes
    [Column(TypeName = "decimal(15,2)")]
    public decimal RemainingBalance { get; set; }

    // "YYYYMM", Gregorian — first period the first installment is deducted
    [Required, StringLength(6)]
    public string StartPeriod { get; set; } = null!;

    [StringLength(500)]
    public string? Reason { get; set; }

    public Pay_EmployeeLoanStatus Status { get; set; } = Pay_EmployeeLoanStatus.Active;

    public long RequestedByUserId { get; set; }
    public DateTime RequestedDate { get; set; } = DateTime.Now;

    public virtual Hremployee Hremployee { get; set; } = null!;
    public virtual ICollection<Pay_EmployeeLoanInstallment> Pay_EmployeeLoanInstallments { get; set; } = new List<Pay_EmployeeLoanInstallment>();
}
