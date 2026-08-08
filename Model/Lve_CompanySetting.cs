using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

// One row per company — currently just the country, used to decide which
// country-specific LeaveType values (e.g. Ordination, a Thai-law-only leave
// category) are offered to that company. Null/no row = not configured yet,
// treated as Thailand for backward compatibility since every company in this
// system predates this feature and was built around Thai leave law.
[Table("Lve_CompanySetting")]
[Index(nameof(CompanyId), IsUnique = true)]
public class Lve_CompanySetting
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(6)]
    public string CompanyId { get; set; } = null!;

    // ISO 3166-1 alpha-2, e.g. "TH", "VN", "MM", "KH", "LA", "SG", "MY", "PH", "ID"
    [StringLength(2)]
    public string? CountryCode { get; set; }
}
