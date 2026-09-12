using Microsoft.EntityFrameworkCore;

namespace Advance.Workflow.Engine;

// ============================================================================
//  WorkflowLockGuard — ported unchanged from HRM's Services/Workflow/
//  WorkflowLockGuard.cs (only HRMContext -> WorkflowDbContext). No seams:
//  this only ever reads wf_workflow, already in Advance.Workflow.Domain.
// ============================================================================
public static class WorkflowLockGuard
{
    public const string LockedMessage =
        "Workflow นี้เปิดใช้งานอยู่ (Active) — แก้ไข/เพิ่ม/ลบไม่ได้ทุกกรณี หากต้องการเปลี่ยนแปลง ให้ปิดใช้งาน (ยกเลิก) workflow นี้ก่อน แล้วสร้าง workflow ใหม่แทน";

    public static async Task EnsureWorkflowEditableAsync(WorkflowDbContext context, long workflowId, CancellationToken ct = default)
    {
        var isActive = await context.wf_workflows
            .Where(w => w.workflowid == workflowId)
            .Select(w => w.isactive)
            .FirstOrDefaultAsync(ct);
        if (isActive == true) throw new InvalidOperationException(LockedMessage);
    }

    public static Task EnsureLoaEditableAsync(WorkflowDbContext context, long nowWorkflowId, CancellationToken ct = default)
        => EnsureWorkflowEditableAsync(context, nowWorkflowId, ct);
}
