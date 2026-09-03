using HRM.Models;
using HRM.Services.Shared;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Perf;

// Calibration lets HR/a committee review everyone's scores for one
// evaluation period side-by-side and override before each person is
// individually submitted for approval via PerfApprovalService — purely
// additive, no change to PerfScoringService/PerfApprovalService. Only
// instances that are Status=PendingApproval and JobMasterId==null are
// eligible (not yet submitted into the real workflow); ApplyAdjustmentAsync
// re-checks this at write time, not just at grid-read time.
public class PerfCalibrationService(IDbContextFactory<HRMContext> dbFactory)
{
    public record GridEmployeeRow(
        long HremployeeId, long InstanceId, string EmpNo, string Name, string? OrganizationName,
        decimal? FinalScorePercent, string? FinalGrade);

    public record CalibrationGridResult(
        List<string> OrganizationNames, List<string> Grades,
        Dictionary<(string Organization, string Grade), List<GridEmployeeRow>> Cells,
        List<GridEmployeeRow> NoGrade);

    public async Task<Perf_CalibrationSession> OpenSessionAsync(
        string companyId, long evaluationPeriodId, string name, long? organizationId, long actorUserId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var session = new Perf_CalibrationSession
        {
            CompanyId = companyId,
            EvaluationPeriodId = evaluationPeriodId,
            Name = name,
            OrganizationId = organizationId,
            Status = CalibrationSessionStatus.Open,
            CreatedByUserId = actorUserId,
            CreatedDate = DateTime.Now,
        };
        context.Perf_CalibrationSessions.Add(session);
        await context.SaveChangesAsync(ct);
        return session;
    }

    public async Task<CalibrationGridResult> GetGridAsync(long sessionId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var session = await context.Perf_CalibrationSessions.FirstOrDefaultAsync(s => s.Id == sessionId, ct)
            ?? throw new InvalidOperationException("ไม่พบ session นี้แล้ว");

        var query = context.Perf_EvaluationInstances
            .Where(i => i.EvaluationPeriodId == session.EvaluationPeriodId
                && i.Status == PerfInstanceStatus.PendingApproval && i.JobMasterId == null);

        if (session.OrganizationId is long orgId)
        {
            var scopeEmployees = await OrgEmployeeResolverHelper.ResolveOrganizationSubtreeAsync(context, session.CompanyId, orgId, ct);
            var scopeEmployeeIds = scopeEmployees.Select(e => e.id).ToList();
            query = query.Where(i => scopeEmployeeIds.Contains(i.HremployeeId));
        }

        var instances = await query.ToListAsync(ct);

        var rows = instances.Select(i => new GridEmployeeRow(
            i.HremployeeId, i.Id, i.SnapshotEmpNo ?? "-", i.SnapshotEmpName ?? "-",
            i.SnapshotOrganizationName ?? "ไม่ระบุหน่วยงาน", i.FinalScorePercent, i.FinalGrade)).ToList();

        var gradedRows = rows.Where(r => !string.IsNullOrEmpty(r.FinalGrade)).ToList();
        var noGradeRows = rows.Where(r => string.IsNullOrEmpty(r.FinalGrade)).ToList();

        var orgNames = gradedRows.Select(r => r.OrganizationName!).Distinct().OrderBy(o => o).ToList();
        var grades = gradedRows.Select(r => r.FinalGrade!).Distinct().OrderBy(g => g).ToList();

        var cells = gradedRows
            .GroupBy(r => (Organization: r.OrganizationName!, Grade: r.FinalGrade!))
            .ToDictionary(g => g.Key, g => g.ToList());

        return new CalibrationGridResult(orgNames, grades, cells, noGradeRows);
    }

    // AutoX #4 — bell-curve / forced-distribution recommendation. Per grade band,
    // compares the ACTUAL % of people who landed in it (across this session's
    // scope — a dept subtree, or the whole company when the session has no org)
    // against the band's configured TargetDistributionPercent, so a supervisor
    // can see "you have 30% in A, target is 15% → เกินเกณฑ์" and re-balance.
    public record DistributionRow(
        string Grade, int SortOrder, decimal? TargetPercent,
        int ActualCount, decimal ActualPercent, decimal? VariancePercent, string Status);

