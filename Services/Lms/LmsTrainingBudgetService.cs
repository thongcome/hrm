namespace HRM.Services.Lms;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

public class LmsTrainingBudgetService(IDbContextFactory<HRMContext> dbFactory)
{
    public record BudgetVsActualResult(decimal BudgetAmount, decimal ActualTotal, decimal Variance);

    // Sums ActualCost of every Lms_CourseSession that started within the
    // given fiscal year, for courses belonging to companyId, and compares
    // against the (optional) Lms_TrainingBudget row for that year.
    public async Task<BudgetVsActualResult> GetBudgetVsActualAsync(string companyId, int fiscalYear, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var courseIds = await context.Lms_Courses.Where(c => c.CompanyId == companyId).Select(c => c.Id).ToListAsync(ct);

        var actualTotal = await context.Lms_CourseSessions
            .Where(s => courseIds.Contains(s.CourseId) && s.StartDate.Year == fiscalYear && s.ActualCost != null)
            .SumAsync(s => s.ActualCost ?? 0m, ct);

        var budget = await context.Lms_TrainingBudgets
            .Where(b => b.CompanyId == companyId && b.FiscalYear == fiscalYear && b.OrganizationId == null)
            .Select(b => (decimal?)b.BudgetAmount)
            .FirstOrDefaultAsync(ct) ?? 0m;

        return new BudgetVsActualResult(budget, actualTotal, budget - actualTotal);
    }
}
