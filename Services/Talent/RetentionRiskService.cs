namespace HRM.Services.Talent;

using HRM.Models;
using HRM.Services.Shared;
using Microsoft.EntityFrameworkCore;

// Retention/flight-risk indicator — see Talent_RetentionRiskSettings for
// the design stance (transparent additive checklist, HR-tunable, opt-in,
// no comp data, no anonymity piercing). Computed live on read, never
// stored per employee: a risk label is a lens over current data, not a
// fact to snapshot, and storing it would demand its own retention/PDPA
// story for no benefit.
public class RetentionRiskService(IDbContextFactory<HRMContext> dbFactory)
{
    public record RiskRow(long HremployeeId, string EmpNo, string EmployeeName, int Score, List<string> Reasons, bool IsHighRisk);

    public async Task<Talent_RetentionRiskSettings> GetSettingsAsync(string companyId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        return await context.Talent_RetentionRiskSettingsList.FirstOrDefaultAsync(s => s.CompanyId == companyId, ct)
            ?? new Talent_RetentionRiskSettings { CompanyId = companyId };
    }

    public async Task SaveSettingsAsync(Talent_RetentionRiskSettings settings, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var existing = await context.Talent_RetentionRiskSettingsList.FirstOrDefaultAsync(s => s.CompanyId == settings.CompanyId, ct);
        if (existing is null)
        {
            context.Talent_RetentionRiskSettingsList.Add(settings);
        }
        else
        {
            existing.IsEnabled = settings.IsEnabled;
            existing.NewHireMonthsThreshold = settings.NewHireMonthsThreshold;
            existing.NewHireWeight = settings.NewHireWeight;
            existing.StagnationMonthsThreshold = settings.StagnationMonthsThreshold;
            existing.StagnationWeight = settings.StagnationWeight;
            existing.HighPerformerScorePercent = settings.HighPerformerScorePercent;
            existing.HighPerformerWeight = settings.HighPerformerWeight;
            existing.HighRiskScoreThreshold = settings.HighRiskScoreThreshold;
        }
        await context.SaveChangesAsync(ct);
    }

    // Empty list when the feature is disabled — callers render nothing, so
    // no employee is ever risk-labeled unless the company opted in.
    public async Task<List<RiskRow>> ComputeAsync(string companyId, CancellationToken ct = default)
    {
        var settings = await GetSettingsAsync(companyId, ct);
        if (!settings.IsEnabled) return new();

        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var candidates = await context.Hremployee.Where(e => e.companyid == companyId).ToListAsync(ct);
        // CanTransact, not raw ResignDate == null — someone in their notice
        // period is still working (see EmployeeStatusHelper), though for
        // THIS feature they're arguably the least interesting rows since
        // the risk already materialized; HR can read the list either way.
        var employees = candidates.Where(EmployeeStatusHelper.CanTransact).ToList();
        if (employees.Count == 0) return new();
        var empIds = employees.Select(e => e.id).ToList();

        var today = DateTime.Today;

        // Last position change per employee — latest Pos_PositionSlot_his
        // row where they were the incoming occupant; WorkDate fallback for
        // employees hired before slot history existed.
        var lastChanges = await context.Pos_PositionSlot_hises
            .Where(h => h.NewHremployeeId != null && empIds.Contains(h.NewHremployeeId.Value))
            .GroupBy(h => h.NewHremployeeId!.Value)
            .Select(g => new { HremployeeId = g.Key, LastChange = g.Max(h => h.ChangeDate) })
            .ToDictionaryAsync(x => x.HremployeeId, x => x.LastChange, ct);

        // Latest approved evaluation score per employee.
        var latestScores = (await context.Perf_EvaluationInstances
            .Where(i => empIds.Contains(i.HremployeeId) && i.Status == PerfInstanceStatus.Approved && i.FinalScorePercent != null)
            .Select(i => new { i.HremployeeId, i.FinalScorePercent, i.Id })
            .ToListAsync(ct))
            .GroupBy(i => i.HremployeeId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(i => i.Id).First().FinalScorePercent!.Value);

        var rows = new List<RiskRow>();
        foreach (var emp in employees)
        {
            var score = 0;
            var reasons = new List<string>();

            if (emp.WorkDate is DateTime workDate && workDate.AddMonths(settings.NewHireMonthsThreshold) >= today)
            {
                score += settings.NewHireWeight;
                reasons.Add($"พนักงานใหม่ (อายุงานยังไม่เกิน {settings.NewHireMonthsThreshold} เดือน)");
            }

            var lastMove = lastChanges.TryGetValue(emp.id, out var change) ? change : emp.WorkDate;
            if (lastMove is DateTime moveDate && moveDate.AddMonths(settings.StagnationMonthsThreshold) < today)
            {
                score += settings.StagnationWeight;
                reasons.Add($"ไม่มีการเปลี่ยนตำแหน่งเกิน {settings.StagnationMonthsThreshold} เดือน");
            }

            if (latestScores.TryGetValue(emp.id, out var perfScore) && perfScore >= settings.HighPerformerScorePercent)
            {
                score += settings.HighPerformerWeight;
                reasons.Add($"ผลงานระดับสูง ({perfScore:0.#}% — กลุ่มที่การลาออกเสียหายที่สุด)");
            }

            if (score == 0) continue; // only surface employees with at least one signal

            rows.Add(new RiskRow(emp.id, emp.EmpNo, $"{emp.EmpName} {emp.EmpSurname}", score, reasons,
                score >= settings.HighRiskScoreThreshold));
        }

        return rows.OrderByDescending(r => r.Score).ThenBy(r => r.EmpNo).ToList();
    }
}
