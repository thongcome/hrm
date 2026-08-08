namespace HRM.Services.Leave;

using HRM.Models;

// Decides which LeaveType values are offered to a given company based on its
// configured country (Lve_CompanySetting.CountryCode). Only Ordination is
// country-restricted today (Thai-law-only leave category) — every other
// LeaveType is treated as universal. Null/unset country code preserves prior
// behavior (everything visible) since this predates the country concept.
public static class LeaveCountryHelper
{
    public static bool IsApplicable(LeaveType type, string? countryCode)
    {
        if (type != LeaveType.Ordination) return true;
        return string.IsNullOrWhiteSpace(countryCode) || countryCode == "TH";
    }

    public static readonly (string Code, string Label)[] CommonCountries =
    [
        ("TH", "ไทย (Thailand)"),
        ("VN", "เวียดนาม (Vietnam)"),
        ("MM", "เมียนมา (Myanmar)"),
        ("KH", "กัมพูชา (Cambodia)"),
        ("LA", "ลาว (Laos)"),
        ("SG", "สิงคโปร์ (Singapore)"),
        ("MY", "มาเลเซีย (Malaysia)"),
        ("PH", "ฟิลิปปินส์ (Philippines)"),
        ("ID", "อินโดนีเซีย (Indonesia)"),
    ];
}
