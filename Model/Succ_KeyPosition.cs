using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// A Job (Pos_ExecType — the reusable role, not a specific headcount seat)
// that HR/leadership has flagged as business-critical for succession
// purposes. PosExecTypeId is a soft link, matching the convention already
// used across Pos_ExecType/Job_CompetencyRequirement. Business Impact and
// Replacement Difficulty are the two standard axes international succession
// frameworks use — distinct from Pos_ExecType.IsBoss/IsTopLevel, which are
// org-chart flags, not succession-criticality flags.
[Table("Succ_KeyPosition")]
public class Succ_KeyPosition
{
    [Key]
    public long Id { get; set; }

    // Stable human-facing code for this record (audit: every master/document table needs one beyond the surrogate Id).
    [StringLength(30)]
    public string? Code { get; set; }

    [Required, StringLength(6)]
    public string CompanyId { get; set; } = null!;

    public long PosExecTypeId { get; set; }

    public BusinessImpactLevel BusinessImpact { get; set; }
    public ReplacementDifficultyLevel ReplacementDifficulty { get; set; }

    // Optional eligibility config, config-first per house convention (this
    // legitimately differs per business/position, so it's a data field HR
    // sets, not a hardcoded threshold). Null = no experience floor applied.
    // Total years is computed at read time from Hrd_Experience + tenure —
    // never persisted per employee (same "don't persist a derived %" stance
    // as Okr_Objective progress elsewhere in this codebase).
    public int? MinYearsExperience { get; set; }

    [StringLength(1000)]
    public string? Note { get; set; }

    public bool IsActive { get; set; } = true;

    public long AddedByUserId { get; set; }
    public DateTime AddedDate { get; set; } = DateTime.Now;
}
