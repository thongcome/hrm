using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Perf;

// OKR-style cascading goals (Company -> Organization -> Employee), added to
// v1 beyond what the legacy JSP system had, per explicit user instruction.
// Cascade direction is enforced here in the service layer (not a DB
// constraint, matching this codebase's Topic/SubTopic/Indicator weight-sum
// precedent) by requiring a child goal's OwnerType to be numerically deeper
// than its parent's (PerfGoalOwnerType: Company=1 < Organization=2 < Employee=3).
public class PerfGoalService(IDbContextFactory<HRMContext> dbFactory)
{
    public async Task<List<Perf_Goal>> GetGoalsForPeriodAsync(long evaluationPeriodId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        return await context.Perf_Goals.Where(g => g.EvaluationPeriodId == evaluationPeriodId)
            .OrderBy(g => g.OwnerType).ThenBy(g => g.Title).ToListAsync(ct);
    }

    public async Task<Perf_Goal> CreateGoalAsync(Perf_Goal goal, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        switch (goal.OwnerType)
        {
            case PerfGoalOwnerType.Organization when goal.OwnerOrganizationId is null:
                throw new InvalidOperationException("กรุณาเลือกหน่วยงานเจ้าของเป้าหมาย");
            case PerfGoalOwnerType.Employee when goal.OwnerHremployeeId is null:
                throw new InvalidOperationException("กรุณาเลือกพนักงานเจ้าของเป้าหมาย");
        }

        if (goal.ParentGoalId is long parentId)
        {
            var parent = await context.Perf_Goals.FirstOrDefaultAsync(g => g.Id == parentId, ct)
                ?? throw new InvalidOperationException("ไม่พบเป้าหมายแม่ที่เลือก");
            if (parent.EvaluationPeriodId != goal.EvaluationPeriodId)
                throw new InvalidOperationException("เป้าหมายแม่ต้องอยู่ในรอบการประเมินเดียวกัน");
            if ((int)goal.OwnerType <= (int)parent.OwnerType)
                throw new InvalidOperationException("เป้าหมายลูกต้องอยู่ระดับที่ลึกกว่าเป้าหมายแม่เสมอ (บริษัท → หน่วยงาน → บุคคล)");
        }

        goal.CreatedDate = DateTime.Now;
        context.Perf_Goals.Add(goal);
        await context.SaveChangesAsync(ct);
        return goal;
    }

    public async Task UpdateGoalAsync(Perf_Goal goal, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var existing = await context.Perf_Goals.FirstOrDefaultAsync(g => g.Id == goal.Id, ct)
            ?? throw new InvalidOperationException("ไม่พบเป้าหมายนี้แล้ว");

        existing.Title = goal.Title;
        existing.Description = goal.Description;
        existing.TargetValue = goal.TargetValue;
        existing.TargetUnit = goal.TargetUnit;
        existing.Status = goal.Status;

        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteGoalAsync(long goalId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var hasChildren = await context.Perf_Goals.AnyAsync(g => g.ParentGoalId == goalId, ct);
        if (hasChildren)
            throw new InvalidOperationException("ลบไม่ได้ — ยังมีเป้าหมายลูกผูกอยู่กับเป้าหมายนี้");

        var goal = await context.Perf_Goals.FirstOrDefaultAsync(g => g.Id == goalId, ct);
        if (goal is null) return;
        context.Perf_Goals.Remove(goal);
        await context.SaveChangesAsync(ct);
    }

    public async Task<List<Perf_GoalCheckIn>> GetCheckInsAsync(long goalId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        return await context.Perf_GoalCheckIns.Where(c => c.GoalId == goalId)
            .OrderByDescending(c => c.CheckInDate).ToListAsync(ct);
    }

    public async Task RecordCheckInAsync(long goalId, decimal? valueAtCheckIn, string? note, long actorUserId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var goal = await context.Perf_Goals.FirstOrDefaultAsync(g => g.Id == goalId, ct)
            ?? throw new InvalidOperationException("ไม่พบเป้าหมายนี้แล้ว");

        context.Perf_GoalCheckIns.Add(new Perf_GoalCheckIn
        {
            GoalId = goalId,
            CheckInDate = DateTime.Now,
            ValueAtCheckIn = valueAtCheckIn,
            Note = note,
            CreatedByUserId = actorUserId,
        });

        if (valueAtCheckIn is not null)
            goal.CurrentValue = valueAtCheckIn;

        await context.SaveChangesAsync(ct);
    }
}