    public async Task<List<DistributionRow>> GetDistributionRecommendationAsync(long sessionId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var session = await context.Perf_CalibrationSessions.FirstOrDefaultAsync(s => s.Id == sessionId, ct)
            ?? throw new InvalidOperationException("ไม่พบ session นี้แล้ว");

        var query = context.Perf_EvaluationInstances
            .Where(i => i.EvaluationPeriodId == session.EvaluationPeriodId
                && i.Status == PerfInstanceStatus.PendingApproval && i.JobMasterId == null);

        if (session.OrganizationId is long orgId)
        {
            var scopeEmployees = await OrgEmployeeResolverHelper.ResolveOrganizationSubtreeAsync(context, session.CompanyId, orgId, ct);
            var scopeEmployeeIds = scopeEmployees.Select(e => e.id).ToList();
            query = query.Where(i => scopeEmployeeIds.Contains(i.HremployeeId));
        }

        var graded = (await query.ToListAsync(ct)).Where(i => !string.IsNullOrEmpty(i.FinalGrade)).ToList();
        var total = graded.Count;
        var countByGrade = graded.GroupBy(i => i.FinalGrade!).ToDictionary(g => g.Key, g => g.Count());

        var gradeBands = await context.Perf_GradeBands
            .Where(g => g.EvaluationPeriodId == session.EvaluationPeriodId && g.IsActive)
            .OrderBy(g => g.SortOrder)
            .ToListAsync(ct);

        var result = new List<DistributionRow>();
        foreach (var band in gradeBands)
        {
            var actualCount = countByGrade.TryGetValue(band.Grade, out var c) ? c : 0;
            var actualPercent = total == 0 ? 0m : Math.Round(actualCount * 100m / total, 1);
            decimal? variance = null;
            string status;
            if (band.TargetDistributionPercent is decimal target)
            {
                variance = Math.Round(actualPercent - target, 1);
                status = variance > 0m ? "over" : variance < 0m ? "under" : "ok";
            }
            else status = "no-target";
            result.Add(new DistributionRow(band.Grade, band.SortOrder, band.TargetDistributionPercent,
                actualCount, actualPercent, variance, status));
        }
        return result;
    }

    public record AdjustmentPreview(decimal AdjustedScorePercent, string? AdjustedGrade);

    public async Task<AdjustmentPreview> PreviewAdjustmentAsync(long instanceId, decimal newScorePercent, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var instance = await context.Perf_EvaluationInstances.FirstOrDefaultAsync(i => i.Id == instanceId, ct)
            ?? throw new InvalidOperationException("ไม่พบรายการประเมินนี้แล้ว");

        var gradeBands = await context.Perf_GradeBands
            .Where(g => g.EvaluationPeriodId == instance.EvaluationPeriodId && g.IsActive)
            .OrderBy(g => g.SortOrder)
            .ToListAsync(ct);

        var matched = gradeBands.FirstOrDefault(g => newScorePercent >= g.MinPercent && newScorePercent <= g.MaxPercent);
        return new AdjustmentPreview(newScorePercent, matched?.Grade);
    }

    public async Task ApplyAdjustmentAsync(
        long sessionId, long instanceId, decimal adjustedScorePercent, string? adjustedGrade,
        string justification, long actorUserId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(justification))
            throw new InvalidOperationException("ต้องระบุเหตุผลการปรับคะแนนทุกครั้ง");

        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var session = await context.Perf_CalibrationSessions.FirstOrDefaultAsync(s => s.Id == sessionId, ct)
            ?? throw new InvalidOperationException("ไม่พบ session นี้แล้ว");
        if (session.Status != CalibrationSessionStatus.Open)
            throw new InvalidOperationException("Session นี้ปิดแล้ว ไม่สามารถปรับคะแนนต่อได้");

        var instance = await context.Perf_EvaluationInstances.FirstOrDefaultAsync(i => i.Id == instanceId, ct)
            ?? throw new InvalidOperationException("ไม่พบรายการประเมินนี้แล้ว");
        if (instance.Status != PerfInstanceStatus.PendingApproval || instance.JobMasterId != null)
            throw new InvalidOperationException("ปรับคะแนนได้เฉพาะรายการที่ยังไม่ถูกส่งเข้าอนุมัติเท่านั้น");

        context.Perf_CalibrationAdjustments.Add(new Perf_CalibrationAdjustment
        {
            SessionId = sessionId,
            InstanceId = instanceId,
            OriginalScorePercent = instance.FinalScorePercent,
            OriginalGrade = instance.FinalGrade,
            AdjustedScorePercent = adjustedScorePercent,
            AdjustedGrade = adjustedGrade,
            Justification = justification,
            AdjustedByUserId = actorUserId,
            AdjustedDate = DateTime.Now,
        });

        instance.FinalScorePercent = adjustedScorePercent;
        instance.FinalGrade = adjustedGrade;

        await context.SaveChangesAsync(ct);
    }

    public async Task CloseSessionAsync(long sessionId, long actorUserId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var session = await context.Perf_CalibrationSessions.FirstOrDefaultAsync(s => s.Id == sessionId, ct)
            ?? throw new InvalidOperationException("ไม่พบ session นี้แล้ว");
        if (session.Status == CalibrationSessionStatus.Closed) return;

        session.Status = CalibrationSessionStatus.Closed;
        session.ClosedDate = DateTime.Now;
        await context.SaveChangesAsync(ct);
    }

    public async Task<List<Perf_CalibrationSession>> GetSessionsAsync(string companyId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        return await context.Perf_CalibrationSessions
            .Where(s => s.CompanyId == companyId)
            .OrderByDescending(s => s.CreatedDate)
            .ToListAsync(ct);
    }

    public async Task<List<Perf_CalibrationAdjustment>> GetAdjustmentsAsync(long sessionId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        return await context.Perf_CalibrationAdjustments
            .Where(a => a.SessionId == sessionId)
            .OrderByDescending(a => a.AdjustedDate)
            .ToListAsync(ct);
    }
}
