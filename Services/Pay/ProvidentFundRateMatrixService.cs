namespace HRM.Services.Pay;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Resolves a SUGGESTED employer contribution rate from
// Pay_ProvidentFundRateMatrixRule (years of service x employee's own rate
// band -> fixed rate or "match employee rate"). This is only a suggestion
// fed into Pay_ProvidentFundRateChangeRequest for HR to review/override
// before approval — it never writes to Pay_ProvidentFundElection directly.
public class ProvidentFundRateMatrixService
{
    private readonly IDbContextFactory<HRMContext> _dbFactory;

    public ProvidentFundRateMatrixService(IDbContextFactory<HRMContext> dbFactory) => _dbFactory = dbFactory;

    public record MatrixSuggestion(decimal? SuggestedRate, string? MatchedRuleDescription);

    public async Task<MatrixSuggestion> SuggestCompanyRateAsync(long policyId, decimal yearsOfService, decimal employeeRate, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var rules = await context.Pay_ProvidentFundRateMatrixRules
            .Where(r => r.PolicyId == policyId)
            .OrderBy(r => r.SortOrder)
            .ToListAsync(ct);

        var matched = rules.FirstOrDefault(r =>
            yearsOfService >= r.MinYearsOfService && (r.MaxYearsOfService == null || yearsOfService < r.MaxYearsOfService) &&
            employeeRate >= r.EmployeeRateMin && employeeRate <= r.EmployeeRateMax);

        if (matched is null)
            return new MatrixSuggestion(null, null);

        var rate = matched.ResultType == ProvidentFundMatrixResultType.MatchEmployeeRate ? employeeRate : matched.FixedCompanyRate;
        var resultText = matched.ResultType == ProvidentFundMatrixResultType.MatchEmployeeRate
            ? "เท่ากับอัตราเงินสะสมของพนักงาน"
            : $"{matched.FixedCompanyRate:0.00}%";
        var desc = $"อายุงาน {matched.MinYearsOfService}-{matched.MaxYearsOfService?.ToString() ?? "+"} ปี, เงินสะสม {matched.EmployeeRateMin:0.0}-{matched.EmployeeRateMax:0.0}% → {resultText}";

        return new MatrixSuggestion(rate, desc);
    }
}
