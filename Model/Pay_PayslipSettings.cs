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
    // generator must stay globally unique, not just per company. Total
    // length (prefix + digits) must fit Hremployee.EmpNo's StringLength(6).
    [StringLength(20)]
    public string? EmpCodePrefix { get; set; }
    public int EmpCodeDigits { get; set; } = 3;

    // Null = derive the next number by scanning existing EmpNo values
    // matching the prefix (i.e. "continue" automatically from real data).
    // Set = use this value explicitly and advance it by 1 after each
    // employee created with an auto-generated code (i.e. an admin chose to
    // "start over" from a specific number via the format settings page).
    public int? EmpCodeNextNumber { get; set; }

    // 1 = calendar year (Jan-Dec, default). Set e.g. 4 for a Thai
    // government-style fiscal year (Apr-Mar). Drives the Payroll Dashboard's
    // year-to-date window and period grouping.
    public int FiscalYearStartMonth { get; set; } = 1;

    // Calendar days (not business days) from Hremployee.WorkDate to the
    // suggested probation end date — Thai employers commonly use 119 days
    // to stay just under the Section 118 (120-day) severance threshold.
    // HR can still override the suggested date per employee.
    public int DefaultProbationDays { get; set; } = 119;

    // Initial/reset login password for newly-created (or reset) sc_user +
    // ApplicationUser accounts = Part1 + Part2 concatenated verbatim (e.g.
    // "Abcd" + "2025" -> "Abcd2025"). Company-wide and static — unlike
    // PasswordTemplate above (which derives a per-employee value from
    // birthdate/ID-card and is used only for payslip PDF passwords), this
    // never depends on employee data being complete, so it can't block
    // account creation the way the old formula-based attempt did.
    [StringLength(50)]
    public string? DefaultPasswordPart1 { get; set; }
    [StringLength(50)]
    public string? DefaultPasswordPart2 { get; set; }

    public DateTime ModifiedDate { get; set; } = DateTime.Now;
    public long? ModifiedByUserId { get; set; }
}
