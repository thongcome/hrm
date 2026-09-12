using Advance.Payroll.Contracts;
using Advance.Payroll.Data;
using Microsoft.EntityFrameworkCore;

namespace Advance.Payroll.Engine;

// Ported from HRM Services/Pay/HrucfsecurityRateProvider.cs — HRMContext -> PayrollDbContext
// is the only change; Hrucfsecurity is Payroll's own table (copied into
// Advance.Payroll.Domain), so no Contracts seam was needed here.
public class HrucfsecurityRateProvider : ISocialSecurityRateProvider
{
    public const string CurrentEmployeeSecurityCode = "01";

    private readonly IDbContextFactory<PayrollDbContext> _dbFactory;

    public HrucfsecurityRateProvider(IDbContextFactory<PayrollDbContext> dbFactory)
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

    public async Task<(decimal EmployeeRatePercent, decimal EmployerRatePercent, decimal WageCap)> GetCurrentRatesAsync(string companyId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var config = await context.Hrucfsecuritys
            .Where(x => x.companyid == companyId && x.SecurityCode == CurrentEmployeeSecurityCode)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException(
                $"No Hrucfsecurity rate configured for companyid='{companyId}', SecurityCode='{CurrentEmployeeSecurityCode}'.");
        var employee = config.PercenSecurity ?? 0m;
        return (employee, config.EmployerPercenSecurity ?? employee, config.SecurityMoney ?? 0m);
    }
}
