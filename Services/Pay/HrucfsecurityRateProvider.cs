namespace HRM.Services.Pay;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Wraps Hrucfsecurity so the "01" magic-string social-security code lookup
// exists in exactly one named, documented place instead of being duplicated
// inline (it appeared both in PayrollProcess.razor line ~404 and in the
// legacy Services\Payroll\PayrollCalculationService.cs stub line ~27).
public class HrucfsecurityRateProvider : ISocialSecurityRateProvider
{
    public const string CurrentEmployeeSecurityCode = "01";

    private readonly IDbContextFactory<HRMContext> _dbFactory;

    public HrucfsecurityRateProvider(IDbContextFactory<HRMContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<(decimal RatePercent, decimal WageCap)> GetCurrentRateAsync(string companyId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var config = await context.Hrucfsecuritys
            .Where(x => x.companyid == companyId && x.SecurityCode == CurrentEmployeeSecurityCode)
            .FirstOrDefaultAsync(ct);

        if (config == null)
            throw new InvalidOperationException(
                $"No Hrucfsecurity rate configured for companyid='{companyId}', SecurityCode='{CurrentEmployeeSecurityCode}'.");

        return (config.PercenSecurity ?? 0m, config.SecurityMoney ?? 0m);
    }
}
