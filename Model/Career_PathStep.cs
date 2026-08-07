using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// One rung on the career ladder for a Job Family — an ordered sequence of
// Pos_ExecType (Job) steps HR expects people to progress through within that
// family (e.g. Finance: Analyst -> Senior Analyst -> Manager). References a
// concrete Pos_ExecType (not just a Job_Level) so CareerPathService can feed
// the "next step" straight into IdpAssessmentService's existing
// GetGapAnalysisForTargetPositionAsync — no separate gap-analysis logic
// needed for career progression.
[Table("Career_PathStep")]
public class Career_PathStep
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(6)]
    public string CompanyId { get; set; } = null!;

    public long JobFamilyId { get; set; }
    public long PosExecTypeId { get; set; }

    public int SortOrder { get; set; }
}
