namespace HRM.Services.Pos;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Grade-based promotion. Owner rule (2026-09-03): job grade drives promotion —
// whoever holds the higher grade has priority to be promoted first. So the
// candidate list is ranked by the employee's current grade (plevel from
// pos_position_level, resolved via Hremployee.EMPLEVEL_CODE) descending.
// Promoting sets the new grade on the employee and records a
// Pos_GradeChangeHistory row (never a silent edit).
public class PromotionService(IDbContextFactory<HRMContext> dbFactory)
{
    public record GradeRung(string Code, string? Name, decimal? Plevel);
    public record Candidate(long HremployeeId, string EmpNo, string FullName, string? OrgName,
        string? PositionName, string? GradeCode, decimal? Plevel);

    public async Task<List<GradeRung>> GetGradesAsync(CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        return await context.pos_position_levels
            .Where(l => l.code != null)
            .OrderByDescending(l => l.plevel)
            .Select(l => new GradeRung(l.code!, l.name, l.plevel))
            .ToListAsync(ct);
    }

    public async Task<List<Candidate>> GetCandidatesAsync(string companyId, long? organizationId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var plevelByCode = await context.pos_position_levels.Where(l => l.code != null)
            .ToDictionaryAsync(l => l.code!, l => l.plevel, ct);
        var orgNames = await context.com_organizations.ToDictionaryAsync(o => o.id, o => o.name, ct);
        var execNames = await context.Pos_ExecTypes.Where(t => t.CompanyId == companyId)
            .ToDictionaryAsync(t => t.Code, t => t.Name, ct);

        var q = context.Hremployee.Where(e => e.companyid == companyId && e.ResignDate == null);
        if (organizationId is long oid) q = q.Where(e => e.OrganizationId == oid);
        var emps = await q.ToListAsync(ct);

        return emps.Select(e => new Candidate(
                e.id, e.EmpNo, $"{e.EmpName} {e.EmpSurname}".Trim(),
                e.OrganizationId is long o && orgNames.TryGetValue(o, out var on) ? on : null,
                e.PosCode != null && execNames.TryGetValue(e.PosCode, out var pn) ? pn : e.PosCode,
                e.EmplevelCode,
                e.EmplevelCode != null && plevelByCode.TryGetValue(e.EmplevelCode, out var pl) ? pl : null))
            // Grade 1 = highest (CEO); higher grade (lower plevel) = promotion
            // priority, so rank ascending. Ungraded employees sort last.
            .OrderBy(c => c.Plevel ?? decimal.MaxValue)
            .ThenBy(c => c.EmpNo)
            .ToList();
    }

    public async Task PromoteAsync(long hremployeeId, string newGradeCode, string? reason,
        DateTime effectiveDate, long actorUserId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var emp = await context.Hremployee.FirstOrDefaultAsync(e => e.id == hremployeeId, ct)
            ?? throw new InvalidOperationException("ไม่พบพนักงาน");
        var grades = await context.pos_position_levels.Where(l => l.code != null).ToListAsync(ct);
        var newG = grades.FirstOrDefault(g => g.code == newGradeCode)
            ?? throw new InvalidOperationException("ไม่พบระดับที่เลือก");
        var oldCode = emp.EmplevelCode;
        var oldG = oldCode != null ? grades.FirstOrDefault(g => g.code == oldCode) : null;
        if (oldCode == newGradeCode)
            throw new InvalidOperationException("ระดับใหม่ตรงกับระดับเดิม");

        emp.EmplevelCode = newGradeCode;
        context.Pos_GradeChangeHistories.Add(new Pos_GradeChangeHistory
        {
            HremployeeId = emp.id,
            EmpNo = emp.EmpNo,
            CompanyId = emp.companyid,
            OldGradeCode = oldCode,
            NewGradeCode = newGradeCode,
            OldPlevel = oldG?.plevel,
            NewPlevel = newG.plevel,
            IsPromotion = (newG.plevel ?? 99m) < (oldG?.plevel ?? 99m), // grade 1 = highest, so a lower plevel is a promotion
            Reason = reason,
            EffectiveDate = effectiveDate,
            ChangedByUserId = actorUserId,
            ChangedDate = DateTime.Now,
        });
        await context.SaveChangesAsync(ct);
    }

    public async Task<List<Pos_GradeChangeHistory>> GetHistoryAsync(string companyId, long? hremployeeId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var q = context.Pos_GradeChangeHistories.Where(h => h.CompanyId == companyId);
        if (hremployeeId is long id) q = q.Where(h => h.HremployeeId == id);
        return await q.OrderByDescending(h => h.ChangedDate).Take(200).ToListAsync(ct);
    }
}
