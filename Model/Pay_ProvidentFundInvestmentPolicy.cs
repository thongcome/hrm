using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// "Employee's Choice" investment policy catalog — the risk/asset-mix options
// an employee can pick from within the company's single provident fund
// (not a choice between multiple funds/AMCs). Purely informational: does
// not affect payroll calculation at all, only recorded for the employee's
// own reference/audit trail. The actual investment performance is managed
// by the fund's asset management company (บลจ.), not this system.
[Table("Pay_ProvidentFundInvestmentPolicy")]
public class Pay_ProvidentFundInvestmentPolicy
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(6)]
    public string CompanyId { get; set; } = null!;

    [Required, StringLength(20)]
    public string Code { get; set; } = null!;

    [Required, StringLength(200)]
    public string Name { get; set; } = null!;

    [StringLength(500)]
    public string? RiskDescription { get; set; }

    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
