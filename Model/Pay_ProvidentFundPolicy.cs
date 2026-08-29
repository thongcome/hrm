using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Company-level config for กองทุนสำรองเลี้ยงชีพ (Provident Fund Act B.E. 2530).
// A voluntary employer-sponsored fund — NOT the same as Pay_WelfareFundPolicy
// (กองทุนสงเคราะห์ลูกจ้าง, มาตรา 130 พ.ร.บ.คุ้มครองแรงงาน). Per มาตรา 130, an
// employer with an active/enabled provident fund is EXEMPT from the mandatory
// welfare fund — PayrollCalculationService.cs checks IsEnabled here to skip
// the welfare fund deduction automatically.
// Rate bounds: statute caps both employee and employer contribution at 2-15%
// of wages, and the employer rate must not be lower than the employee rate.
// RateChangeLimitPerYear/vesting schedule have no statutory standard — each
// fund's own "ข้อบังคับกองทุน" sets these, so they're HR-configurable here,
// not hardcoded.
[Table("Pay_ProvidentFundPolicy")]
public class Pay_ProvidentFundPolicy
{
    [Key]
    public long Id { get; set; }

    // Stable human-facing code for this record (audit: every master/document table needs one beyond the surrogate Id).
    [StringLength(30)]
    public string? PolicyCode { get; set; }

    [Required, StringLength(6)]
    public string CompanyId { get; set; } = null!;

    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal MinEmployeeRate { get; set; } = 2m;
    [Column(TypeName = "decimal(5,2)")]
    public decimal MaxEmployeeRate { get; set; } = 15m;
    [Column(TypeName = "decimal(5,2)")]
    public decimal MinCompanyRate { get; set; } = 2m;
    [Column(TypeName = "decimal(5,2)")]
    public decimal MaxCompanyRate { get; set; } = 15m;

    // Null = unlimited rate changes per year (no statutory limit exists).
    public int? RateChangeLimitPerYear { get; set; }

    // true = company has an active provident fund -> exempt from the
    // mandatory Employee Welfare Fund (มาตรา 130). Defaults false like
    // Pay_WelfareFundPolicy so no company is affected until HR opts in.
    public bool IsEnabled { get; set; }

    [StringLength(1000)]
    public string? Note { get; set; }

    // Optional per company — when true, vesting/exit calculations use
    // Pay_ProvidentFundMembershipPeriod (fund-membership tenure, which
    // resets on rejoin) instead of Hremployee.WorkDate (employment tenure).
    // Defaults false so companies that don't need this distinction see no
    // behavior change at all.
    public bool UseFundMembershipYearsForVesting { get; set; }

    public virtual ICollection<Pay_ProvidentFundVestingTier> VestingTiers { get; set; } = new List<Pay_ProvidentFundVestingTier>();
}
