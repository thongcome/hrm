using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// One line item of a Job Description's "หน้าที่ความรับผิดชอบ" (duties /
// key accountabilities) for a Pos_ExecType — the structured replacement for
// the legacy Pos_ExecType.KeyAccountabilities single-text blob (CEO order,
// 2026-09-01: duties become weighted line items instead of free text).
// The blob column is deliberately kept on Pos_ExecType as a one-way
// fallback: a position with no duty rows yet still displays its old blob,
// but once duty rows exist the blob is ignored (never deleted/migrated
// destructively).
//
// PosExecTypeId is a soft link (no nav) — same convention as
// Job_CompetencyRequirement.PosExecTypeId / Pos_ExecType.EmployeeTypeId.
// LinkedCompetencyId is a real FK with explicit fluent config in
// HRMContext.OnModelCreating (house rule: never rely on nav-property
// auto-convention — see the Perf_Indicator.Competency block there).
public class Job_ProfileDuty
{
    [Key]
    public long Id { get; set; }

    public long PosExecTypeId { get; set; }

    [Required, StringLength(500)]
    public string Text { get; set; } = null!;

    // % weight of this duty within the position (e.g. 25.0). Nullable —
    // HR may list duties without weighting them. Sum ≈ 100 is advisory
    // (soft hint in the UI), not a DB constraint — same convention as the
    // weight-sum checks noted on Job_CompetencyRequirement.
    [Column(TypeName = "decimal(5,1)")]
    public decimal? WeightPercent { get; set; }

    public int SortOrder { get; set; }

    // CEO requirement ("นำไปวัด competency"): when true, this duty is meant
    // to be measured as a competency — HR then picks LinkedCompetencyId and
    // saving upserts a Job_CompetencyRequirement for the position+competency.
    // Unflagging does NOT delete that requirement (it may since have been
    // tuned by HR — see JobProfileDetail.razor's upsert comment).
    public bool IncludeInCompetency { get; set; }

    public long? LinkedCompetencyId { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual Comp_Competency? LinkedCompetency { get; set; }
}
