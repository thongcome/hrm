namespace HRM.Services.Workflow;

using HRM.Models;
using HRM.Services.Audit;
using Microsoft.EntityFrameworkCore;

// Turns "deactivate/reactivate a workflow" into an approval request routed
// through the WF_STATE_CHANGE workflow, and applies the change only after that
// approval completes — the same submit/apply-on-read shape as
// OrgChangeRequestService. The actual wf_workflow.isactive flip never happens
// on the button click; it happens in ApplyApprovedAsync once the job is closed
// and COMPLETED.
public class WorkflowStateChangeService(
    IDbContextFactory<HRMContext> dbFactory,
    WorkflowEngineService engine,
    IAuditLogger auditLogger)
{
    // Files a state-change request and starts its approval job. Guards against
    // a duplicate open request for the same target, and won't request a state
    // the workflow is already in.
    public async Task<long> RequestStateChangeAsync(
        long targetWorkflowId, bool setActive, string reason, long requesterUserId, string? requesterEmpId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("กรุณาระบุเหตุผลของการเปลี่ยนสถานะ");

        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var target = await context.wf_workflows.FirstOrDefaultAsync(w => w.workflowid == targetWorkflowId, ct)
            ?? throw new InvalidOperationException("ไม่พบ workflow ที่ต้องการเปลี่ยนสถานะ");
        if ((target.isactive == true) == setActive)
            throw new InvalidOperationException(setActive ? "workflow นี้เปิดใช้งานอยู่แล้ว" : "workflow นี้ปิดใช้งานอยู่แล้ว");

        if (target.workflowcode == WorkflowStateChangeSeeder.WorkflowCode)
            throw new InvalidOperationException("ไม่สามารถเปลี่ยนสถานะ workflow ตัวอนุมัติการเปลี่ยนสถานะเองได้");

        var hasOpen = await context.Wf_WorkflowStateChangeRequests
            .AnyAsync(r => r.TargetWorkflowId == targetWorkflowId && !r.IsApplied && r.JobMasterId != null && r.IsActive, ct);
        if (hasOpen)
            throw new InvalidOperationException("มีคำขอเปลี่ยนสถานะของ workflow นี้ที่รออนุมัติอยู่แล้ว");

        var approvalWorkflow = await context.wf_workflows
            .FirstOrDefaultAsync(w => w.workflowcode == WorkflowStateChangeSeeder.WorkflowCode, ct)
            ?? throw new InvalidOperationException("ยังไม่ได้ตั้งค่า workflow อนุมัติการเปลี่ยนสถานะ (WF_STATE_CHANGE)");

        var request = new Wf_WorkflowStateChangeRequest
        {
            TargetWorkflowId = targetWorkflowId,
            SnapshotWorkflowCode = target.workflowcode,
            SnapshotWorkflowName = target.wname,
            SetActive = setActive,
            Reason = reason.Trim(),
            RequestedByUserId = requesterUserId,
            RequestedDate = DateTime.Now,
            IsApplied = false,
            IsActive = true,
        };
        context.Wf_WorkflowStateChangeRequests.Add(request);
        await context.SaveChangesAsync(ct);

        var action = setActive ? "เปิดใช้งาน" : "ปิดใช้งาน";
        var jobId = await engine.StartJobAsync(
            approvalWorkflow.workflowid, "Wf_WorkflowStateChangeRequest", request.Id.ToString(),
            requesterUserId, requesterEmpId,
            $"ขอ{action} workflow {target.workflowcode} ({target.wname})", null, ct);

        request.JobMasterId = jobId;
        await context.SaveChangesAsync(ct);
        return request.Id;
    }

    // Apply-on-read: any approved-and-closed request whose change hasn't been
    // applied yet flips its target workflow's isactive and records it. Called on
    // the tracking page load (there is no background scheduler in this app).
    // Returns how many were applied.
    public async Task<int> ApplyApprovedAsync(CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var pending = await context.Wf_WorkflowStateChangeRequests
            .Where(r => !r.IsApplied && r.JobMasterId != null && r.IsActive)
            .ToListAsync(ct);
        if (pending.Count == 0) return 0;

        var jobIds = pending.Select(r => r.JobMasterId!.Value).ToList();
        var jobs = await context.job_masters.Where(j => jobIds.Contains(j.jobmasterid)).ToDictionaryAsync(j => j.jobmasterid, ct);

        var applied = 0;
        foreach (var req in pending)
        {
            if (!jobs.TryGetValue(req.JobMasterId!.Value, out var job)) continue;
            if (job.isJobClosed != true || job.status != WorkflowEngineService.StatusCompleted) continue; // still pending / rejected

            var target = await context.wf_workflows.FirstOrDefaultAsync(w => w.workflowid == req.TargetWorkflowId, ct);
            if (target is null) { req.IsApplied = true; req.AppliedDate = DateTime.Now; continue; }

            target.isactive = req.SetActive;
            req.IsApplied = true;
            req.AppliedDate = DateTime.Now;
            applied++;

            await auditLogger.LogAccessAsync("wf_workflow", req.TargetWorkflowId.ToString(), isSensitive: false,
                note: $"state change {(req.SetActive ? "reactivate" : "deactivate")} applied after approval (request {req.Id})");
        }

        await context.SaveChangesAsync(ct);
        return applied;
    }

    public record StateChangeRow(
        long Id, long TargetWorkflowId, string WorkflowCode, string WorkflowName,
        bool SetActive, string Reason, long? JobMasterId, string StatusText, bool IsApplied, DateTime RequestedDate);

    public async Task<List<StateChangeRow>> GetRequestsAsync(CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var rows = await context.Wf_WorkflowStateChangeRequests
            .Where(r => r.IsActive)
            .OrderByDescending(r => r.RequestedDate)
            .ToListAsync(ct);
        if (rows.Count == 0) return new();

        var jobIds = rows.Where(r => r.JobMasterId != null).Select(r => r.JobMasterId!.Value).ToList();
        var jobs = await context.job_masters.Where(j => jobIds.Contains(j.jobmasterid))
            .ToDictionaryAsync(j => j.jobmasterid, j => j.status, ct);

        return rows.Select(r => new StateChangeRow(
            r.Id, r.TargetWorkflowId, r.SnapshotWorkflowCode ?? "-", r.SnapshotWorkflowName ?? "-",
            r.SetActive, r.Reason, r.JobMasterId,
            r.IsApplied ? "ดำเนินการแล้ว"
                : r.JobMasterId is long jid && jobs.TryGetValue(jid, out var s) ? (s ?? "PENDING")
                : "ร่าง",
            r.IsApplied, r.RequestedDate)).ToList();
    }
}
