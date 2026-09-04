using HRM.Models;
using HRM.Services.Idp;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Talent;

// The cross-module "talent card" for one employee — the single view a
// calibration committee wants: performance grade, 9-box potential placement,
// competency readiness, IDP status, succession standing, and talent-pool flag,
// each pulled from the module that owns it. Everything here is a cheap
// per-employee read (no company-wide scan), so it opens instantly from a
// search, unlike the org-wide analytics services.
public class TalentReviewService(IDbContextFactory<HRMContext> dbFactory, IdpAssessmentService idpService)
{
    public record SuccessorStanding(string PositionName, ReadinessLevel Readiness, bool Approved);

    public record TalentCard(
        long HremployeeId, string EmpNo, string EmpName, string? PositionName, string? OrgName,
        // performance
        string? PerfGrade, decimal? PerfScorePercent, string? PerfPeriod, bool PerfApproved,
        // potential + 9-box
        PotentialLevel? Potential, string PerformanceBand, int NineBox, string NineBoxLabel,
        // competency
        int CompetencyMet, int CompetencyTotal, int CompetencyCritical,
        // idp
        IdpPlanStatus? IdpStatus, int IdpActionCount,
        // succession + pool
        List<SuccessorStanding> Successor, bool InTalentPool);

    public async Task<TalentCard?> GetCardAsync(long hremployeeId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var emp = await context.Hremployee.FirstOrDefaultAsync(e => e.id == hremployeeId, ct);
        if (emp is null) return null;

        // position + org names (best-effort)
        string? posName = null, orgName = null;
        if (!string.IsNullOrEmpty(emp.PosCode))
            posName = await context.pos_positions.Where(p => p.pos_code == emp.PosCode).Select(p => p.name).FirstOrDefaultAsync(ct);
        if (!string.IsNullOrEmpty(emp.DeptgrpCode))
            orgName = await context.com_organizations.Where(o => o.code == emp.DeptgrpCode).Select(o => o.name).FirstOrDefaultAsync(ct);

        // latest performance result
        var perf = await context.Perf_EvaluationInstances
            .Where(i => i.HremployeeId == hremployeeId && i.FinalGrade != null)
            .OrderByDescending(i => i.EvaluationPeriodId)
            .FirstOrDefaultAsync(ct);
        string? perfPeriod = null;
        if (perf is not null)
            perfPeriod = await context.Perf_EvaluationPeriods.Where(p => p.Id == perf.EvaluationPeriodId).Select(p => p.Name).FirstOrDefaultAsync(ct);

        // latest potential rating
        var potential = await context.Talent_PotentialRatings
            .Where(r => r.HremployeeId == hremployeeId)
            .OrderByDescending(r => r.EvaluationPeriodId)
            .Select(r => (PotentialLevel?)r.Level).FirstOrDefaultAsync(ct);

        // competency readiness (current position)
        var gaps = await idpService.GetGapAnalysisAsync(hremployeeId, ct);
        var scored = gaps.Where(g => g.Gap is not null).ToList();
        var met = scored.Count(g => g.Gap <= 0);
        var critical = scored.Count(g => g.IsCritical && g.Gap > 0);

        // IDP
        var idp = await context.Idp_Plans.Where(p => p.HremployeeId == hremployeeId)
            .OrderByDescending(p => p.Id).FirstOrDefaultAsync(ct);
        var idpActions = idp is null ? 0
            : await context.Idp_DevelopmentActions.CountAsync(a => a.PlanId == idp.Id, ct);

        // succession standing
        var noms = await context.Succ_SuccessorNominations
            .Where(n => n.HremployeeId == hremployeeId && n.IsActive)
            .ToListAsync(ct);
        var successor = new List<SuccessorStanding>();
        foreach (var n in noms)
        {
            var kp = await context.Succ_KeyPositions.FirstOrDefaultAsync(k => k.Id == n.KeyPositionId, ct);
            if (kp is null) continue;
            var pn = await context.Pos_ExecTypes.Where(p => p.Id == kp.PosExecTypeId).Select(p => p.Name).FirstOrDefaultAsync(ct);
            successor.Add(new SuccessorStanding(pn ?? $"#{kp.PosExecTypeId}", n.ReadinessLevel, n.Status == SuccessionNominationStatus.Approved));
        }

        var inPool = await context.Talent_PoolEntries.AnyAsync(p => p.HremployeeId == hremployeeId && p.IsActive, ct);

        // 9-box: performance band from score, potential band from rating
        var perfBand = PerformanceBand(perf?.FinalScorePercent);
        var (box, boxLabel) = NineBox(perfBand, potential);

        return new TalentCard(
            hremployeeId, emp.EmpNo, $"{emp.EmpName} {emp.EmpSurname}".Trim(), posName, orgName,
            perf?.FinalGrade, perf?.FinalScorePercent, perfPeriod, perf?.Status == PerfInstanceStatus.Approved,
            potential, perfBand, box, boxLabel,
            met, scored.Count, critical,
            idp?.Status, idpActions,
            successor, inPool);
    }

    // Performance axis banding (thresholds mirror a typical 9-box: <70 low,
    // 70–85 medium, ≥85 high). Unknown when there's no scored result.
    private static string PerformanceBand(decimal? score) => score switch
    {
        null => "unknown",
        >= 85 => "high",
        >= 70 => "medium",
        _ => "low",
    };

    // Maps (performance band, potential level) to the 1–9 box + a Thai label.
    private static (int Box, string Label) NineBox(string perfBand, PotentialLevel? potential)
    {
        if (perfBand == "unknown" || potential is null) return (0, "ยังประเมินไม่ครบ");
        var perf = perfBand switch { "high" => 3, "medium" => 2, _ => 1 };
        var pot = (int)potential.Value; // Low1/Med2/High3
        var box = (pot - 1) * 3 + perf; // 1..9
        var label = (perf, pot) switch
        {
            (3, 3) => "ดาวเด่น (Star)",
            (3, 2) or (2, 3) => "ผู้มีศักยภาพสูง (High Potential)",
            (2, 2) => "กำลังหลัก (Core Player)",
            (3, 1) => "ผู้เชี่ยวชาญ (Expert)",
            (1, 3) => "เพชรในตม (Rough Diamond)",
            (2, 1) or (1, 2) => "ต้องพัฒนา (Developing)",
            _ => "ต้องปรับปรุง (Under-performer)",
        };
        return (box, label);
    }
}
