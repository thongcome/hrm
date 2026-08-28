namespace HRM.Services.Workflow;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Once a workflow is Active it is a live, in-force approval contract — HR's
// own rule (2026-08-28) is that NOTHING about it may be edited from that
// point on: not the header, not its approval levels, not any of the
// approver-assignment tables underneath a level (custom user/role, LOA
// bands, LOA users, adhoc users). The only sanctioned mutation on an active
// workflow is deactivating it (see WfWorkflowAdmin.razor's DeactivateAsync,
// a dedicated single-purpose action — never a side effect of a general
// field-by-field edit form). To change the shape of an active workflow, HR
// must deactivate it and build a brand new wf_workflow instead of resurrecting
// or editing the old one — this keeps a workflow's definition a stable,
// auditable contract for the whole time it's actually running jobs.
//
// Every write path (raw EF-scaffold Create/Edit/Delete pages, the hand-built
// CrudScaffold-based admin pages, and the Canvas designer's bulk level
// reorder) must call the matching Ensure*EditableAsync guard before writing.
public static class WorkflowLockGuard
{
    public const string LockedMessage =
        "Workflow นี้เปิดใช้งานอยู่ (Active) — แก้ไข/เพิ่ม/ลบไม่ได้ทุกกรณี หากต้องการเปลี่ยนแปลง ให้ปิดใช้งาน (ยกเลิก) workflow นี้ก่อน แล้วสร้าง workflow ใหม่แทน";

    public static async Task EnsureWorkflowEditableAsync(HRMContext context, long workflowId, CancellationToken ct = default)
    {
        var isActive = await context.wf_workflows
            .Where(w => w.workflowid == workflowId)
            .Select(w => w.isactive)
            .FirstOrDefaultAsync(ct);
        if (isActive == true) throw new InvalidOperationException(LockedMessage);
    }

    // wf_loa keys its owning workflow as nowWorkflowid, not workflowid — every
    // other child table (wf_sub_workflow_master, wf_custom_user,
    // wf_custom_role, wf_adhoc_user, wf_loa_user) has a plain `workflowid`
    // column already on the row, so callers for those can go straight to
    // EnsureWorkflowEditableAsync with that value — no extra lookup needed.
    public static Task EnsureLoaEditableAsync(HRMContext context, long nowWorkflowId, CancellationToken ct = default)
        => EnsureWorkflowEditableAsync(context, nowWorkflowId, ct);
}
