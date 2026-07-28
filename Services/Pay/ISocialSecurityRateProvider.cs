namespace HRM.Services.Pay;

public interface ISocialSecurityRateProvider
{
    Task<(decimal RatePercent, decimal WageCap)> GetCurrentRateAsync(string companyId, CancellationToken ct = default);
}
