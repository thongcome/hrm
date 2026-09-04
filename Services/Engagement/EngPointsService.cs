using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Engagement;

// Activity-based points earning — the "earn coins from doing things, not just
// kudos" side of engagement (AutoX ask). Rules (Eng_PointsRule) say how many
// points each source is worth per company; SyncEarnedPointsAsync scans for
// qualifying activity that hasn't been credited yet and writes idempotent
// Eng_PointsLedger rows (RefTable+RefId prevents double-award). There is no
// scheduler in this app, so the sync runs on demand from the admin page and
// whenever the balance is read. Kudos points stay on Eng_Recognition; this only
// covers the non-kudos sources.
public class EngPointsService(IDbContextFactory<HRMContext> dbFactory)
{
    public async Task<int> GetActivityPointsAsync(long hremployeeId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        return await context.Eng_PointsLedgers
            .Where(l => l.HremployeeId == hremployeeId && l.IsActive)
            .SumAsync(l => (int?)l.Points, ct) ?? 0;
    }

    public record SourceTotal(EngPointsSource Source, int Points, int Count);

    public async Task<List<SourceTotal>> GetBreakdownAsync(long hremployeeId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var rows = await context.Eng_PointsLedgers.Where(l => l.HremployeeId == hremployeeId && l.IsActive).ToListAsync(ct);
        return rows.GroupBy(l => l.Source)
            .Select(g => new SourceTotal(g.Key, g.Sum(x => x.Points), g.Count()))
            .OrderByDescending(s => s.Points).ToList();
    }

    // Scans activity for the company and credits any not-yet-recorded events.
    // Returns how many ledger rows were created. Safe to run repeatedly.
    public async Task<int> SyncEarnedPointsAsync(string companyId, long? actorUserId = null, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var rules = await context.Eng_PointsRules
            .Where(r => r.CompanyId == companyId && r.IsActive)
            .ToListAsync(ct);
        if (rules.Count == 0) return 0;

        var existing = await context.Eng_PointsLedgers
            .Where(l => l.CompanyId == companyId && l.RefTable != null)
            .Select(l => l.RefTable + "|" + l.RefId)
            .ToListAsync(ct);
        var seen = existing.ToHashSet();
        var toAdd = new List<Eng_PointsLedger>();

        // ---- Training completion (LMS) ----
        var trainingRule = rules.FirstOrDefault(r => r.Source == EngPointsSource.TrainingCompletion);
        if (trainingRule is not null)
        {
            var completions = await (
                from e in context.Lms_Enrollments
                join emp in context.Hremployee on e.HremployeeId equals emp.id
                where e.Status == EnrollmentStatus.Completed && emp.companyid == companyId
                select new { e.Id, e.HremployeeId }).ToListAsync(ct);

            foreach (var c in completions)
            {
                var key = "Lms_Enrollment|" + c.Id;
                if (seen.Contains(key)) continue;
                toAdd.Add(new Eng_PointsLedger
                {
                    CompanyId = companyId, HremployeeId = c.HremployeeId, Source = EngPointsSource.TrainingCompletion,
                    Points = trainingRule.Points, RefTable = "Lms_Enrollment", RefId = c.Id.ToString(),
                    Note = "จบหลักสูตรอบรม", AwardedByUserId = actorUserId, EarnedDate = DateTime.Now, IsActive = true,
                });
                seen.Add(key);
            }
        }

        // ---- Tenure anniversary ----
        var tenureRule = rules.FirstOrDefault(r => r.Source == EngPointsSource.TenureAnniversary);
        if (tenureRule is not null)
        {
            var today = DateTime.Today;
            var emps = await context.Hremployee
                .Where(e => e.companyid == companyId && e.ResignDate == null && e.WorkDate != null)
                .Select(e => new { e.id, e.WorkDate })
                .ToListAsync(ct);

            foreach (var e in emps)
            {
                var wd = e.WorkDate!.Value.Date;
                // full years completed as of today (only if the anniversary has passed)
                var years = today.Year - wd.Year;
                if (wd.AddYears(years) > today) years--;
                if (years < 1) continue;

                var key = "TenureAnniversary|" + e.id + ":" + years;
                if (seen.Contains(key)) continue;
                toAdd.Add(new Eng_PointsLedger
                {
                    CompanyId = companyId, HremployeeId = e.id, Source = EngPointsSource.TenureAnniversary,
                    Points = tenureRule.Points, RefTable = "TenureAnniversary", RefId = e.id + ":" + years,
                    Note = $"ครบ {years} ปีการทำงาน", AwardedByUserId = actorUserId, EarnedDate = DateTime.Now, IsActive = true,
                });
                seen.Add(key);
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
            CompanyId = companyId, HremployeeId = hremployeeId, Source = EngPointsSource.Manual,
            Points = points, Note = note, AwardedByUserId = actorUserId, EarnedDate = DateTime.Now, IsActive = true,
        });
        await context.SaveChangesAsync(ct);
    }

