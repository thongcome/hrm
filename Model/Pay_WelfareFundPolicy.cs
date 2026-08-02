using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Config for the new Thai "กองทุนสงเคราะห์ลูกจ้าง" (Employee Welfare Fund)
// under the Labour Protection Act, taking effect 1 Oct 2026 for employers
// with 10+ employees (unless already covered by an equivalent Provident
// Fund). Deliberately NOT wired into PayrollCalculationService yet — the
// wage cap and exact remittance mechanics could not be confirmed against
// the primary legal text as of 2026-08, only cross-referenced secondary
// sources (rate + effective date are corroborated by multiple sources,
// wage cap is not). IsEnabled defaults to false so no company is affected
// until HR explicitly confirms details and turns it on. Time-versioned
// (EffectiveFrom/EffectiveTo) because the published rate schedule already
// includes a scheduled increase (0.25%->0.5% from 1 Oct 2031).
[Table("Pay_WelfareFundPolicy")]
public class Pay_WelfareFundPolicy
{
    [Key]
    public long Id { get; set; }

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
