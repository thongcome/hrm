namespace HRM.Services.Career;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Career path = an ordered list of Pos_ExecType (Job) steps within a
// Job_Family that HR defines as the expected progression. GetMyPathAsync
// resolves an employee's current step from their active Pos_PositionSlot and
// returns the next step (if any) so the UI can drive
// IdpAssessmentService.GetGapAnalysisForTargetPositionAsync against it.
public class CareerPathService(IDbContextFactory<HRMContext> dbFactory)
{
    public record StepRow(long StepId, long PosExecTypeId, string PosExecTypeName, int SortOrder);

    public record MyPathResult(
        long? CurrentPosExecTypeId, string? CurrentPosExecTypeName, long? JobFamilyId, string? JobFamilyName,
        List<StepRow> Steps, StepRow? CurrentStep, StepRow? NextStep);

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
            return new(null, null, null, null, new(), null, null);

        var execType = await context.Pos_ExecTypes.FirstOrDefaultAsync(t => t.Id == posExecTypeId, ct);
        if (execType?.JobFamilyId is not long jobFamilyId)
            return new(posExecTypeId, execType?.Name, null, null, new(), null, null);

        var jobFamily = await context.Job_Families.FirstOrDefaultAsync(f => f.Id == jobFamilyId, ct);
        var steps = await BuildStepRowsAsync(context, execType.CompanyId, jobFamilyId, ct);

        var currentStep = steps.FirstOrDefault(s => s.PosExecTypeId == posExecTypeId);
        var nextStep = currentStep is null ? null : steps.Where(s => s.SortOrder > currentStep.SortOrder).OrderBy(s => s.SortOrder).FirstOrDefault();

        return new(posExecTypeId, execType.Name, jobFamilyId, jobFamily?.Name, steps, currentStep, nextStep);
    }
}
