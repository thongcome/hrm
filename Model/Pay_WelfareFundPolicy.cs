using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Config for the new Thai "กองทุนสงเคราะห์ลูกจ้าง" (Employee Welfare Fund),
// Labour Protection Act B.E. 2541, Chapter 13 (มาตรา 126-138):
//   - มาตรา 130: mandatory for employers with 10+ employees, EXCEPT those
//     already providing an equivalent provident fund / statutory welfare
//     scheme for all staff.
//   - มาตรา 131: contribution rate (both employee "เงินสะสม" and employer
//     "เงินสมทบ") is capped by statute at a MAXIMUM of 5% of wages; the
//     actual rate is set by ministerial regulation (currently 0.25% each,
//     stepping to 0.5% each per the published schedule) — the statute
//     itself sets NO wage cap for the calculation base, only the 5% rate
//     ceiling, confirmed 2026-08 against the statutory text directly (not
//     just secondary sources). WageCapPerMonth below defaults to null
//     (uncapped) accordingly.
//   - มาตรา 133: employee (or beneficiary, on death) receives back their
//     accumulated contributions + employer match + interest on ANY exit
//     from employment, not just termination.
// Collection start date (1 Oct 2026) is corroborated by the Department of
// Labour Protection and Welfare's own site (ewf.labour.go.th) and multiple
// secondary sources. The exact end date of the first rate tier (30 Sep
// 2030 vs 2031) still has minor conflicting reports across sources as of
// 2026-08 — seeded here using the majority-corroborated 2031 boundary.
// IsEnabled defaults to false regardless, so no company is affected by any
// of this until HR reviews and explicitly turns a rate tier on.
[Table("Pay_WelfareFundPolicy")]
public class Pay_WelfareFundPolicy
{
    [Key]
    public long Id { get; set; }

    // Stable human-facing code for this record (audit: every master/document table needs one beyond the surrogate Id).
    [StringLength(30)]
    public string? Code { get; set; }

    [Required, StringLength(6)]
    public string CompanyId { get; set; } = null!;

    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal EmployeeContributionRate { get; set; }
    [Column(TypeName = "decimal(5,2)")]
    public decimal CompanyContributionRate { get; set; }

    // Null = no wage cap applied (uncapped). Set once the actual cap (if
    // any) is confirmed against the ministerial regulation.
    [Column(TypeName = "decimal(15,2)")]
    public decimal? WageCapPerMonth { get; set; }

    public bool IsEnabled { get; set; }

    [StringLength(1000)]
    public string? Note { get; set; }
}
