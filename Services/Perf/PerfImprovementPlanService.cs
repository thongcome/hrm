namespace HRM.Services.Perf;

using HRM.Models;
using HRM.Services.Workflow;
using Microsoft.EntityFrameworkCore;

// Performance Improvement Plan lifecycle — header CRUD + goal CRUD + the
// generic Workflow Engine submission/lazy-sync pair, mirroring
// IdpPlanService.cs almost exactly. Diverges from IDP in two places: (1)
// "Approved" here means the plan becomes Active (in force, being tracked),
// not a terminal state, and (2) it adds append-only check-ins plus a
// terminal-outcome step (Passed/Extended/Failed) that a development plan
// doesn't need.
public class PerfImprovementPlanService(IDbContextFactory<HRMContext> dbFactory, WorkflowEngineService engine)
{
    public async Task<long> CreateDraftAsync(long hremployeeId, string reason, DateOnly startDate, DateOnly endDate,
        long? managerUserId, long actorUserId, long? sourceEvaluationInstanceId = null, CancellationToken ct = default)
    {
        if (endDate < startDate)
            throw new InvalidOperationException("วันที่สิ้นสุดต้องไม่อยู่ก่อนวันที่เริ่มต้น");

        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var hasOpenPlan = await context.Perf_ImprovementPlans.AnyAsync(p => p.HremployeeId == hremployeeId
            && (p.Status == PipStatus.Draft || p.Status == PipStatus.PendingApproval || p.Status == PipStatus.Active), ct);
        if (hasOpenPlan)
            throw new InvalidOperationException("พนักงานคนนี้มี PIP ที่ยังไม่ปิด (ฉบับร่าง/รออนุมัติ/กำลังดำเนินการ) อยู่แล้ว");

        var plan = new Perf_ImprovementPlan
        {
            HremployeeId = hremployeeId,
            SourceEvaluationInstanceId = sourceEvaluationInstanceId,
            Reason = reason,
            StartDate = startDate,
            EndDate = endDate,
            ManagerUserId = managerUserId,
            CreatedByUserId = actorUserId,
        };
        context.Perf_ImprovementPlans.Add(plan);
        await context.SaveChangesAsync(ct);
        return plan.Id;
    }

    // Used when an Active plan ends in Extended — creates the follow-on plan
    // pre-linked via PreviousPlanId so the history reads as one continuous
    // thread across rounds.
    public async Task<long> CreateExtensionAsync(long previousPlanId, DateOnly startDate, DateOnly endDate, string reason, long actorUserId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var previous = await context.Perf_ImprovementPlans.FirstOrDefaultAsync(p => p.Id == previousPlanId, ct)
            ?? throw new InvalidOperationException("ไม่พบ PIP รอบก่อนหน้านี้");
        if (previous.Status != PipStatus.Extended)
            throw new InvalidOperationException("สร้างรอบขยายเวลาได้เฉพาะ PIP ที่ปิดผลเป็น 'ขยายเวลา' เท่านั้น");

        var plan = new Perf_ImprovementPlan
        {
            HremployeeId = previous.HremployeeId,
            SourceEvaluationInstanceId = previous.SourceEvaluationInstanceId,
            Reason = reason,
            StartDate = startDate,
            EndDate = endDate,
            ManagerUserId = previous.ManagerUserId,
            PreviousPlanId = previousPlanId,
            CreatedByUserId = actorUserId,
        };
        context.Perf_ImprovementPlans.Add(plan);
        await context.SaveChangesAsync(ct);
        return plan.Id;
    }

    public async Task<long> AddGoalAsync(long planId, string title, string? successCriteria, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var plan = await context.Perf_ImprovementPlans.FirstOrDefaultAsync(p => p.Id == planId, ct)
            ?? throw new InvalidOperationException("ไม่พบ PIP นี้แล้ว");
        if (plan.Status != PipStatus.Draft)
            throw new InvalidOperationException("แก้ไขเป้าหมายได้เฉพาะ PIP ที่ยังเป็นฉบับร่าง (Draft) เท่านั้น");

        var sortOrder = await context.Perf_ImprovementGoals.CountAsync(g => g.PlanId == planId, ct);
        var goal = new Perf_ImprovementGoal { PlanId = planId, Title = title, SuccessCriteria = successCriteria, SortOrder = sortOrder };
        context.Perf_ImprovementGoals.Add(goal);
        await context.SaveChangesAsync(ct);
        return goal.Id;
    }

