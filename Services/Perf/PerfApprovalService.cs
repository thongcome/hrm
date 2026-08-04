using HRM.Models;
using HRM.Services.Workflow;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Perf;

// Sends a finalized evaluation instance (FinalScorePercent/FinalGrade
// already computed by PerfScoringService.RecomputeInstanceScoreAsync) into
// the generic workflow engine for HR approval — mirrors
// Services/Org/OrgChangeRequestService.cs's SubmitAsync exactly. No
// scheduler/completion-hook exists anywhere in this codebase (confirmed:
// WorkflowEngineService never calls back into a source table), so — like
// OrgChangeRequestService.ApplyDueChangesAsync — instance.Status is only
// synced from the job lazily, on read, not pushed on completion.
public class PerfApprovalService(IDbContextFactory<HRMContext> dbFactory, WorkflowEngineService engine)
{
    public async Task<long> SubmitForApprovalAsync(long instanceId, long requesterUserId, string? requesterEmpId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var instance = await context.Perf_EvaluationInstances.FirstOrDefaultAsync(i => i.Id == instanceId, ct)
            ?? throw new InvalidOperationException("ไม่พบรายการประเมินนี้แล้ว");

        if (instance.Status != PerfInstanceStatus.PendingApproval)
            throw new InvalidOperationException("ส่งอนุมัติได้เฉพาะรายการที่คำนวณคะแนนรวมเสร็จแล้วเท่านั้น (ต้องรอทุกผู้ประเมินให้คะแนนครบก่อน)");
        if (instance.JobMasterId is not null)
            throw new InvalidOperationException("รายการนี้ถูกส่งอนุมัติไปแล้ว");

        var workflow = await context.wf_workflows.FirstOrDefaultAsync(w => w.workflowcode == "PERF_EVAL_APPROVAL", ct)
            ?? throw new InvalidOperationException("ไม่พบ workflow 'PERF_EVAL_APPROVAL' — ติดต่อแอดมินให้ตั้งค่าก่อน");
        if (workflow.isactive != true)
            throw new InvalidOperationException($"workflow '{workflow.wname}' ปิดใช้งานอยู่ ไม่สามารถส่งอนุมัติได้");

        var subject = $"ผลการประเมิน: {instance.SnapshotEmpName ?? $"#{instance.HremployeeId}"} — เกรด {instance.FinalGrade} ({instance.FinalScorePercent:0.#}%)";
        var jobId = await engine.StartJobAsync(workflow.workflowid, "Perf_EvaluationInstance", instance.Id.ToString(),
            requesterUserId, requesterEmpId, subject, amount: null, ct);

        instance.JobMasterId = jobId;
        await context.SaveChangesAsync(ct);

        return jobId;
    }

    // Lazy apply-on-read: called from the instance detail page on every
    // load. A no-op unless the instance is still PendingApproval with a job
    // that has since closed — matches OrgChangeRequestService.ApplyDueChangesAsync's
    // isJobClosed/StatusCompleted check exactly.
    public async Task SyncStatusFromJobAsync(long instanceId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var instance = await context.Perf_EvaluationInstances.FirstOrDefaultAsync(i => i.Id == instanceId, ct);
        if (instance is null || instance.Status != PerfInstanceStatus.PendingApproval || instance.JobMasterId is null)
            return;

        var job = await context.job_masters.FirstOrDefaultAsync(j => j.jobmasterid == instance.JobMasterId, ct);
        if (job is null || job.isJobClosed != true)
            return;

        instance.Status = job.status == WorkflowEngineService.StatusCompleted
            ? PerfInstanceStatus.Approved
            : PerfInstanceStatus.Rejected;

        await context.SaveChangesAsync(ct);
    }
}
