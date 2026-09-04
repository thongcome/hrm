using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Engagement;

// Activity-based points earning — the "earn coins from doing things, not just
// kudos" side (AutoX ask), now pluggable: each earning activity is an
// IPointEarningActivity provider with its own Code, an Eng_PointsRule enrols it
// per company with a point value, and SyncEarnedPointsAsync asks each enrolled
// provider what qualifies and writes idempotent Eng_PointsLedger rows
// (RefTable+RefId dedup). Another module joins the program by shipping a
// provider and being enrolled from the setup page — no change here.
public class EngPointsService(IDbContextFactory<HRMContext> dbFactory, PointActivityRegistry registry)
{
    public const string ManualCode = "MANUAL";

    // Redeem statuses that have committed points (mirror EngagementService).
    private static readonly EngRedeemStatus[] CommittedStatuses =
        { EngRedeemStatus.PendingApproval, EngRedeemStatus.Approved, EngRedeemStatus.Fulfilled };

    // sensible default points when enrolling a known activity
    private static readonly Dictionary<string, int> DefaultPoints = new(StringComparer.OrdinalIgnoreCase)
    {
        ["LMS_COMPLETION"] = 20,
        ["TENURE_ANNIVERSARY"] = 50,
    };

    public async Task<int> GetActivityPointsAsync(long hremployeeId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        return await context.Eng_PointsLedgers.Where(l => l.HremployeeId == hremployeeId && l.IsActive)
            .SumAsync(l => (int?)l.Points, ct) ?? 0;
    }

    // ---- rules (enrolment) ----
    public async Task<List<Eng_PointsRule>> GetRulesAsync(string companyId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        return await context.Eng_PointsRules.Where(r => r.CompanyId == companyId).ToListAsync(ct);
    }

