using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// One Individual Development Plan for one employee for one Year. Approval is
// gated by the generic Workflow Engine (JobMasterId) — Status is only synced
// from the job lazily, on read (IdpPlanService.SyncStatusFromJobAsync),
// mirroring Services/Perf/PerfApprovalService.cs and
// Services/Org/OrgChangeRequestService.cs exactly (no scheduler exists
// anywhere in this codebase).
[Table("Idp_Plan")]
public class Idp_Plan
{
    [Key]
    public long Id { get; set; }

    public long HremployeeId { get; set; }

    public int Year { get; set; }

    public IdpPlanStatus Status { get; set; } = IdpPlanStatus.Draft;

    [Column(TypeName = "nvarchar(max)")]
    public string? Summary { get; set; }

    public long? JobMasterId { get; set; }

    public long CreatedByUserId { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public DateTime? SubmittedDate { get; set; }

    public DateTime? ApprovedDate { get; set; }
}
