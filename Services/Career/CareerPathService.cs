namespace HRM.Services.Career;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Career path comes in two layers:
//  - The legacy LADDER: an ordered list of Pos_ExecType (Job) steps within a
//    Job_Family (Career_PathStep, SortOrder) — still used by the Explorer's
//    per-family overview and as the fallback "next step".
//  - The LATTICE: Career_PathTransition edges ("from X you can go to Y"),
//    which allow multiple outgoing moves per position, including
//    cross-family. When any transitions exist for the employee's current
//    position, they win over the ladder's next-SortOrder step.
// GetMyPathAsync resolves an employee's current position from their active
// Pos_PositionSlot and returns both, so the UI can drive
// IdpAssessmentService.GetGapAnalysisForTargetPositionAsync per target.
public class CareerPathService(IDbContextFactory<HRMContext> dbFactory)
{
    public record StepRow(long StepId, long PosExecTypeId, string PosExecTypeName, int SortOrder);

    // One outgoing lattice edge, with the target position resolved to its
    // display name (soft link — resolved by manual query, no nav props).
    public record TransitionRow(long TransitionId, long FromPosExecTypeId, long ToPosExecTypeId, string ToPosExecTypeName, string? Note, int SortOrder);

    // One incoming lattice edge — "people naturally arrive at this position
    // from here". Used by Succession's KeyPositionDetail as a sourcing hint.
    public record TransitionSourceRow(long TransitionId, long FromPosExecTypeId, string FromPosExecTypeName, string? Note);

    // NextTransitions is the lattice's answer to "where can I go from here";
    // when it is non-empty the UI should prefer it over the legacy NextStep
    // (which is still computed so old ladder-only setups keep working).
    public record MyPathResult(
        long? CurrentPosExecTypeId, string? CurrentPosExecTypeName, long? JobFamilyId, string? JobFamilyName,
        List<StepRow> Steps, StepRow? CurrentStep, StepRow? NextStep, List<TransitionRow> NextTransitions);

    public async Task<List<StepRow>> GetStepsAsync(string companyId, long jobFamilyId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        return await BuildStepRowsAsync(context, companyId, jobFamilyId, ct);
    }

    private static async Task<List<StepRow>> BuildStepRowsAsync(HRMContext context, string companyId, long jobFamilyId, CancellationToken ct)
    {
        var steps = await context.Career_PathSteps
            .Where(s => s.CompanyId == companyId && s.JobFamilyId == jobFamilyId)
            .OrderBy(s => s.SortOrder)
            .ToListAsync(ct);
        if (steps.Count == 0)
            return new();

        var posExecTypeIds = steps.Select(s => s.PosExecTypeId).ToList();
        var names = await context.Pos_ExecTypes.Where(t => posExecTypeIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id, t => t.Name, ct);

        return steps.Select(s => new StepRow(s.Id, s.PosExecTypeId, names.TryGetValue(s.PosExecTypeId, out var n) ? n : $"#{s.PosExecTypeId}", s.SortOrder)).ToList();
    }

    public async Task<long> AddStepAsync(string companyId, long jobFamilyId, long posExecTypeId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var alreadyInPath = await context.Career_PathSteps.AnyAsync(s => s.CompanyId == companyId && s.JobFamilyId == jobFamilyId && s.PosExecTypeId == posExecTypeId, ct);
        if (alreadyInPath)
            throw new InvalidOperationException("ตำแหน่งนี้อยู่ใน career path ของสายงานนี้อยู่แล้ว");

        var maxSortOrder = await context.Career_PathSteps.Where(s => s.CompanyId == companyId && s.JobFamilyId == jobFamilyId)
            .Select(s => (int?)s.SortOrder).MaxAsync(ct) ?? 0;

        var step = new Career_PathStep { CompanyId = companyId, JobFamilyId = jobFamilyId, PosExecTypeId = posExecTypeId, SortOrder = maxSortOrder + 1 };
        context.Career_PathSteps.Add(step);
        await context.SaveChangesAsync(ct);
        return step.Id;
    }