    public async Task RemoveGoalAsync(long goalId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var goal = await context.Perf_ImprovementGoals.Include(g => g.Plan).FirstOrDefaultAsync(g => g.Id == goalId, ct);
        if (goal is null) return;
        if (goal.Plan.Status != PipStatus.Draft)
            throw new InvalidOperationException("ลบเป้าหมายได้เฉพาะ PIP ที่ยังเป็นฉบับร่าง (Draft) เท่านั้น");

        context.Perf_ImprovementGoals.Remove(goal);
        await context.SaveChangesAsync(ct);
    }

    // Goal status can be updated any time the plan is Active or Draft — this
    // just tracks progress, not the plan's own lifecycle state.
    public async Task UpdateGoalStatusAsync(long goalId, PipGoalStatus status, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var goal = await context.Perf_ImprovementGoals.FirstOrDefaultAsync(g => g.Id == goalId, ct)
            ?? throw new InvalidOperationException("ไม่พบเป้าหมายนี้แล้ว");
        goal.Status = status;
        await context.SaveChangesAsync(ct);
    }

    public async Task<long> SubmitForApprovalAsync(long planId, long requesterUserId, string? requesterEmpId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var plan = await context.Perf_ImprovementPlans.FirstOrDefaultAsync(p => p.Id == planId, ct)
            ?? throw new InvalidOperationException("ไม่พบ PIP นี้แล้ว");
        if (plan.Status != PipStatus.Draft)
            throw new InvalidOperationException("ส่งอนุมัติได้เฉพาะ PIP ที่ยังเป็นฉบับร่าง (Draft) เท่านั้น");

        var goalCount = await context.Perf_ImprovementGoals.CountAsync(g => g.PlanId == planId, ct);
        if (goalCount == 0)
            throw new InvalidOperationException("กรุณาเพิ่มเป้าหมายอย่างน้อย 1 ข้อก่อนส่งอนุมัติ");

        var workflow = await context.wf_workflows.FirstOrDefaultAsync(w => w.workflowcode == "PIP_APPROVAL", ct)
            ?? throw new InvalidOperationException("ไม่พบ workflow 'PIP_APPROVAL' — ติดต่อแอดมินให้ตั้งค่าก่อน");
        if (workflow.isactive != true)
            throw new InvalidOperationException($"workflow '{workflow.wname}' ปิดใช้งานอยู่ ไม่สามารถส่งอนุมัติได้");

        var employee = await context.Hremployee.FirstOrDefaultAsync(e => e.id == plan.HremployeeId, ct);
        var subject = $"Performance Improvement Plan: {employee?.EmpName} {employee?.EmpSurname} ({plan.StartDate:dd/MM/yyyy} - {plan.EndDate:dd/MM/yyyy})";

        var jobId = await engine.StartJobAsync(workflow.workflowid, "Perf_ImprovementPlan", plan.Id.ToString(),
            requesterUserId, requesterEmpId, subject, amount: null, ct);

        plan.JobMasterId = jobId;
        plan.Status = PipStatus.PendingApproval;
        await context.SaveChangesAsync(ct);

        return jobId;
    }

    // Lazy apply-on-read — called from the plan detail page on every load.
    // No-op unless the plan is still PendingApproval with a job that has
    // since closed. On approval the plan becomes Active (not a terminal
    // "Approved" state) since a PIP is meant to be tracked, not filed away.
    public async Task SyncStatusFromJobAsync(long planId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var plan = await context.Perf_ImprovementPlans.FirstOrDefaultAsync(p => p.Id == planId, ct);
        if (plan is null || plan.Status != PipStatus.PendingApproval || plan.JobMasterId is null)
            return;

        var job = await context.job_masters.FirstOrDefaultAsync(j => j.jobmasterid == plan.JobMasterId, ct);
        if (job is null || job.isJobClosed != true)
            return;

        plan.Status = job.status == WorkflowEngineService.StatusCompleted ? PipStatus.Active : PipStatus.Rejected;
        await context.SaveChangesAsync(ct);
    }

