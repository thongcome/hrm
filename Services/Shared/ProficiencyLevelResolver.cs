using HRM.Models;

namespace HRM.Services.Shared;

// Comp_ProficiencyLevel is effective-dated (see Model/Comp_ProficiencyLevel.cs) — a competency
// can have several rows for the same (CompetencyId, Level) over time as HR revises the BARS
// anchor wording. "Current" is resolved here, not stored, the same idiom as
// Services/Pay/MinimumWageRule.cs and every other effective-dated table in this codebase:
// pure and unit-testable, callers load the rows and pass them in.
public static class ProficiencyLevelResolver
{
    public static Comp_ProficiencyLevel? Current(IEnumerable<Comp_ProficiencyLevel> rows, long competencyId, int level, DateOnly asOf) =>
        rows
            .Where(l => l.CompetencyId == competencyId && l.Level == level && l.IsActive && l.EffectiveFrom <= asOf
                        && (l.EffectiveTo is null || l.EffectiveTo >= asOf))
            .OrderByDescending(l => l.EffectiveFrom)
            .FirstOrDefault();

    // One current row per (CompetencyId, Level) among the given rows — what a scoring form or
    // job-profile page needs after batch-loading every historical row for a set of competencies.
    public static List<Comp_ProficiencyLevel> CurrentOnly(IEnumerable<Comp_ProficiencyLevel> rows, DateOnly asOf) =>
        rows
            .Where(l => l.IsActive && l.EffectiveFrom <= asOf && (l.EffectiveTo is null || l.EffectiveTo >= asOf))
            .GroupBy(l => (l.CompetencyId, l.Level))
            .Select(g => g.OrderByDescending(l => l.EffectiveFrom).First())
            .ToList();
}
