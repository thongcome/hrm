namespace Advance.Payroll.Contracts;

// Ported verbatim from HRM Services/Pay/ISocialSecurityRateProvider.cs. Not one of the 7
// "external module" seams (Hrucfsecurity is Payroll's own table, copied into
// Advance.Payroll.Domain) — kept as an interface anyway because
// Advance.Payroll.Engine's PayrollCalculationService already depended on the
// abstraction, not the concrete HrucfsecurityRateProvider, and there's no reason to
// change that now.
public interface ISocialSecurityRateProvider
{
    Task<(decimal RatePercent, decimal WageCap)> GetCurrentRateAsync(string companyId, CancellationToken ct = default);

    async Task<(decimal EmployeeRatePercent, decimal EmployerRatePercent, decimal WageCap)> GetCurrentRatesAsync(string companyId, CancellationToken ct = default)
    {
        var (rate, cap) = await GetCurrentRateAsync(companyId, ct);
        return (rate, rate, cap);
    }
}
