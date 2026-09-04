using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

// A request to change a workflow's active state (deactivate / reactivate a
// wf_workflow). Because a business workflow is load-bearing for the whole
// company — deactivating LEAVE_APPROVAL once blocked every employee from
// filing leave — this can no longer be a one-click toggle: the request routes
// through the generic WorkflowEngine (the WF_STATE_CHANGE workflow, whose
// approvers are configured like any other, not hardcoded) and the actual
// isactive flip is applied only after approval, via the same lazy
// apply-on-read pattern Org_OrganizationChangeRequest uses.
[Table("Wf_WorkflowStateChangeRequest")]
[Index(nameof(TargetWorkflowId))]
public class Wf_WorkflowStateChangeRequest
{
    [Key]
    public long Id { get; set; }

    // The wf_workflow whose isactive is being changed.
    public long TargetWorkflowId { get; set; }

    [StringLength(50)]
    public string? SnapshotWorkflowCode { get; set; }
    [StringLength(200)]
    public string? SnapshotWorkflowName { get; set; }

    // The requested target state: false = deactivate, true = reactivate.
    public bool SetActive { get; set; }

    [Required, StringLength(500)]
    public string Reason { get; set; } = null!;

    // Set once the request enters the workflow engine.
    public long? JobMasterId { get; set; }

    // Flipped true after approval has been applied to the target workflow —
    // mirrors Org_OrganizationChangeRequest.IsApplied so apply-on-read is idempotent.
    public bool IsApplied { get; set; }
    public DateTime? AppliedDate { get; set; }

    public long RequestedByUserId { get; set; }
    public DateTime RequestedDate { get; set; } = DateTime.Now;
    public bool IsActive { get; set; } = true;
}
