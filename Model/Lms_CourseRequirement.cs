using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// HR-authored rule: "this course is mandatory for X". PosExecTypeId is a
// soft link (no navigation property, resolved by manual query — same
// convention as Job_CompetencyRequirement.PosExecTypeId and
// Pos_PositionSlot.PosExecTypeId) — null means "every new hire, regardless
// of position", a non-null value scopes it to that one position/job title.
// LmsMandatoryTrainingHelper.SyncAssignmentsAsync turns matching rows here
// into Lms_MandatoryAssignment rows per employee; this table itself never
// changes once an employee has been assigned — editing/deactivating a rule
// doesn't retroactively touch assignments already handed out (same
// snapshot-on-assignment principle as Hrd_LifecycleTaskTemplate).
[Table("Lms_CourseRequirement")]
public class Lms_CourseRequirement
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(6)]
    public string CompanyId { get; set; } = null!;

    public long CourseId { get; set; }

    public long? PosExecTypeId { get; set; }

    public bool IsActive { get; set; } = true;

    public long CreatedByUserId { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;
}
