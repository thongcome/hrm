namespace HRM.Services.Succession;

using HRM.Models;
using HRM.Services.Workflow;
using Microsoft.EntityFrameworkCore;

// Succession Planning: Key Position (a Job/Pos_ExecType flagged as
// business-critical) + Successor Nomination (candidates against that
// position, with a standard 4-level readiness rating) + Bench Strength
// reporting (which key positions have zero successors — "single point of
// failure"). Deliberately mirrors TalentGridService's shape (record-based
// read models, upsert-by-explicit-method rather than a generic CRUD helper,
// soft-delete via IsActive) since this module sits right next to Talent
// Management in the HRD roadmap and reuses the same conventions.
public class SuccessionService(IDbContextFactory<HRMContext> dbFactory, WorkflowEngineService engine)
{
    public record KeyPositionRow(
        long Id, long PosExecTypeId, string PosExecTypeName,
        BusinessImpactLevel BusinessImpact, ReplacementDifficultyLevel ReplacementDifficulty,
        string? Note, int SuccessorCount, int? MinYearsExperience);

    // MeetsMinExperience is informational input for HR, never used to set
    // ReadinessLevel automatically — readiness stays a human judgment call
    // (same stance as every other approval/rating flow in this codebase:
    // computed signals get shown, not silently applied).
    public record NominationRow(
        long Id, long HremployeeId, string EmpNo, string Name,
        ReadinessLevel ReadinessLevel, string? Note, DateTime NominatedDate,
        decimal TotalYearsExperience, bool? MeetsMinExperience,
        SuccessionNominationStatus Status);

    public record BenchStrengthRow(
        long KeyPositionId, string PosExecTypeName,
        BusinessImpactLevel BusinessImpact, ReplacementDifficultyLevel ReplacementDifficulty,
        int ReadyNowCount, int TotalSuccessorCount, bool IsSinglePointOfFailure,
        int? MinYearsExperience, int MeetsMinExperienceCount);

    public record EligibleCandidateRow(
        long HremployeeId, string EmpNo, string Name,
        decimal TotalYearsExperience, bool? MeetsMinExperience,
        string? HighestEducationLabel, bool EducationKeywordMatched);

    public record SuggestedCandidateRow(long HremployeeId, string EmpNo, string Name, string SourceLabel);

