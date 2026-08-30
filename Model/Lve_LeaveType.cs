using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Config-driven catalog of leave types — HR adds/edits rows at
// /leave-requests/leave-types, no code change needed for a new category.
// Global (no CompanyId), same convention as Pay_TaxDeductionType: a company
// opts into a catalog row by adding a Lve_LeavePolicy for it (see that
// table's comment) rather than the catalog itself being per-company.
[Table("Lve_LeaveType")]
public class Lve_LeaveType
{
    [Key]
    public int Id { get; set; }

    // PascalCase, matches the retired LeaveType enum's member names exactly
    // (e.g. "Sick") — ExternalApiEndpoints.cs's ?leaveType= query param and
    // JSON response echo this Code verbatim, so changing casing here would
    // be a breaking change for that external contract.
    [Required, StringLength(30)]
    public string Code { get; set; } = null!;

    [Required, StringLength(100)]
    public string NameTh { get; set; } = null!;

    [Required, StringLength(100)]
    public string NameEn { get; set; } = null!;

    // True for leave categories grounded in the Thai Labor Protection Act
    // (พ.ร.บ.คุ้มครองแรงงาน) — informational, blocks Code edits in the admin
    // UI but never blocks deactivation (that's a business call for HR).
    public bool IsStatutory { get; set; }

    // e.g. "มาตรา 32" — informational only, not enforced.
    [StringLength(100)]
    public string? LawReference { get; set; }

    // Legal minimum days/year, shown next to a company's actual
    // Lve_LeavePolicy.EntitlementDaysPerYear as a reference warning only.
    [Column(TypeName = "decimal(5,1)")]
    public decimal? StatutoryMinDaysPerYear { get; set; }

    // "M"/"F"/null=all — same single-char convention as Hremployee.Sex.
    [StringLength(1)]
    public string? ApplicableGender { get; set; }

    // null = every country, "TH" = Thailand-only, etc. — generalizes what
    // used to be a hardcoded Ordination-only check in LeaveCountryHelper.
    [StringLength(2)]
    public string? ApplicableCountryCode { get; set; }

    // Drives the medical-certificate-attachment UI in LeaveRequestList.razor.
    public bool RequiresMedicalCert { get; set; }

    public bool AllowHalfDay { get; set; } = true;

    public int SortOrder { get; set; }

    // Soft block, never hard delete — a type referenced by any
    // Lve_LeavePolicy/Lve_LeaveRequest history must stay intact.
    public bool IsActive { get; set; } = true;
}
