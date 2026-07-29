using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

// One row per company. Governs how Pay_Payslip PDFs are password-protected.
// The password itself is never stored anywhere (here or on Pay_Payslip) —
// it's re-derived from this template + the employee's own data every time a
// slip is generated or re-opened, via PayslipPasswordService.
[Table("Pay_PayslipSettings")]
[Index(nameof(CompanyId), IsUnique = true)]
public class Pay_PayslipSettings
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(6)]
    public string CompanyId { get; set; } = null!;

    // Supported tokens: {BirthDateDDMMYYYY} {BirthDateDDMMYY} {IdCardLast4} {EmpNo}
    // — see PayslipPasswordService for the resolver.
    [Required, StringLength(200)]
    public string PasswordTemplate { get; set; } = "{BirthDateDDMMYYYY}";

    public DateTime ModifiedDate { get; set; } = DateTime.Now;
    public long? ModifiedByUserId { get; set; }
}
