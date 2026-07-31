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

    // Shown on the payslip PDF header. Hremployee.companyid/Pay_PayrollRun.CompanyId
    // ("001"-style codes) and com_company.code ("AD"-style codes) are two
    // unrelated identifier spaces with no reliable join between them (same
    // issue flagged in the OWASP A01 review) — rather than bridging that,
    // the display name just lives here against the CompanyId Pay_* already uses.
    [StringLength(250)]
    public string? CompanyName { get; set; }
    [StringLength(250)]
    public string? CompanyNameEn { get; set; }

    // For the withholding tax certificate (50 ทวิ) header — same "no
    // reliable join to com_company" reasoning as CompanyName above, so this
    // lives here too rather than trying to bridge com_company.tax_id.
    [StringLength(13)]
    public string? CompanyTaxId { get; set; }
    [StringLength(500)]
    public string? CompanyAddress { get; set; }

    // Auto-generate formula for Hremployee.EmpNo: EmpCodePrefix + running
    // number zero-padded to EmpCodeDigits (e.g. prefix "EMP", digits 3 ->
    // "EMP001"). EmpNo also doubles as sc_user.loginname now, so the
    // generator must stay globally unique, not just per company.
    [StringLength(20)]
    public string? EmpCodePrefix { get; set; }
    public int EmpCodeDigits { get; set; } = 3;

    public DateTime ModifiedDate { get; set; } = DateTime.Now;
    public long? ModifiedByUserId { get; set; }
}