    public async Task RemoveStepAsync(long stepId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var step = await context.Career_PathSteps.FirstOrDefaultAsync(s => s.Id == stepId, ct);
        if (step is null) return;
        context.Career_PathSteps.Remove(step);
        await context.SaveChangesAsync(ct);
    }

    // Swaps SortOrder with the immediate neighbor in the given direction —
    // no drag-drop in this codebase (see CRUD skill notes), up/down buttons
    // are the established pattern for reorderable lists.
    public async Task MoveStepAsync(long stepId, bool moveUp, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var step = await context.Career_PathSteps.FirstOrDefaultAsync(s => s.Id == stepId, ct);
        if (step is null) return;

        var neighbor = moveUp
            ? await context.Career_PathSteps.Where(s => s.CompanyId == step.CompanyId && s.JobFamilyId == step.JobFamilyId && s.SortOrder < step.SortOrder).OrderByDescending(s => s.SortOrder).FirstOrDefaultAsync(ct)
            : await context.Career_PathSteps.Where(s => s.CompanyId == step.CompanyId && s.JobFamilyId == step.JobFamilyId && s.SortOrder > step.SortOrder).OrderBy(s => s.SortOrder).FirstOrDefaultAsync(ct);
        if (neighbor is null) return;

        (step.SortOrder, neighbor.SortOrder) = (neighbor.SortOrder, step.SortOrder);
        await context.SaveChangesAsync(ct);
    }

    public async Task<MyPathResult> GetMyPathAsync(long hremployeeId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var slot = await context.Pos_PositionSlots.FirstOrDefaultAsync(s => s.HremployeeId == hremployeeId && s.IsActive, ct);
        if (slot?.PosExecTypeId is not long posExecTypeId)
            return new(null, null, null, null, new(), null, null, new());

        var execType = await context.Pos_ExecTypes.FirstOrDefaultAsync(t => t.Id == posExecTypeId, ct);
        if (execType is null)
            return new(posExecTypeId, null, null, null, new(), null, null, new());

        // Lattice first: outgoing transitions from the current position.
        // Deliberately resolved even when the position has no Job_Family —
        // the lattice is cross-family, so an unclassified position can still
        // have "where next" edges the family ladder could never give it.
        var nextTransitions = await BuildTransitionRowsAsync(context, execType.CompanyId, posExecTypeId, ct);

        if (execType.JobFamilyId is not long jobFamilyId)
            return new(posExecTypeId, execType.Name, null, null, new(), null, null, nextTransitions);

        var jobFamily = await context.Job_Families.FirstOrDefaultAsync(f => f.Id == jobFamilyId, ct);
        var steps = await BuildStepRowsAsync(context, execType.CompanyId, jobFamilyId, ct);

        // Legacy ladder fallback: still computed unconditionally so
        // companies that haven't defined any transitions keep the exact
        // pre-lattice behavior (next step = next SortOrder in the family).
        // The UI prefers NextTransitions whenever it is non-empty.
        var currentStep = steps.FirstOrDefault(s => s.PosExecTypeId == posExecTypeId);
        var nextStep = currentStep is null ? null : steps.Where(s => s.SortOrder > currentStep.SortOrder).OrderBy(s => s.SortOrder).FirstOrDefault();

        return new(posExecTypeId, execType.Name, jobFamilyId, jobFamily?.Name, steps, currentStep, nextStep, nextTransitions);
    }

    // ----- Lattice (Career_PathTransition) -----

    public async Task<List<TransitionRow>> GetTransitionsFromAsync(string companyId, long posExecTypeId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        return await BuildTransitionRowsAsync(context, companyId, posExecTypeId, ct);
    }