    public record LedgerRow(long Id, long HremployeeId, string EmpName, EngPointsSource Source, int Points, string? Note, DateTime EarnedDate);

    public async Task<List<LedgerRow>> GetLedgerAsync(string companyId, int take = 300, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var rows = await context.Eng_PointsLedgers
            .Where(l => l.CompanyId == companyId && l.IsActive)
            .OrderByDescending(l => l.EarnedDate).Take(take).ToListAsync(ct);
        if (rows.Count == 0) return new();

        var ids = rows.Select(r => r.HremployeeId).Distinct().ToList();
        var names = await context.Hremployee.Where(e => ids.Contains(e.id))
            .Select(e => new { e.id, Name = (e.EmpName ?? "") + " " + (e.EmpSurname ?? "") })
            .ToDictionaryAsync(e => e.id, e => e.Name.Trim(), ct);

        return rows.Select(r => new LedgerRow(
            r.Id, r.HremployeeId, names.GetValueOrDefault(r.HremployeeId, $"#{r.HremployeeId}"),
            r.Source, r.Points, r.Note, r.EarnedDate)).ToList();
    }

    public async Task<List<Eng_PointsRule>> GetRulesAsync(string companyId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        return await context.Eng_PointsRules.Where(r => r.CompanyId == companyId).ToListAsync(ct);
    }

    public async Task SaveRuleAsync(Eng_PointsRule rule, CancellationToken ct = default)
    {
        if (rule.Points < 0) throw new InvalidOperationException("คะแนนต้องไม่ติดลบ");
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var existing = await context.Eng_PointsRules.FirstOrDefaultAsync(r => r.Id == rule.Id, ct)
            ?? throw new InvalidOperationException("ไม่พบกติกานี้แล้ว");
        existing.Points = rule.Points;
        existing.IsActive = rule.IsActive;
        existing.Description = rule.Description;
        await context.SaveChangesAsync(ct);
    }

    // Idempotent default rules so activity-earning works out of the box; HR can
    // change points or disable a source afterward.
    public async Task EnsureDefaultRulesAsync(string companyId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var have = await context.Eng_PointsRules.Where(r => r.CompanyId == companyId).Select(r => r.Source).ToListAsync(ct);
        var defaults = new (EngPointsSource Source, int Points, string Desc)[]
        {
            (EngPointsSource.TrainingCompletion, 20, "แต้มเมื่อจบหลักสูตรอบรม"),
            (EngPointsSource.TenureAnniversary, 50, "แต้มเมื่อครบรอบปีการทำงาน"),
            (EngPointsSource.Manual, 0, "ให้แต้มพิเศษโดย HR"),
        };
        var added = false;
        foreach (var d in defaults.Where(d => !have.Contains(d.Source)))
        {
            context.Eng_PointsRules.Add(new Eng_PointsRule
            {
                CompanyId = companyId, Source = d.Source, Points = d.Points, Description = d.Desc, IsActive = true,
            });
            added = true;
        }
        if (added) await context.SaveChangesAsync(ct);
    }
}
