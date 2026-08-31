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

    // Superseded by AttachmentDocName/AttachmentMinDays (which name the
    // required document and let it kick in only from a minimum duration).
    // Column kept so no data is lost and old migrations stay valid, but no
    // new logic may read it — the 2026-08-31 rule migration copies its
    // intent into AttachmentDocName.
    [Obsolete("Use AttachmentDocName/AttachmentMinDays instead — kept only so the column and historical data survive.")]
    public bool RequiresMedicalCert { get; set; }

    public bool AllowHalfDay { get; set; } = true;

    // ต้องลาต่อเนื่องเป็นช่วงเดียว (maternity/ordination/military style).
    // A single date-range request is inherently consecutive, so the only
    // enforceable rule is "no half-day" — LeaveRequestService guards that.
    public bool MustBeConsecutive { get; set; }

    // How often the entitlement renews — see LeaveEntitlementFrequency.
    // Non-PerYear types are not capped/tracked by the yearly balance.
    public LeaveEntitlementFrequency EntitlementFrequency { get; set; } = LeaveEntitlementFrequency.PerYear;

    // Working-day count (default, existing LeaveDayCalculator path) vs plain
    // inclusive calendar-day count — see LeaveDayCountMethod.
    public LeaveDayCountMethod DayCountMethod { get; set; } = LeaveDayCountMethod.WorkingDays;

    // false = start date must not be before today (ลาย้อนหลังไม่ได้).
    public bool AllowRetroactive { get; set; } = true;

    // Minimum advance notice in days: submission is rejected when the start
    // date is earlier than today + N. Null = no notice requirement.
    public int? AdvanceNoticeDays { get; set; }

    // Name of the document that must be attached before the request can be
    // submitted, e.g. "ใบรับรองแพทย์", "หมายเรียก". Null = no attachment
    // requirement. Supersedes RequiresMedicalCert.
    [StringLength(100)]
    public string? AttachmentDocName { get; set; }

    // When AttachmentDocName is set: the attachment is only mandatory once
    // the request's duration reaches this many days (e.g. sick = 3 → a 1-2
    // day sick leave needs no certificate). Null = required from day 1.
    [Column(TypeName = "decimal(5,1)")]
    public decimal? AttachmentMinDays { get; set; }

    // MudBlazor icon constant name (e.g. "BeachAccess") — see
    // LeaveIconCatalog.cs for the curated picker list and the fallback used
    // when null or not recognized. Drives the icon-card leave-type picker in
    // LeaveRequestList.razor.
    [StringLength(100)]
    public string? IconName { get; set; }

    public int SortOrder { get; set; }

    // Soft block, never hard delete — a type referenced by any
    // Lve_LeavePolicy/Lve_LeaveRequest history must stay intact.
    public bool IsActive { get; set; } = true;
}
