namespace HRM.Services.Pay;

public interface ISocialSecurityRateProvider
{
    // asOf = วันเริ่มงวดเงินเดือนที่กำลังคำนวณ — อัตรา/เพดานประกันสังคมเปลี่ยนตามวันที่มีผล (audit H-03)
    // ไม่ระบุ = ใช้วันนี้
    Task<(decimal RatePercent, decimal WageCap)> GetCurrentRateAsync(string companyId, DateOnly? asOf = null, CancellationToken ct = default);

    // ฝั่งนายจ้าง (audit M10): ค่าเริ่มต้น = อัตราเดียวกับลูกจ้าง (กฎหมายกำหนดเท่ากัน เว้นช่วงลดพิเศษที่ต่างกัน)
    async Task<(decimal EmployeeRatePercent, decimal EmployerRatePercent, decimal WageCap)> GetCurrentRatesAsync(string companyId, DateOnly? asOf = null, CancellationToken ct = default)
    {
        var (rate, cap) = await GetCurrentRateAsync(companyId, asOf, ct);
        return (rate, rate, cap);
    }

    // ม.33: อายุวันเริ่มงานสูงสุดที่ยังเป็นผู้ประกันตน — ค่าเริ่มต้นตามกฎหมาย 60
    Task<int> GetMaxEntryAgeAsync(string companyId, DateOnly? asOf = null, CancellationToken ct = default) =>
        Task.FromResult(HRM.Services.Pay.Calculators.SsoCoverage.DefaultMaxEntryAge);
}
