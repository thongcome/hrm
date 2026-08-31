namespace HRM.Services.Idp;

using HRM.Models;
using HRM.Services.Workflow;
using Microsoft.EntityFrameworkCore;

// Development-plan header + action CRUD, plus submission into the generic
// Workflow Engine and lazy status sync — mirrors
// Services/Perf/PerfApprovalService.cs's SubmitForApprovalAsync /
// SyncStatusFromJobAsync pair exactly (no scheduler exists anywhere in this
// codebase, so status is only ever pulled from the job on read).
public class IdpPlanService(IDbContextFactory<HRMContext> dbFactory, WorkflowEngineService engine)
{
    public async Task<long> CreateDraftAsync(long hremployeeId, int year, long actorUserId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var exists = await context.Idp_Plans.AnyAsync(p => p.HremployeeId == hremployeeId && p.Year == year && p.Status != IdpPlanStatus.Rejected, ct);
        if (exists)
            throw new InvalidOperationException($"มีแผนพัฒนาปี {year} ของพนักงานคนนี้อยู่แล้ว");

        var plan = new Idp_Plan { HremployeeId = hremployeeId, Year = year, CreatedByUserId = actorUserId };
        context.Idp_Plans.Add(plan);
        await context.SaveChangesAsync(ct);
        return plan.Id;
    }

    // `method` (70-20-10) is optional and placed after actorUserId so existing
    // callers that don't classify (e.g. Succession's quick gap-to-action) keep
    // compiling unchanged and simply leave it null ("ยังไม่ระบุ").
    public async Task AddActionAsync(long planId, long? competencyId, string title, string? description, DateOnly? targetDate, long actorUserId, IdpDevelopmentMethod? method = null, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var plan = await context.Idp_Plans.FirstOrDefaultAsync(p => p.Id == planId, ct)
            ?? throw new InvalidOperationException("ไม่พบแผนพัฒนานี้แล้ว");
        if (plan.Status != IdpPlanStatus.Draft)
            throw new InvalidOperationException("แก้ไขรายการได้เฉพาะแผนที่ยังเป็นฉบับร่าง (Draft) เท่านั้น");

        var sortOrder = await context.Idp_DevelopmentActions.CountAsync(a => a.PlanId == planId, ct);
        context.Idp_DevelopmentActions.Add(new Idp_DevelopmentAction
        {
            PlanId = planId,
            CompetencyId = competencyId,
            Title = title,
            Description = description,
            TargetDate = targetDate,
            Method = method,
            SortOrder = sortOrder,
        });
        await context.SaveChangesAsync(ct);
    }

    public async Task UpdateActionStatusAsync(long actionId, IdpActionStatus status, string? note, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var action = await context.Idp_DevelopmentActions.FirstOrDefaultAsync(a => a.Id == actionId, ct)
            ?? throw new InvalidOperationException("ไม่พบรายการนี้แล้ว");

        action.Status = status;
        action.Note = note;
        action.CompletedDate = status == IdpActionStatus.Completed ? DateTime.Now : null;
        await context.SaveChangesAsync(ct);
    }

    public async Task RemoveActionAsync(long actionId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var action = await context.Idp_DevelopmentActions.Include(a => a.Plan).FirstOrDefaultAsync(a => a.Id == actionId, ct);
        if (action is null)
            return;
        if (action.Plan.Status != IdpPlanStatus.Draft)
            throw new InvalidOperationException("ลบรายการได้เฉพาะแผนที่ยังเป็นฉบับร่าง (Draft) เท่านั้น");

        context.Idp_DevelopmentActions.Remove(action);
        await context.SaveChangesAsync(ct);
    }

    public async Task<long> SubmitForApprovalAsync(long planId, long requesterUserId, string? requesterEmpId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var plan = await context.Idp_Plans.FirstOrDefaultAsync(p => p.Id == planId, ct)
            ?? throw new InvalidOperationException("ไม่พบแผนพัฒนานี้แล้ว");
        if (plan.Status != IdpPlanStatus.Draft)
            throw new InvalidOperationException("ส่งอนุมัติได้เฉพาะแผนที่ยังเป็นฉบับร่าง (Draft) เท่านั้น");

        var actionCount = await context.Idp_DevelopmentActions.CountAsync(a => a.PlanId == planId, ct);
        if (actionCount == 0)
            throw new InvalidOperationException("กรุณาเพิ่มรายการพัฒนาอย่างน้อย 1 รายการก่อนส่งอนุมัติ");

        var workflow = await context.wf_workflows.FirstOrDefaultAsync(w => w.workflowcode == "IDP_APPROVAL", ct)
            ?? throw new InvalidOperationException("ไม่พบ workflow 'IDP_APPROVAL' — ติดต่อแอดมินให้ตั้งค่าก่อน");
        if (workflow.isactive != true)
            throw new InvalidOperationException($"workflow '{workflow.wname}' ปิดใช้งานอยู่ ไม่สามารถส่งอนุมัติได้");

        var employee = await context.Hremployee.FirstOrDefaultAsync(e => e.id == plan.HremployeeId, ct);
        var subject = $"แผนพัฒนารายบุคคล (IDP) ปี {plan.Year}: {employee?.EmpName} {employee?.EmpSurname}";

        var jobId = await engine.StartJobAsync(workflow.workflowid, "Idp_Plan", plan.Id.ToString(),
            requesterUserId, requesterEmpId, subject, amount: null, ct);

        plan.JobMasterId = jobId;
        plan.Status = IdpPlanStatus.PendingApproval;
        plan.SubmittedDate = DateTime.Now;
        await context.SaveChangesAsync(ct);

        return jobId;
    }

    // Lazy apply-on-read — called from the plan detail page on every load.
    // No-op unless the plan is still PendingApproval with a job that has
    // since closed.
    public async Task SyncStatusFromJobAsync(long planId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var plan = await context.Idp_Plans.FirstOrDefaultAsync(p => p.Id == planId, ct);
        if (plan is null || plan.Status != IdpPlanStatus.PendingApproval || plan.JobMasterId is null)
            return;

        var job = await context.job_masters.FirstOrDefaultAsync(j => j.jobmasterid == plan.JobMasterId, ct);
        if (job is null || job.isJobClosed != true)
            return;

        plan.Status = job.status == WorkflowEngineService.StatusCompleted
            ? IdpPlanStatus.Approved
            : IdpPlanStatus.Rejected;
        if (plan.Status == IdpPlanStatus.Approved)
            plan.ApprovedDate = DateTime.Now;

        await context.SaveChangesAsync(ct);
    }

    public async Task<List<Idp_Plan>> GetPlansForEmployeeAsync(long hremployeeId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        return await context.Idp_Plans.Where(p => p.HremployeeId == hremployeeId).OrderByDescending(p => p.Year).ToListAsync(ct);
    }

    public async Task<(Idp_Plan Plan, List<Idp_DevelopmentAction> Actions)?> GetPlanDetailAsync(long planId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var plan = await context.Idp_Plans.FirstOrDefaultAsync(p => p.Id == planId, ct);
        if (plan is null)
            return null;

        var actions = await context.Idp_DevelopmentActions
            .Include(a => a.Competency)
            .Where(a => a.PlanId == planId)
            .OrderBy(a => a.SortOrder)
            .ToListAsync(ct);

        return (plan, actions);
    }
}