    private static async Task<List<TransitionRow>> BuildTransitionRowsAsync(HRMContext context, string companyId, long fromPosExecTypeId, CancellationToken ct)
    {
        var transitions = await context.Career_PathTransitions
            .Where(t => t.CompanyId == companyId && t.FromPosExecTypeId == fromPosExecTypeId && t.IsActive)
            .OrderBy(t => t.SortOrder)
            .ToListAsync(ct);
        if (transitions.Count == 0)
            return new();

        var toIds = transitions.Select(t => t.ToPosExecTypeId).Distinct().ToList();
        var names = await context.Pos_ExecTypes.Where(p => toIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name, ct);

        return transitions.Select(t => new TransitionRow(
            t.Id, t.FromPosExecTypeId, t.ToPosExecTypeId,
            names.TryGetValue(t.ToPosExecTypeId, out var n) ? n : $"#{t.ToPosExecTypeId}",
            t.Note, t.SortOrder)).ToList();
    }

    // Incoming edges — which positions HR has mapped as leading INTO the
    // given position. Succession's KeyPositionDetail shows these as the
    // natural sourcing pools for successors.
    public async Task<List<TransitionSourceRow>> GetTransitionsIntoAsync(string companyId, long toPosExecTypeId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var transitions = await context.Career_PathTransitions
            .Where(t => t.CompanyId == companyId && t.ToPosExecTypeId == toPosExecTypeId && t.IsActive)
            .OrderBy(t => t.SortOrder)
            .ToListAsync(ct);
        if (transitions.Count == 0)
            return new();

        var fromIds = transitions.Select(t => t.FromPosExecTypeId).Distinct().ToList();
        var names = await context.Pos_ExecTypes.Where(p => fromIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name, ct);

        return transitions.Select(t => new TransitionSourceRow(
            t.Id, t.FromPosExecTypeId,
            names.TryGetValue(t.FromPosExecTypeId, out var n) ? n : $"#{t.FromPosExecTypeId}",
            t.Note)).ToList();
    }

    public async Task<long> AddTransitionAsync(string companyId, long fromPosExecTypeId, long toPosExecTypeId, string? note, long createdByUserId, CancellationToken ct = default)
    {
        if (fromPosExecTypeId == toPosExecTypeId)
            throw new InvalidOperationException("ตำแหน่งต้นทางและปลายทางต้องไม่ใช่ตำแหน่งเดียวกัน");

        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var duplicate = await context.Career_PathTransitions.AnyAsync(t =>
            t.CompanyId == companyId && t.FromPosExecTypeId == fromPosExecTypeId && t.ToPosExecTypeId == toPosExecTypeId && t.IsActive, ct);
        if (duplicate)
            throw new InvalidOperationException("มีเส้นทางจากตำแหน่งนี้ไปยังตำแหน่งปลายทางนี้อยู่แล้ว");

        var maxSortOrder = await context.Career_PathTransitions
            .Where(t => t.CompanyId == companyId && t.FromPosExecTypeId == fromPosExecTypeId && t.IsActive)
            .Select(t => (int?)t.SortOrder).MaxAsync(ct) ?? 0;

        var transition = new Career_PathTransition
        {
            CompanyId = companyId,
            FromPosExecTypeId = fromPosExecTypeId,
            ToPosExecTypeId = toPosExecTypeId,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            SortOrder = maxSortOrder + 1,
            CreatedByUserId = createdByUserId,
        };
        context.Career_PathTransitions.Add(transition);
        await context.SaveChangesAsync(ct);
        return transition.Id;
    }

    // Soft delete (IsActive=false), matching house convention — history of
    // who defined which edge survives for audit.
    public async Task RemoveTransitionAsync(long transitionId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var transition = await context.Career_PathTransitions.FirstOrDefaultAsync(t => t.Id == transitionId, ct);
        if (transition is null) return;
        transition.IsActive = false;
        await context.SaveChangesAsync(ct);
    }
}
