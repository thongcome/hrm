namespace HRM.Services.Pos;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Seeds a decimal job-grade ladder into pos_position_level (the job-grade master,
// owner-chosen 2026-09-03) and stamps each active employee's grade
// (Hremployee.EMPLEVEL_CODE) from their position code — A01→grade 1 … A07→grade 7.
// plevel is decimal so a half-rung like 7.5 can be added by hand later; the
// backfill assigns the integer rung of the employee's position. Idempotent: adds
// only missing grade rungs, sets only an empty EMPLEVEL_CODE (never overwrites a
// grade HR set by hand).
public class GradeLadderService(IDbContextFactory<HRMContext> dbFactory)
{
    public record LadderResult(int GradesSeeded, int EmployeesGraded);

    private const int MaxRung = 7;

    public async Task<LadderResult> BuildAsync(string companyId, long actorUserId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        // 1) Grade rungs 1..7 (pos_position_level is global, not company-scoped).
        var existing = await context.pos_position_levels.ToListAsync(ct);
        var codes = existing.Where(l => l.code != null).Select(l => l.code!).ToHashSet();
        var seeded = 0;
        for (var n = 1; n <= MaxRung; n++)
        {
            var code = n.ToString();
            if (codes.Contains(code)) continue;
            context.pos_position_levels.Add(new pos_position_level
            {
                code = code,
                name = $"ระดับ {n}",
                plevel = n,
                update_by = actorUserId.ToString(),   // stamp the acting user (sc_userid); DB col nvarchar(7) until widened, so id not loginname for now
                update_date = DateTime.Now,
            });
            seeded++;
        }
        if (seeded > 0) await context.SaveChangesAsync(ct);

        // 2) Stamp each active employee's grade from their POS_CODE rung
        //    (only where EMPLEVEL_CODE is still empty).
        var emps = await context.Hremployee
            .Where(e => e.companyid == companyId && e.ResignDate == null
                        && e.PosCode != null && e.EmplevelCode == null)
            .ToListAsync(ct);
        var graded = 0;
        foreach (var e in emps)
        {
            var digits = new string(e.PosCode!.Where(char.IsDigit).ToArray());
            if (!int.TryParse(digits, out var posNo) || posNo < 1 || posNo > MaxRung) continue;
            // Grade 1 = highest (CEO), grade 7 = lowest (worker). Position codes
            // run the other way (A01 worker … A07 CEO), so invert: A07→1, A01→7.
            var grade = MaxRung + 1 - posNo;
            e.EmplevelCode = grade.ToString();
            graded++;
        }
        if (graded > 0) await context.SaveChangesAsync(ct);

        return new LadderResult(seeded, graded);
    }
}