    // Nomination candidates should be informed by existing performance/potential
    // signal, not typed in blind — international succession practice pulls the
    // slate from the talent pool / high-potential quadrant rather than an open
    // search. Union of Talent_PoolEntry (explicit HR flag) and the most recent
    // Talent_PotentialRating=High per employee (any period — this module
    // deliberately doesn't ask HR to pick an evaluation period, unlike
    // /talent/nine-box, to keep the succession screen simple). Suggestion only:
    // HR can still nominate anyone via the free-text search below.
    public async Task<List<SuggestedCandidateRow>> GetSuggestedCandidatesAsync(string companyId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var poolEmployeeIds = await context.Talent_PoolEntries
            .Where(p => p.IsActive)
            .Select(p => p.HremployeeId)
            .Distinct()
            .ToListAsync(ct);

        var highPotentialIds = await context.Talent_PotentialRatings
            .Where(r => r.Level == PotentialLevel.High)
            .Select(r => r.HremployeeId)
            .Distinct()
            .ToListAsync(ct);

        var candidateIds = poolEmployeeIds.Union(highPotentialIds).ToList();
        if (candidateIds.Count == 0)
            return new();

        var employees = await context.Hremployee
            .Where(e => candidateIds.Contains(e.id) && e.companyid == companyId && e.ResignDate == null)
            .ToListAsync(ct);

        var poolSet = poolEmployeeIds.ToHashSet();
        var potentialSet = highPotentialIds.ToHashSet();

        return employees.Select(e =>
        {
            var sources = new List<string>();
            if (poolSet.Contains(e.id)) sources.Add("Talent Pool");
            if (potentialSet.Contains(e.id)) sources.Add("9-box: ศักยภาพสูง");
            return new SuggestedCandidateRow(e.id, e.EmpNo, $"{e.EmpName} {e.EmpSurname}", string.Join(" · ", sources));
        }).OrderBy(r => r.Name).ToList();
    }

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
            successorCounts.TryGetValue(k.Id, out var count) ? count : 0,
            k.MinYearsExperience)).ToList();
    }

    public async Task<long> AddKeyPositionAsync(string companyId, long posExecTypeId, BusinessImpactLevel impact, ReplacementDifficultyLevel difficulty, string? note, long actorUserId, int? minYearsExperience = null, CancellationToken ct = default)
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
            MinYearsExperience = minYearsExperience,
        };
        context.Succ_KeyPositions.Add(keyPosition);
        await context.SaveChangesAsync(ct);
        return keyPosition.Id;
    }

    public async Task UpdateKeyPositionAsync(long id, BusinessImpactLevel impact, ReplacementDifficultyLevel difficulty, string? note, int? minYearsExperience = null, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var keyPosition = await context.Succ_KeyPositions.FirstOrDefaultAsync(k => k.Id == id, ct)
            ?? throw new InvalidOperationException("ไม่พบตำแหน่งสำคัญนี้");

        keyPosition.BusinessImpact = impact;
        keyPosition.ReplacementDifficulty = difficulty;
        keyPosition.Note = note;
        keyPosition.MinYearsExperience = minYearsExperience;
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
        await using (var syncContext = await dbFactory.CreateDbContextAsync(ct))
        {
            // Lazy apply-on-read for every nomination still pending under this
            // key position — batched into one context/query rather than
            // calling SyncStatusFromJobAsync (which opens its own context) in
            // a loop, since a key position can carry several pending
            // nominations at once.
            var pending = await syncContext.Succ_SuccessorNominations
                .Where(n => n.KeyPositionId == keyPositionId && n.IsActive
                    && n.Status == SuccessionNominationStatus.PendingApproval && n.JobMasterId != null)
                .ToListAsync(ct);
            if (pending.Count > 0)
            {
                var jobIds = pending.Select(n => n.JobMasterId!.Value).ToList();
                var jobs = await syncContext.job_masters.Where(j => jobIds.Contains(j.jobmasterid)).ToListAsync(ct);
                foreach (var nomination in pending)
                {
                    var job = jobs.FirstOrDefault(j => j.jobmasterid == nomination.JobMasterId);
                    if (job is null || job.isJobClosed != true) continue;
                    nomination.Status = job.status == WorkflowEngineService.StatusCompleted
                        ? SuccessionNominationStatus.Approved
                        : SuccessionNominationStatus.Rejected;
                }
                await syncContext.SaveChangesAsync(ct);
            }
        }

        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var keyPosition = await context.Succ_KeyPositions.FirstOrDefaultAsync(k => k.Id == keyPositionId, ct);

        var nominations = await context.Succ_SuccessorNominations
            .Where(n => n.KeyPositionId == keyPositionId && n.IsActive)
            .OrderBy(n => n.ReadinessLevel)
            .ToListAsync(ct);
        if (nominations.Count == 0)
            return new();

        var employeeIds = nominations.Select(n => n.HremployeeId).Distinct().ToList();
        var employees = await context.Hremployee.Where(e => employeeIds.Contains(e.id)).ToListAsync(ct);
        var experiences = await context.Hrd_Experiences.Where(e => employeeIds.Contains(e.HremployeeId)).ToListAsync(ct);

        return nominations.Select(n =>
        {
            var emp = employees.FirstOrDefault(e => e.id == n.HremployeeId);
            var years = CalculateTotalYearsExperience(emp, experiences.Where(e => e.HremployeeId == n.HremployeeId).ToList());
            bool? meetsMin = keyPosition?.MinYearsExperience is int min ? years >= min : null;
            return new NominationRow(n.Id, n.HremployeeId, emp?.EmpNo ?? "?", emp is null ? $"#{n.HremployeeId}" : $"{emp.EmpName} {emp.EmpSurname}", n.ReadinessLevel, n.Note, n.NominatedDate, years, meetsMin, n.Status);
        }).ToList();
    }

    // Sums external Hrd_Experience durations (StartDate -> EndDate, or ->
    // today if still ongoing) plus internal tenure (Hremployee.WorkDate ->
    // today). Not persisted anywhere — recomputed on every read, same stance
    // as Okr_Objective progress% elsewhere in this codebase.
    private static decimal CalculateTotalYearsExperience(Hremployee? emp, List<Hrd_Experience> experiences)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        var externalDays = experiences.Sum(e =>
        {
            if (e.StartDate is not DateOnly start) return 0;
            var end = e.EndDate ?? today;
            return end > start ? end.DayNumber - start.DayNumber : 0;
        });

        var internalDays = 0;
        if (emp?.WorkDate is DateTime workDate)
        {
            var start = DateOnly.FromDateTime(workDate);
            if (today > start) internalDays = today.DayNumber - start.DayNumber;
        }

        return Math.Round((externalDays + internalDays) / 365m, 1);
    }

    public async Task<long> NominateAsync(long keyPositionId, long hremployeeId, ReadinessLevel readinessLevel, long actorUserId, string? note, string? actorEmpId = null, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var alreadyActive = await context.Succ_SuccessorNominations.AnyAsync(n => n.KeyPositionId == keyPositionId && n.HremployeeId == hremployeeId && n.IsActive, ct);
        if (alreadyActive)
            throw new InvalidOperationException("พนักงานคนนี้ถูกเสนอชื่อสำหรับตำแหน่งนี้ไปแล้ว");

        var workflow = await context.wf_workflows.FirstOrDefaultAsync(w => w.workflowcode == "SUCCESSION_NOMINATION_APPROVAL", ct)
            ?? throw new InvalidOperationException("ไม่พบ workflow 'SUCCESSION_NOMINATION_APPROVAL' — ติดต่อแอดมินให้ตั้งค่าก่อน");
        if (workflow.isactive != true)
            throw new InvalidOperationException($"workflow '{workflow.wname}' ปิดใช้งานอยู่ ไม่สามารถส่งอนุมัติได้");

        var keyPosition = await context.Succ_KeyPositions.FirstOrDefaultAsync(k => k.Id == keyPositionId, ct)
            ?? throw new InvalidOperationException("ไม่พบตำแหน่งสำคัญนี้");
        var posExecType = await context.Pos_ExecTypes.FirstOrDefaultAsync(p => p.Id == keyPosition.PosExecTypeId, ct);
        var candidate = await context.Hremployee.FirstOrDefaultAsync(e => e.id == hremployeeId, ct);

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

        var subject = $"เสนอชื่อผู้สืบทอดตำแหน่ง {posExecType?.Name}: {candidate?.EmpName} {candidate?.EmpSurname}";
        var jobId = await engine.StartJobAsync(workflow.workflowid, "Succ_SuccessorNomination", nomination.Id.ToString(),
            actorUserId, actorEmpId, subject, amount: null, ct);

        nomination.JobMasterId = jobId;
        await context.SaveChangesAsync(ct);

        return nomination.Id;
    }

    // Lazy apply-on-read for a single nomination — used by pages that only
    // need to refresh one row rather than a whole key position's list (see
    // GetNominationsAsync for the batched version).
    public async Task SyncStatusFromJobAsync(long nominationId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var nomination = await context.Succ_SuccessorNominations.FirstOrDefaultAsync(n => n.Id == nominationId, ct);
        if (nomination is null || nomination.Status != SuccessionNominationStatus.PendingApproval || nomination.JobMasterId is null)
            return;

        var job = await context.job_masters.FirstOrDefaultAsync(j => j.jobmasterid == nomination.JobMasterId, ct);
        if (job is null || job.isJobClosed != true)
            return;

        nomination.Status = job.status == WorkflowEngineService.StatusCompleted
            ? SuccessionNominationStatus.Approved
            : SuccessionNominationStatus.Rejected;
        await context.SaveChangesAsync(ct);
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
        if (nomination.Status == SuccessionNominationStatus.PendingApproval)
            throw new InvalidOperationException("ไม่สามารถถอดรายชื่อที่กำลังรออนุมัติได้ — กรุณารออนุมัติ/ปฏิเสธให้เสร็จก่อน");

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

        // Bench strength / single-point-of-failure only means something if
        // the nominations it counts have actually cleared governance — a
        // nomination still pending sign-off (or rejected) is not a real
        // successor yet. Sync first so a job that closed since the last read
        // is reflected before we filter.
        var pending = await context.Succ_SuccessorNominations
            .Where(n => keyPositionIds.Contains(n.KeyPositionId) && n.IsActive
                && n.Status == SuccessionNominationStatus.PendingApproval && n.JobMasterId != null)
            .ToListAsync(ct);
        if (pending.Count > 0)
        {
            var jobIds = pending.Select(n => n.JobMasterId!.Value).ToList();
            var jobs = await context.job_masters.Where(j => jobIds.Contains(j.jobmasterid)).ToListAsync(ct);
            foreach (var nomination in pending)
            {
                var job = jobs.FirstOrDefault(j => j.jobmasterid == nomination.JobMasterId);
                if (job is null || job.isJobClosed != true) continue;
                nomination.Status = job.status == WorkflowEngineService.StatusCompleted
                    ? SuccessionNominationStatus.Approved
                    : SuccessionNominationStatus.Rejected;
            }
            await context.SaveChangesAsync(ct);
        }

        var nominations = await context.Succ_SuccessorNominations
            .Where(n => keyPositionIds.Contains(n.KeyPositionId) && n.IsActive && n.Status == SuccessionNominationStatus.Approved)
            .ToListAsync(ct);

        var employeeIds = nominations.Select(n => n.HremployeeId).Distinct().ToList();
        var employees = await context.Hremployee.Where(e => employeeIds.Contains(e.id)).ToListAsync(ct);
        var experiences = await context.Hrd_Experiences.Where(e => employeeIds.Contains(e.HremployeeId)).ToListAsync(ct);

        return keyPositions.Select(k =>
        {
            var forThisPosition = nominations.Where(n => n.KeyPositionId == k.Id).ToList();
            var readyNow = forThisPosition.Count(n => n.ReadinessLevel == ReadinessLevel.ReadyNow);
            var meetsMinCount = k.MinYearsExperience is int min
                ? forThisPosition.Count(n =>
                    CalculateTotalYearsExperience(
                        employees.FirstOrDefault(e => e.id == n.HremployeeId),
                        experiences.Where(e => e.HremployeeId == n.HremployeeId).ToList()) >= min)
                : 0;
            return new BenchStrengthRow(
                k.Id, posExecTypeNames.TryGetValue(k.PosExecTypeId, out var name) ? name : $"#{k.PosExecTypeId}",
                k.BusinessImpact, k.ReplacementDifficulty,
                readyNow, forThisPosition.Count, forThisPosition.Count == 0,
                k.MinYearsExperience, meetsMinCount);
        }).ToList();
    }

    // Read-only reference panel for KeyPositionDetail.razor's nomination
    // expand toggle — sits next to the existing competency gap analysis.
    public async Task<(List<Hrd_Education> Education, List<Hrd_Experience> Experience)> GetEducationExperienceAsync(long hremployeeId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var education = await context.Hrd_Educations.Where(e => e.HremployeeId == hremployeeId).OrderByDescending(e => e.IsHighestDegree).ThenByDescending(e => e.FinishedDate).ToListAsync(ct);
        var experience = await context.Hrd_Experiences.Where(e => e.HremployeeId == hremployeeId).OrderByDescending(e => e.StartDate).ToListAsync(ct);
        return (education, experience);
    }

    // Additive discovery path alongside GetSuggestedCandidatesAsync's
    // Talent-Pool/High-Potential suggestions — searches every active
    // employee in the company by computed years of experience and an
    // optional education keyword (Hrd_Education.Level/Degree/Major/Institute
    // is free text, so this is a LIKE match, not a ranked-degree
    // comparison — matching this codebase's EntitySearchHelper convention
    // rather than inventing a fragile degree-ranking scheme).
    public async Task<List<EligibleCandidateRow>> SearchEligibleCandidatesAsync(string companyId, long keyPositionId, string? educationKeyword = null, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var keyPosition = await context.Succ_KeyPositions.FirstOrDefaultAsync(k => k.Id == keyPositionId, ct)
            ?? throw new InvalidOperationException("ไม่พบตำแหน่งสำคัญนี้");

        var alreadyNominated = await context.Succ_SuccessorNominations
            .Where(n => n.KeyPositionId == keyPositionId && n.IsActive)
            .Select(n => n.HremployeeId)
            .ToListAsync(ct);

        var employees = await context.Hremployee
            .Where(e => e.companyid == companyId && e.ResignDate == null && !alreadyNominated.Contains(e.id))
            .ToListAsync(ct);
        if (employees.Count == 0)
            return new();

        var employeeIds = employees.Select(e => e.id).ToList();
        var allExperience = await context.Hrd_Experiences.Where(e => employeeIds.Contains(e.HremployeeId)).ToListAsync(ct);
        var allEducation = await context.Hrd_Educations.Where(e => employeeIds.Contains(e.HremployeeId)).ToListAsync(ct);

        var trimmedKeyword = educationKeyword?.Trim();
        var hasKeyword = !string.IsNullOrWhiteSpace(trimmedKeyword);

        var rows = employees.Select(e =>
        {
            var years = CalculateTotalYearsExperience(e, allExperience.Where(x => x.HremployeeId == e.id).ToList());
            var eduRows = allEducation.Where(x => x.HremployeeId == e.id).ToList();
            var highest = eduRows.FirstOrDefault(x => x.IsHighestDegree) ?? eduRows.OrderByDescending(x => x.FinishedDate).FirstOrDefault();
            var highestLabel = highest is null ? null : $"{highest.Level} {highest.Degree} {highest.Major}".Trim();

            var matched = hasKeyword && eduRows.Any(x =>
                (x.Level?.Contains(trimmedKeyword!, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (x.Degree?.Contains(trimmedKeyword!, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (x.Major?.Contains(trimmedKeyword!, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (x.Institute?.Contains(trimmedKeyword!, StringComparison.OrdinalIgnoreCase) ?? false));

            bool? meetsMin = keyPosition.MinYearsExperience is int min ? years >= min : null;

            return new EligibleCandidateRow(e.id, e.EmpNo, $"{e.EmpName} {e.EmpSurname}", years, meetsMin, highestLabel, matched);
        });

        // Meeting the experience floor (if one is set) is a hard filter —
        // it's the config threshold HR chose for this position. The
        // education keyword is a soft filter to help HR narrow the list,
        // not a requirement (many valid successors won't match an exact
        // keyword against free-text degree data).
        if (keyPosition.MinYearsExperience is not null)
            rows = rows.Where(r => r.MeetsMinExperience == true);
        if (hasKeyword)
            rows = rows.Where(r => r.EducationKeywordMatched);

        return rows.OrderByDescending(r => r.TotalYearsExperience).ToList();
    }
}
