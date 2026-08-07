namespace HRM.Services.Succession;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Succession Planning: Key Position (a Job/Pos_ExecType flagged as
// business-critical) + Successor Nomination (candidates against that
// position, with a standard 4-level readiness rating) + Bench Strength
// reporting (which key positions have zero successors — "single point of
// failure"). Deliberately mirrors TalentGridService's shape (record-based
// read models, upsert-by-explicit-method rather than a generic CRUD helper,
// soft-delete via IsActive) since this module sits right next to Talent
// Management in the HRD roadmap and reuses the same conventions.
public class SuccessionService(IDbContextFactory<HRMContext> dbFactory)
{
    public record KeyPositionRow(
        long Id, long PosExecTypeId, string PosExecTypeName,
        BusinessImpactLevel BusinessImpact, ReplacementDifficultyLevel ReplacementDifficulty,
        string? Note, int SuccessorCount);

    public record NominationRow(
        long Id, long HremployeeId, string EmpNo, string Name,
        ReadinessLevel ReadinessLevel, string? Note, DateTime NominatedDate);

    public record BenchStrengthRow(
        long KeyPositionId, string PosExecTypeName,
        BusinessImpactLevel BusinessImpact, ReplacementDifficultyLevel ReplacementDifficulty,
        int ReadyNowCount, int TotalSuccessorCount, bool IsSinglePointOfFailure);

    public async Task<List<KeyPositionRow>> GetKeyPositionsAsync(string companyId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var keyPositions = await context.Succ_KeyPositions
            .Where(k => k.CompanyId == companyId && k.IsActive)
            .OrderByDescending(k => k.BusinessImpact)
            .ToListAsync(ct);
        if (keyPositions.Count == 0)
            return new();

        var posExecTypeIds = keyPositions.Select(k => k.PosExecTypeId).Distinct().ToList();
        var posExecTypeNames = await context.Pos_ExecTypes
            .Where(p => posExecTypeIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name, ct);

        var keyPositionIds = keyPositions.Select(k => k.Id).ToList();
        var successorCounts = await context.Succ_SuccessorNominations
            .Where(n => keyPositionIds.Contains(n.KeyPositionId) && n.IsActive)
            .GroupBy(n => n.KeyPositionId)
            .Select(g => new { KeyPositionId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.KeyPositionId, g => g.Count, ct);

        return keyPositions.Select(k => new KeyPositionRow(
            k.Id, k.PosExecTypeId,
            posExecTypeNames.TryGetValue(k.PosExecTypeId, out var name) ? name : $"#{k.PosExecTypeId}",
            k.BusinessImpact, k.ReplacementDifficulty, k.Note,
            successorCounts.TryGetValue(k.Id, out var count) ? count : 0)).ToList();
    }

    public async Task<long> AddKeyPositionAsync(string companyId, long posExecTypeId, BusinessImpactLevel impact, ReplacementDifficultyLevel difficulty, string? note, long actorUserId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var alreadyActive = await context.Succ_KeyPositions.AnyAsync(k => k.CompanyId == companyId && k.PosExecTypeId == posExecTypeId && k.IsActive, ct);
        if (alreadyActive)
            throw new InvalidOperationException("ตำแหน่งนี้ถูกกำหนดเป็นตำแหน่งสำคัญอยู่แล้ว");

        var keyPosition = new Succ_KeyPosition
        {
            CompanyId = companyId,
            PosExecTypeId = posExecTypeId,
            BusinessImpact = impact,
            ReplacementDifficulty = difficulty,
            Note = note,
            AddedByUserId = actorUserId,
        };
        context.Succ_KeyPositions.Add(keyPosition);
        await context.SaveChangesAsync(ct);
        return keyPosition.Id;
    }

    public async Task UpdateKeyPositionAsync(long id, BusinessImpactLevel impact, ReplacementDifficultyLevel difficulty, string? note, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var keyPosition = await context.Succ_KeyPositions.FirstOrDefaultAsync(k => k.Id == id, ct)
            ?? throw new InvalidOperationException("ไม่พบตำแหน่งสำคัญนี้");

        keyPosition.BusinessImpact = impact;
        keyPosition.ReplacementDifficulty = difficulty;
        keyPosition.Note = note;
        await context.SaveChangesAsync(ct);
    }

    public async Task RemoveKeyPositionAsync(long id, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var keyPosition = await context.Succ_KeyPositions.FirstOrDefaultAsync(k => k.Id == id, ct);
        if (keyPosition is null)
            return;

        keyPosition.IsActive = false;
        await context.SaveChangesAsync(ct);
    }

    public async Task<List<NominationRow>> GetNominationsAsync(long keyPositionId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var nominations = await context.Succ_SuccessorNominations
            .Where(n => n.KeyPositionId == keyPositionId && n.IsActive)
            .OrderBy(n => n.ReadinessLevel)
            .ToListAsync(ct);
        if (nominations.Count == 0)
            return new();

        var employeeIds = nominations.Select(n => n.HremployeeId).Distinct().ToList();
        var employees = await context.Hremployee.Where(e => employeeIds.Contains(e.id)).ToListAsync(ct);

        return nominations.Select(n =>
        {
            var emp = employees.FirstOrDefault(e => e.id == n.HremployeeId);
            return new NominationRow(n.Id, n.HremployeeId, emp?.EmpNo ?? "?", emp is null ? $"#{n.HremployeeId}" : $"{emp.EmpName} {emp.EmpSurname}", n.ReadinessLevel, n.Note, n.NominatedDate);
        }).ToList();
    }

    public async Task<long> NominateAsync(long keyPositionId, long hremployeeId, ReadinessLevel readinessLevel, long actorUserId, string? note, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var alreadyActive = await context.Succ_SuccessorNominations.AnyAsync(n => n.KeyPositionId == keyPositionId && n.HremployeeId == hremployeeId && n.IsActive, ct);
        if (alreadyActive)
            throw new InvalidOperationException("พนักงานคนนี้ถูกเสนอชื่อสำหรับตำแหน่งนี้ไปแล้ว");

        var nomination = new Succ_SuccessorNomination
        {
            KeyPositionId = keyPositionId,
            HremployeeId = hremployeeId,
            ReadinessLevel = readinessLevel,
            NominatedByUserId = actorUserId,
            Note = note,
        };
        context.Succ_SuccessorNominations.Add(nomination);
        await context.SaveChangesAsync(ct);
        return nomination.Id;
    }

    public async Task UpdateNominationAsync(long nominationId, ReadinessLevel readinessLevel, string? note, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var nomination = await context.Succ_SuccessorNominations.FirstOrDefaultAsync(n => n.Id == nominationId, ct)
            ?? throw new InvalidOperationException("ไม่พบรายชื่อผู้สืบทอดนี้");

        nomination.ReadinessLevel = readinessLevel;
        nomination.Note = note;
        await context.SaveChangesAsync(ct);
    }

    public async Task RemoveNominationAsync(long nominationId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var nomination = await context.Succ_SuccessorNominations.FirstOrDefaultAsync(n => n.Id == nominationId, ct);
        if (nomination is null)
            return;

        nomination.IsActive = false;
        await context.SaveChangesAsync(ct);
    }

    public async Task<List<BenchStrengthRow>> GetBenchStrengthAsync(string companyId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var keyPositions = await context.Succ_KeyPositions
            .Where(k => k.CompanyId == companyId && k.IsActive)
            .OrderByDescending(k => k.BusinessImpact).ThenByDescending(k => k.ReplacementDifficulty)
            .ToListAsync(ct);
        if (keyPositions.Count == 0)
            return new();

        var posExecTypeIds = keyPositions.Select(k => k.PosExecTypeId).Distinct().ToList();
        var posExecTypeNames = await context.Pos_ExecTypes
            .Where(p => posExecTypeIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name, ct);

        var keyPositionIds = keyPositions.Select(k => k.Id).ToList();
        var nominations = await context.Succ_SuccessorNominations
            .Where(n => keyPositionIds.Contains(n.KeyPositionId) && n.IsActive)
            .ToListAsync(ct);

        return keyPositions.Select(k =>
        {
            var forThisPosition = nominations.Where(n => n.KeyPositionId == k.Id).ToList();
            var readyNow = forThisPosition.Count(n => n.ReadinessLevel == ReadinessLevel.ReadyNow);
            return new BenchStrengthRow(
                k.Id, posExecTypeNames.TryGetValue(k.PosExecTypeId, out var name) ? name : $"#{k.PosExecTypeId}",
                k.BusinessImpact, k.ReplacementDifficulty,
                readyNow, forThisPosition.Count, forThisPosition.Count == 0);
        }).ToList();
    }
}
