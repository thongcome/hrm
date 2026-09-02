using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// The scope a welfare-entitlement rule applies to. Resolution is most-specific-
// wins: an Employee rule beats a Position rule beats the company-wide All rule
// (which itself falls back to the Wel_BenefitType's own default limits) — the
// same longest-prefix idea as the access-control check.
public enum WelfareEntitlementScope
{
    All = 0,       // ทุกคนในบริษัท (ค่าเริ่มต้น)
    Position = 1,  // ตามตำแหน่ง (Pos_ExecType)
    Employee = 2,  // เจาะจงรายบุคคล (กรณีพิเศษ)
}

// An override rule saying WHO gets HOW MUCH of a welfare benefit — the layer
// that lets the same benefit differ by person: a company default, a per-
// position amount (e.g. ค่ารถ ผู้จัดการ 8,000 / พนักงาน 3,000), and individual
// special cases, all for one Wel_BenefitType. Without this the catalog gives
// everyone the flat Wel_BenefitType limit.
//
// Only the fields relevant to the chosen Scope are set (PosExecTypeId for
// Position, HremployeeId for Employee; neither for All). An override field left
// null means "inherit from the less-specific level" — so a rule can override
// just the amount and leave the claim-count cap as the benefit-type default.
public class Wel_Entitlement
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(6)]
    public string CompanyId { get; set; } = null!;

    public long BenefitTypeId { get; set; }
    public virtual Wel_BenefitType BenefitType { get; set; } = null!;

    public WelfareEntitlementScope Scope { get; set; }

    // Set when Scope == Position — the Pos_ExecType this rule applies to.
    // Soft link (no FK/nav), same convention as Job_CompetencyRequirement.
    public long? PosExecTypeId { get; set; }

    // Set when Scope == Employee — the specific employee this rule applies to.
    public long? HremployeeId { get; set; }

    // Override amount, reinterpreted per BenefitType.EntitlementMode (annual
    // limit / per-event limit). Null = inherit the less-specific level's amount.
    [Column(TypeName = "decimal(15,2)")]
    public decimal? OverrideAmount { get; set; }

    // Null = inherit the less-specific level's claim-count cap.
    public int? OverrideMaxClaimsPerYear { get; set; }

    // The reason for this rule — especially a negotiated individual override
    // ("ต่อรองเพิ่มค่ารถเป็น 10,000 ตามที่ตกลง 2 ก.ย. 69"). Kept as a visible
    // record on the rule itself, beyond the automatic AuditLog change history.
    [StringLength(300)]
    public string? Note { get; set; }

    // Who last set/adjusted this rule and when — surfaced on the rule row so a
    // per-person override is auditable at a glance (the generic AuditLog still
    // keeps the full immutable who/what/when history of every change besides).
    public long? SetByUserId { get; set; }
    public DateTime SetDate { get; set; } = DateTime.Now;

    public bool IsActive { get; set; } = true;
}