    public async Task<long> RecordCheckInAsync(long planId, DateOnly checkInDate, PipCheckInRating rating, string note, long actorUserId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var plan = await context.Perf_ImprovementPlans.FirstOrDefaultAsync(p => p.Id == planId, ct)
            ?? throw new InvalidOperationException("ไม่พบ PIP นี้แล้ว");
        if (plan.Status != PipStatus.Active)
            throw new InvalidOperationException("บันทึกความคืบหน้าได้เฉพาะ PIP ที่กำลังดำเนินการ (Active) เท่านั้น");

        var checkIn = new Perf_ImprovementCheckIn { PlanId = planId, CheckInDate = checkInDate, Rating = rating, Note = note, RecordedByUserId = actorUserId };
        context.Perf_ImprovementCheckIns.Add(checkIn);
        await context.SaveChangesAsync(ct);
        return checkIn.Id;
    }

    // Closes out an Active plan with one of the three standard PIP
    // terminal outcomes. Failed/Passed are final; Extended expects the
    // caller to follow up with CreateExtensionAsync to open the next round.
    public async Task CloseOutcomeAsync(long planId, PipStatus outcome, string? note, long actorUserId, CancellationToken ct = default)
    {
        if (outcome is not (PipStatus.Passed or PipStatus.Extended or PipStatus.Failed))
            throw new InvalidOperationException("ผลลัพธ์ต้องเป็น ผ่าน / ขยายเวลา / ไม่ผ่าน เท่านั้น");

        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var plan = await context.Perf_ImprovementPlans.FirstOrDefaultAsync(p => p.Id == planId, ct)
            ?? throw new InvalidOperationException("ไม่พบ PIP นี้แล้ว");
        if (plan.Status != PipStatus.Active)
            throw new InvalidOperationException("ปิดผลได้เฉพาะ PIP ที่กำลังดำเนินการ (Active) เท่านั้น");

        plan.Status = outcome;
        plan.OutcomeNote = note;
        plan.OutcomeDate = DateTime.Now;
        await context.SaveChangesAsync(ct);
    }

    public async Task CancelAsync(long planId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var plan = await context.Perf_ImprovementPlans.FirstOrDefaultAsync(p => p.Id == planId, ct)
            ?? throw new InvalidOperationException("ไม่พบ PIP นี้แล้ว");
        if (plan.Status is not (PipStatus.Draft or PipStatus.PendingApproval))
            throw new InvalidOperationException("ยกเลิกได้เฉพาะ PIP ที่ยังเป็นฉบับร่างหรือรออนุมัติเท่านั้น — PIP ที่กำลังดำเนินการต้องปิดผลผ่าน 'ปิดผล' แทน");

        plan.Status = PipStatus.Cancelled;
        await context.SaveChangesAsync(ct);
    }

    public async Task<List<Perf_ImprovementPlan>> GetPlansForEmployeeAsync(long hremployeeId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        return await context.Perf_ImprovementPlans
            .Where(p => p.HremployeeId == hremployeeId).OrderByDescending(p => p.CreatedDate).ToListAsync(ct);
    }

    public async Task<List<Perf_ImprovementPlan>> GetAllPlansAsync(string companyId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var employeeIds = await context.Hremployee.Where(e => e.companyid == companyId).Select(e => e.id).ToListAsync(ct);
        return await context.Perf_ImprovementPlans
            .Where(p => employeeIds.Contains(p.HremployeeId))
            .OrderByDescending(p => p.CreatedDate)
            .ToListAsync(ct);
    }

    public async Task<(Perf_ImprovementPlan Plan, List<Perf_ImprovementGoal> Goals, List<Perf_ImprovementCheckIn> CheckIns)?> GetPlanDetailAsync(long planId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var plan = await context.Perf_ImprovementPlans.FirstOrDefaultAsync(p => p.Id == planId, ct);
        if (plan is null) return null;

        var goals = await context.Perf_ImprovementGoals.Where(g => g.PlanId == planId).OrderBy(g => g.SortOrder).ToListAsync(ct);
        var checkIns = await context.Perf_ImprovementCheckIns.Where(c => c.PlanId == planId).OrderByDescending(c => c.CheckInDate).ToListAsync(ct);
        return (plan, goals, checkIns);
    }
}
