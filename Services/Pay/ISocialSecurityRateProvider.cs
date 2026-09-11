namespace HRM.Services.Pay;

public interface ISocialSecurityRateProvider
{
    Task<(decimal RatePercent, decimal WageCap)> GetCurrentRateAsync(string companyId, CancellationToken ct = default);

    // ฝั่งนายจ้าง (audit M10): ค่าเริ่มต้น = อัตราเดียวกับลูกจ้าง (กฎหมายกำหนดเท่ากัน เว้นช่วงลดพิเศษที่ต่างกัน)
    async Task<(decimal EmployeeRatePercent, decimal EmployerRatePercent, decimal WageCap)> GetCurrentRatesAsync(string companyId, CancellationToken ct = default)
    {
        var (rate, cap) = await GetCurrentRateAsync(companyId, ct);
        return (rate, rate, cap);
    }
}
