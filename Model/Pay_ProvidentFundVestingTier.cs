using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Employer-contribution vesting schedule by years of service, scoped to one
// Pay_ProvidentFundPolicy. The employee's own contribution is always 100%
// vested regardless of tenure — only the employer match vests on this
// schedule, per each fund's own "ข้อบังคับกองทุน" (no statutory table exists,
// so this is intentionally left empty for HR to configure — see
// ProvidentFundVestingCalculator for the "no tiers configured = 100%" rule).
[Table("Pay_ProvidentFundVestingTier")]
public class Pay_ProvidentFundVestingTier
{
    [Key]
    public long Id { get; set; }

    public long PolicyId { get; set; }

    public int MinYearsOfService { get; set; }
    public int? MaxYearsOfService { get; set; } // null = no upper bound

    [Column(TypeName = "decimal(5,2)")]
    public decimal VestingPercent { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual Pay_ProvidentFundPolicy Policy { get; set; } = null!;
}