    // Activities registered in code but not yet enrolled for this company —
    // the choices the setup page's "add activity" button offers.
    public async Task<List<IPointEarningActivity>> GetAvailableActivitiesAsync(string companyId, CancellationToken ct = default)
    {
        var enrolled = (await GetRulesAsync(companyId, ct)).Select(r => r.ActivityCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return registry.All.Where(a => !enrolled.Contains(a.Code)).ToList();
    }

    public async Task AddRuleAsync(string companyId, string activityCode, CancellationToken ct = default)
    {
        var activity = registry.Find(activityCode) ?? throw new InvalidOperationException("ไม่พบกิจกรรมนี้ในระบบ");
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        if (await context.Eng_PointsRules.AnyAsync(r => r.CompanyId == companyId && r.ActivityCode == activityCode, ct))
            throw new InvalidOperationException("กิจกรรมนี้ถูกเพิ่มไปแล้ว");
        context.Eng_PointsRules.Add(new Eng_PointsRule
        {
            CompanyId = companyId, ActivityCode = activity.Code, ActivityName = activity.Name,
            Points = DefaultPoints.GetValueOrDefault(activity.Code, 10), IsActive = true,
        });
        await context.SaveChangesAsync(ct);
    }

    public async Task SaveRuleAsync(Eng_PointsRule rule, CancellationToken ct = default)
    {
        if (rule.Points < 0) throw new InvalidOperationException("คะแนนต้องไม่ติดลบ");
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var existing = await context.Eng_PointsRules.FirstOrDefaultAsync(r => r.Id == rule.Id, ct)
            ?? throw new InvalidOperationException("ไม่พบกติกานี้แล้ว");
        existing.Points = rule.Points;
        existing.IsActive = rule.IsActive;
        await context.SaveChangesAsync(ct);
    }

    // Enrol every registered activity that isn't enrolled yet, so points work
    // out of the box; HR still adds future ones via the button and can disable
    // or re-point any of these.
    public async Task EnsureDefaultRulesAsync(string companyId, CancellationToken ct = default)
    {
        var available = await GetAvailableActivitiesAsync(companyId, ct);
        foreach (var a in available)
            await AddRuleAsync(companyId, a.Code, ct);
    }

    // ---- earning ----
    public async Task<int> SyncEarnedPointsAsync(string companyId, long? actorUserId = null, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var rules = await context.Eng_PointsRules.Where(r => r.CompanyId == companyId && r.IsActive).ToListAsync(ct);
        if (rules.Count == 0) return 0;

        var existing = (await context.Eng_PointsLedgers
                .Where(l => l.CompanyId == companyId && l.RefTable != null)
                .Select(l => l.RefTable + "|" + l.RefId).ToListAsync(ct))
            .ToHashSet();

        var toAdd = new List<Eng_PointsLedger>();
        foreach (var rule in rules)
        {
            var provider = registry.Find(rule.ActivityCode);
            if (provider is null) continue; // enrolled code with no provider (module removed) — skip
            var events = await provider.DetectAsync(context, companyId, ct);
            foreach (var ev in events)
            {
                var key = ev.RefTable + "|" + ev.RefId;
                if (!existing.Add(key)) continue;
                toAdd.Add(new Eng_PointsLedger
                {
                    CompanyId = companyId, HremployeeId = ev.HremployeeId, ActivityCode = rule.ActivityCode,
                    Points = rule.Points, RefTable = ev.RefTable, RefId = ev.RefId, Note = ev.Note,
                    AwardedByUserId = actorUserId, EarnedDate = DateTime.Now, IsActive = true,
                });
            }
        }

        if (toAdd.Count == 0) return 0;
        context.Eng_PointsLedgers.AddRange(toAdd);
        await context.SaveChangesAsync(ct);
        return toAdd.Count;
    }

    public async Task AwardManualAsync(string companyId, long hremployeeId, int points, string? note, long actorUserId, CancellationToken ct = default)
    {
        if (points <= 0) throw new InvalidOperationException("คะแนนต้องมากกว่า 0");
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        context.Eng_PointsLedgers.Add(new Eng_PointsLedger
        {
            CompanyId = companyId, HremployeeId = hremployeeId, ActivityCode = ManualCode,
            Points = points, Note = note, AwardedByUserId = actorUserId, EarnedDate = DateTime.Now, IsActive = true,
        });
        await context.SaveChangesAsync(ct);
    }

    // ---- ledger + activity-name resolution ----
    public record LedgerRow(long Id, long HremployeeId, string EmpName, string ActivityCode, string ActivityName, int Points, string? Note, DateTime EarnedDate);

    public async Task<List<LedgerRow>> GetLedgerAsync(string companyId, int take = 300, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var rows = await context.Eng_PointsLedgers.Where(l => l.CompanyId == companyId && l.IsActive)
            .OrderByDescending(l => l.EarnedDate).Take(take).ToListAsync(ct);
        if (rows.Count == 0) return new();

        var ids = rows.Select(r => r.HremployeeId).Distinct().ToList();
        var names = await context.Hremployee.Where(e => ids.Contains(e.id))
            .Select(e => new { e.id, Name = (e.EmpName ?? "") + " " + (e.EmpSurname ?? "") })
            .ToDictionaryAsync(e => e.id, e => e.Name.Trim(), ct);

        return rows.Select(r => new LedgerRow(
            r.Id, r.HremployeeId, names.GetValueOrDefault(r.HremployeeId, $"#{r.HremployeeId}"),
            r.ActivityCode, ActivityDisplayName(r.ActivityCode), r.Points, r.Note, r.EarnedDate)).ToList();
    }

    public string ActivityDisplayName(string code)
        => code == ManualCode ? "HR ให้แต้มพิเศษ" : registry.Find(code)?.Name ?? code;

    public string ActivityHowEarned(string code)
        => code == ManualCode ? "HR มอบแต้มให้เป็นรายกรณี" : registry.Find(code)?.HowEarned ?? "";

    // ---- per-employee balances (HR view: who has how many points) ----
    public record EmployeeBalanceRow(long HremployeeId, string EmpNo, string EmpName, int KudosPoints, int ActivityPoints, int Earned, int Spent, int Available);

    public async Task<List<EmployeeBalanceRow>> GetEmployeeBalancesAsync(string companyId, string? search, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var kudos = await context.Eng_Recognitions.Where(k => k.CompanyId == companyId && k.IsActive)
            .GroupBy(k => k.ToHremployeeId).Select(g => new { Id = g.Key, P = g.Sum(x => x.Points) }).ToListAsync(ct);
        var activity = await context.Eng_PointsLedgers.Where(l => l.CompanyId == companyId && l.IsActive)
            .GroupBy(l => l.HremployeeId).Select(g => new { Id = g.Key, P = g.Sum(x => x.Points) }).ToListAsync(ct);
        var spent = await context.Eng_RedeemRequests.Where(r => r.CompanyId == companyId && r.IsActive && CommittedStatuses.Contains(r.Status))
            .GroupBy(r => r.HremployeeId).Select(g => new { Id = g.Key, P = g.Sum(x => x.PointsSpent) }).ToListAsync(ct);

        var kudosMap = kudos.ToDictionary(x => x.Id, x => x.P);
        var actMap = activity.ToDictionary(x => x.Id, x => x.P);
        var spentMap = spent.ToDictionary(x => x.Id, x => x.P);

        var empIds = kudosMap.Keys.Concat(actMap.Keys).Concat(spentMap.Keys).Distinct().ToList();
        if (empIds.Count == 0) return new();

        var emps = await context.Hremployee.Where(e => empIds.Contains(e.id))
            .Select(e => new { e.id, e.EmpNo, Name = (e.EmpName ?? "") + " " + (e.EmpSurname ?? "") }).ToListAsync(ct);

        var rows = emps.Select(e =>
        {
            var k = kudosMap.GetValueOrDefault(e.id);
            var a = actMap.GetValueOrDefault(e.id);
            var s = spentMap.GetValueOrDefault(e.id);
            return new EmployeeBalanceRow(e.id, e.EmpNo, e.Name.Trim(), k, a, k + a, s, k + a - s);
        });

        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.Trim();
            rows = rows.Where(r => r.EmpName.Contains(q, StringComparison.OrdinalIgnoreCase)
                || (r.EmpNo?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        return rows.OrderByDescending(r => r.Available).ToList();
    }
}
