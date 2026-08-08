using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// One employee's registration to one course session. Approval (when the
// course requires it) is gated by the generic Workflow Engine
// (JobMasterId) — Status is only synced from the job lazily, on read
// (LmsEnrollmentService.SyncStatusFromJobAsync), mirroring
// IdpPlanService/PerfApprovalService exactly (no scheduler exists
// anywhere in this codebase). SourceDevelopmentActionId is an optional
// soft-link back to an IDP development action this enrollment fulfills.
[Table("Lms_Enrollment")]
public class Lms_Enrollment
{
    [Key]
    public long Id { get; set; }

    public long CourseSessionId { get; set; }

    public long HremployeeId { get; set; }

    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.PendingApproval;

    public DateTime EnrolledDate { get; set; } = DateTime.Now;

    public long RequestedByUserId { get; set; }

    public long? JobMasterId { get; set; }

    public DateTime? ApprovedDate { get; set; }

    public DateTime? AttendedDate { get; set; }

    public DateTime? CompletedDate { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? QuizScorePercent { get; set; }

    public long? SourceDevelopmentActionId { get; set; }
}
