using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// One row per (employee, competency, source) — mutable/upserted by
// IdpAssessmentService, not an append-only history log (HRMContext.Audit.cs
// already captures every change automatically). Source distinguishes the
// employee's own self-rating from their manager's rating; IdpAssessmentService
// prefers Manager over Self when computing the effective level for gap
// analysis (the manager has the final say when both exist).
[Table("Idp_CompetencyAssessment")]
public class Idp_CompetencyAssessment
{
    [Key]
    public long Id { get; set; }

    public long HremployeeId { get; set; }

    public long CompetencyId { get; set; }

    public IdpAssessmentSource Source { get; set; }

    [Range(1, 5)]
    public int RatedLevel { get; set; }

    public long RatedByUserId { get; set; }

    public DateTime RatedDate { get; set; } = DateTime.Now;

    [StringLength(500)]
    public string? Note { get; set; }

    public virtual Comp_Competency Competency { get; set; } = null!;
}
