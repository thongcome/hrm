namespace HRM.Services.OrgDev;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Leadership Development: one plan per employee (typically sourced from
// Talent_PoolEntry, not enforced) with a milestone checklist — mirrors
// Services/Hrd/LifecycleTaskService.cs's shape (instances + checklist items,
// no workflow approval).
public class LeadershipDevelopmentService(IDbContextFactory<HRMContext> dbFactory)
{
    public record PlanRow(long PlanId, long HremployeeId, string EmpNo, string Name, string? TargetPosExecTypeName, LeadershipPlanStatus Status, DateOnly? StartDate, DateOnly? TargetDate, int TotalMilestones, int CompletedMilestones);

    public async Task<List<PlanRow>> GetPlansForCompanyAsync(string companyId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var companyEmployeeIds = await context.Hremployee.Where(e => e.companyid == companyId).Select(e => e.id).ToListAsync(ct);
        var plans = await context.OrgDev_LeadershipPlans.Where(p => companyEmployeeIds.Contains(p.HremployeeId)).OrderByDescending(p => p.CreatedDate).ToListAsync(ct);
        if (plans.Count == 0)
            return new();

        var employeeIds = plans.Select(p => p.HremployeeId).Distinct().ToList();
        var employees = await context.Hremployee.Where(e => employeeIds.Contains(e.id)).ToListAsync(ct);

        var targetIds = plans.Where(p => p.TargetPosExecTypeId is not null).Select(p => p.TargetPosExecTypeId!.Value).Distinct().ToList();
        var targetNames = await context.Pos_ExecTypes.Where(t => targetIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id, t => t.Name, ct);

        var planIds = plans.Select(p => p.Id).ToList();
        var milestones = await context.OrgDev_LeadershipMilestones.Where(m => planIds.Contains(m.PlanId)).ToListAsync(ct);

        return plans.Select(p =>
        {
            var emp = employees.First(e => e.id == p.HremployeeId);
            var forThisPlan = milestones.Where(m => m.PlanId == p.Id).ToList();
            return new PlanRow(p.Id, p.HremployeeId, emp.EmpNo, $"{emp.EmpName} {emp.EmpSurname}",
                p.TargetPosExecTypeId is long tid && targetNames.TryGetValue(tid, out var n) ? n : null,
                p.Status, p.StartDate, p.TargetDate, forThisPlan.Count, forThisPlan.Count(m => m.Status == OrgDevMilestoneStatus.Completed));
        }).ToList();
    }

    public async Task<long> CreatePlanAsync(long hremployeeId, long? targetPosExecTypeId, DateOnly? startDate, DateOnly? targetDate, string? note, long actorUserId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var plan = new OrgDev_LeadershipPlan
        {
            HremployeeId = hremployeeId,
            TargetPosExecTypeId = targetPosExecTypeId,
            StartDate = startDate,
            TargetDate = targetDate,
            Note = note,
            CreatedByUserId = actorUserId,
        };
        context.OrgDev_LeadershipPlans.Add(plan);
        await context.SaveChangesAsync(ct);
        return plan.Id;
    }

    public async Task UpdateStatusAsync(long planId, LeadershipPlanStatus status, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var plan = await context.OrgDev_LeadershipPlans.FirstOrDefaultAsync(p => p.Id == planId, ct);
        if (plan is null) return;
        plan.Status = status;
        await context.SaveChangesAsync(ct);
    }

    public async Task<List<OrgDev_LeadershipMilestone>> GetMilestonesAsync(long planId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        return await context.OrgDev_LeadershipMilestones.Where(m => m.PlanId == planId).OrderBy(m => m.Id).ToListAsync(ct);
    }

    public async Task AddMilestoneAsync(long planId, string title, DateOnly? targetDate, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        context.OrgDev_LeadershipMilestones.Add(new OrgDev_LeadershipMilestone { PlanId = planId, Title = title, TargetDate = targetDate });
        await context.SaveChangesAsync(ct);
    }

    public async Task CompleteMilestoneAsync(long milestoneId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var milestone = await context.OrgDev_LeadershipMilestones.FirstOrDefaultAsync(m => m.Id == milestoneId, ct);
        if (milestone is null) return;
        milestone.Status = OrgDevMilestoneStatus.Completed;
        milestone.CompletedDate = DateTime.Now;
        await context.SaveChangesAsync(ct);
    }
}
