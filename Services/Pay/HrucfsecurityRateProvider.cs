namespace HRM.Services.Pay;

using HRM.Models;
using HRM.Services.Pay.Calculators;
using Microsoft.EntityFrameworkCore;

// Wraps Hrucfsecurity so the "01" magic-string social-security code lookup
// exists in exactly one named, documented place.
//
// Rates change over time (the wage cap rises in steps), so a company can hold several rows —
// InForce picks the one that applies on a given date: the latest EffectiveFrom not after that
// date, falling back to a row with no EffectiveFrom (legacy rows = always valid).
public class HrucfsecurityRateProvider : ISocialSecurityRateProvider
{
    public const string CurrentEmployeeSecurityCode = "01";

    private readonly IDbContextFactory<HRMContext> _dbFactory;

    public HrucfsecurityRateProvider(IDbContextFactory<HRMContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    // The single definition of "which rate row applies on this date" — reports and e-filing use it too.
    public static IQueryable<Hrucfsecurity> InForce(IQueryable<Hrucfsecurity> rows, string companyId, DateOnly asOf) =>
        rows.Where(x => x.companyid == companyId && x.SecurityCode == CurrentEmployeeSecurityCode
                        && (x.EffectiveFrom == null || x.EffectiveFrom <= asOf))
            .OrderByDescending(x => x.EffectiveFrom);

    private async Task<Hrucfsecurity> LoadAsync(string companyId, DateOnly? asOf, CancellationToken ct)
    {
        var date = asOf ?? DateOnly.FromDateTime(DateTime.Today);
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await InForce(context.Hrucfsecuritys.AsNoTracking(), companyId, date).FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException(
                $"No Hrucfsecurity rate configured for companyid='{companyId}', SecurityCode='{CurrentEmployeeSecurityCode}' effective on {date:yyyy-MM-dd}.");
    }

    public async Task<(decimal RatePercent, decimal WageCap)> GetCurrentRateAsync(string companyId, DateOnly? asOf = null, CancellationToken ct = default)
    {
        var config = await LoadAsync(companyId, asOf, ct);
        return (config.PercenSecurity ?? 0m, config.SecurityMoney ?? 0m);
    }

    public async Task<(decimal EmployeeRatePercent, decimal EmployerRatePercent, decimal WageCap)> GetCurrentRatesAsync(string companyId, DateOnly? asOf = null, CancellationToken ct = default)
    {
        var config = await LoadAsync(companyId, asOf, ct);
        var employee = config.PercenSecurity ?? 0m;
        return (employee, config.EmployerPercenSecurity ?? employee, config.SecurityMoney ?? 0m);
    }

    public async Task<int> GetMaxEntryAgeAsync(string companyId, DateOnly? asOf = null, CancellationToken ct = default)
    {
        var config = await LoadAsync(companyId, asOf, ct);
        return config.MaxEntryAge is int age and > 0 ? age : SsoCoverage.DefaultMaxEntryAge;
    }
}
