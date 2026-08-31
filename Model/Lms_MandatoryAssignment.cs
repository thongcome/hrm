using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// "This employee owes this course" — created once per (employee, course)
// by LmsMandatoryTrainingHelper.SyncAssignmentsAsync when a matching
// Lms_CourseRequirement exists (onboarding start, or a position slot
// assignment/transfer — see EmployeePositionSync.cs and
// LifecycleTaskService.StartOnboardingAsync). Deliberately does NOT carry
// its own status field — completion is a lazy apply-on-read computed by
// LmsMandatoryTrainingService.GetStatusForEmployeeAsync joining out to
// Lms_Enrollment/Lms_CourseSession for this CourseId, the same
// apply-on-read idiom used everywhere else in this codebase (no scheduler
// exists anywhere). This intentionally does NOT force the employee into
// any specific course session — they still pick one via the normal
// EssCourseCatalog enrollment flow; this row only tracks that they must
// complete *some* session of CourseId eventually. Never blocks anything —
// track-only, confirmed with user.
[Table("Lms_MandatoryAssignment")]
public class Lms_MandatoryAssignment
{
    [Key]
    public long Id { get; set; }

    public long HremployeeId { get; set; }

    public long CourseId { get; set; }

    // Soft link back to the rule that created this — informational only,
    // never re-read to decide behavior (see Lms_CourseRequirement's own
    // snapshot-on-assignment note).
    public long? RequirementId { get; set; }

    public DateTime AssignedDate { get; set; } = DateTime.Now;

    // Voided instead of hard-deleted if a requirement no longer applies —
    // never actually flipped false anywhere yet (no "unassign" flow exists
    // today), but keeps the soft-delete convention consistent with the
    // rest of this codebase for when one is needed later.
    public bool IsActive { get; set; } = true;
}
