using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Comp;

// Org-wide competency intelligence — the layer the competency module was
// missing (it had only the library CRUD). Reads three things set-based (no
// per-employee loop, so it scales to thousands): who occupies which position
// group (Pos_PositionSlot → PosExecTypeId), what competencies+levels that
// group requires (Job_CompetencyRequirement), and each person's assessed level
// (Idp_CompetencyAssessment, manager rating preferred over self). From those it
// computes coverage (who's been assessed), the gap (required − current) per
// competency, and the critical gaps that become training priorities — the
// bridge from competency data to LMS.
public class CompetencyAnalyticsService(IDbContextFactory<HRMContext> dbFactory)
{
    public record CompetencyRow(
        long CompetencyId, string Name, string Category,
        int EmployeesRequiring, int Assessed, double AvgRequired, double? AvgCurrent,
        int Meeting, int Below, int CriticalGaps, double? AvgGap);

    public record CategorySummary(string Category, int Competencies, int CriticalGaps, double? AvgGap);

    public record CompetencyOverview(
        int CompetencyCount, int EmployeesInScope, int AssessedEmployees, double CoveragePercent,
        int TotalCriticalGaps, int TotalBelowTarget,
        List<CompetencyRow> Rows, List<CategorySummary> Categories);

    public async Task<CompetencyOverview> GetOverviewAsync(string companyId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        // active employees of the company
        var activeEmpIds = await context.Hremployee
            .Where(e => e.companyid == companyId && e.ResignDate == null)
            .Select(e => e.id).ToListAsync(ct);
        if (activeEmpIds.Count == 0)
            return new(0, 0, 0, 0, 0, 0, new(), new());

        // employee → position group (from the slot they occupy)
        var slots = await context.Pos_PositionSlots
            .Where(s => s.CompanyId == companyId && s.IsActive && s.HremployeeId != null && s.PosExecTypeId != null)
            .Select(s => new { EmpId = s.HremployeeId!.Value, Pos = s.PosExecTypeId!.Value })
            .ToListAsync(ct);
        var empToPos = slots
            .Where(s => activeEmpIds.Contains(s.EmpId))
            .GroupBy(s => s.EmpId).ToDictionary(g => g.Key, g => g.First().Pos);
        if (empToPos.Count == 0)
            return new(0, empToPos.Count, 0, 0, 0, 0, new(), new());

        var posIds = empToPos.Values.Distinct().ToList();

        // requirements per position group
        var reqs = await context.Job_CompetencyRequirements
            .Where(r => posIds.Contains(r.PosExecTypeId) && r.IsActive)
            .Select(r => new { r.PosExecTypeId, r.CompetencyId, r.RequiredLevel, r.IsCritical })
            .ToListAsync(ct);
        var reqByPos = reqs.GroupBy(r => r.PosExecTypeId).ToDictionary(g => g.Key, g => g.ToList());

        // assessments for the in-scope employees → effective level (manager ?? self)
        var empIds = empToPos.Keys.ToList();
        var assess = await context.Idp_CompetencyAssessments
            .Where(a => empIds.Contains(a.HremployeeId))
            .Select(a => new { a.HremployeeId, a.CompetencyId, a.Source, a.RatedLevel })
            .ToListAsync(ct);
        var effByEmpComp = assess
            .GroupBy(a => (a.HremployeeId, a.CompetencyId))
            .ToDictionary(g => g.Key, g =>
            {
                var mgr = g.Where(x => x.Source == IdpAssessmentSource.Manager).Select(x => (int?)x.RatedLevel).FirstOrDefault();
                var self = g.Where(x => x.Source == IdpAssessmentSource.Self).Select(x => (int?)x.RatedLevel).FirstOrDefault();
                return mgr ?? self;
            });

        // competency + category names
        var comps = await context.Comp_Competencies.Select(c => new { c.Id, c.Name, c.CategoryId }).ToListAsync(ct);
        var compMeta = comps.ToDictionary(c => c.Id, c => (c.Name, c.CategoryId));
        var cats = await context.Comp_Categories.Select(c => new { c.Id, c.Name }).ToListAsync(ct);
        var catName = cats.ToDictionary(c => c.Id, c => c.Name);

        // aggregate
        var agg = new Dictionary<long, (int Requiring, int Assessed, long SumReq, long SumCur, int Meeting, int Below, int Critical)>();
        var assessedEmployees = new HashSet<long>();

        foreach (var (empId, pos) in empToPos)
        {
            if (!reqByPos.TryGetValue(pos, out var posReqs)) continue;
            foreach (var r in posReqs)
            {
                var a = agg.GetValueOrDefault(r.CompetencyId);
                a.Requiring++;
                a.SumReq += r.RequiredLevel;
                if (effByEmpComp.TryGetValue((empId, r.CompetencyId), out var eff) && eff is int cur)
                {
                    a.Assessed++;
                    a.SumCur += cur;
                    assessedEmployees.Add(empId);
                    if (cur >= r.RequiredLevel) a.Meeting++;
                    else { a.Below++; if (r.IsCritical) a.Critical++; }
                }
                agg[r.CompetencyId] = a;
            }
        }

        var rows = agg.Select(kv =>
        {
            var (name, catId) = compMeta.GetValueOrDefault(kv.Key, ($"#{kv.Key}", 0L));
            var v = kv.Value;
            double? avgCur = v.Assessed == 0 ? null : Math.Round((double)v.SumCur / v.Assessed, 2);
            double avgReq = v.Requiring == 0 ? 0 : Math.Round((double)v.SumReq / v.Requiring, 2);
            double? avgGap = v.Assessed == 0 ? null : Math.Round(avgReq - avgCur!.Value, 2);
            return new CompetencyRow(kv.Key, name, catName.GetValueOrDefault(catId, "ไม่ระบุหมวด"),
                v.Requiring, v.Assessed, avgReq, avgCur, v.Meeting, v.Below, v.Critical, avgGap);
        })
        .OrderByDescending(r => r.CriticalGaps).ThenByDescending(r => r.AvgGap ?? -99)
        .ToList();

        var categories = rows.GroupBy(r => r.Category)
            .Select(g => new CategorySummary(g.Key, g.Count(), g.Sum(r => r.CriticalGaps),
                g.Where(r => r.AvgGap != null).Select(r => r.AvgGap!.Value).DefaultIfEmpty().Average() is var av && g.Any(r => r.AvgGap != null) ? Math.Round(av, 2) : (double?)null))
            .OrderByDescending(c => c.CriticalGaps).ToList();

        var coverage = empToPos.Count == 0 ? 0 : Math.Round(assessedEmployees.Count * 100.0 / empToPos.Count, 1);

        return new CompetencyOverview(
            rows.Count, empToPos.Count, assessedEmployees.Count, coverage,
            rows.Sum(r => r.CriticalGaps), rows.Sum(r => r.Below),
            rows, categories);
    }
}
