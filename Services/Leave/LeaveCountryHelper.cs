namespace HRM.Services.Leave;

using HRM.Models;

// Decides which Lve_LeaveType catalog rows are offered to a given company
// based on its configured country (Lve_CompanySetting.CountryCode), reading
// each type's own Lve_LeaveType.ApplicableCountryCode rather than a
// hardcoded rule (e.g. Ordination used to be hardcoded Thailand-only here —
// that's now just data: ApplicableCountryCode = "TH" on that catalog row).
// Null/unset country code preserves prior behavior (everything visible)
// since this predates the country concept.
public static class LeaveCountryHelper
{
    public static bool IsApplicable(Lve_LeaveType type, string? countryCode)
    {
        if (string.IsNullOrWhiteSpace(type.ApplicableCountryCode)) return true;
        return string.IsNullOrWhiteSpace(countryCode) || countryCode == type.ApplicableCountryCode;
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
