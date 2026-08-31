using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// One edge in the career LATTICE: "from position X you can move to position
// Y" (optionally with a note like "ต้องผ่านงานอย่างน้อย 2 ปี"). This
// supersedes the linear Career_PathStep ladder as the source of "where can I
// go from here" — a position can have MULTIPLE outgoing transitions (Analyst
// → Senior Analyst OR Specialist), including cross-family moves, which the
// per-family SortOrder ladder can't express. The family ladder remains for
// the Explorer's per-family overview display; CareerPathService falls back
// to it for positions that have no transitions defined yet.
//
// FromPosExecTypeId/ToPosExecTypeId are soft links to Pos_ExecType (no
// navigation properties), matching the convention used by Career_PathStep,
// Succ_KeyPosition and Pos_ExecType.JobFamilyId itself.
[Table("Career_PathTransition")]
public class Career_PathTransition
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(6)]
    public string CompanyId { get; set; } = null!;

    public long FromPosExecTypeId { get; set; }
    public long ToPosExecTypeId { get; set; }

    // Optional guidance shown to the employee alongside the target position,
    // e.g. "ต้องผ่านงานอย่างน้อย 2 ปี".
    [StringLength(500)]
    public string? Note { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public long CreatedByUserId { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;
}
