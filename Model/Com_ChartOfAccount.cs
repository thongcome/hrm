using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Chart of accounts master (ผังบัญชี), covering the standard 5 accounting
// categories. Cost centers are not a separate concept here — they're just
// entries in this same master flagged IsCostCenter=true (per user decision
// 2026-08-02), so com_organization.CostCenterCode / Hremployee.CostCenterCode
// pick a Code from this table via search instead of free-typing one.
[Table("Com_ChartOfAccount")]
public class Com_ChartOfAccount
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(6)]
    public string CompanyId { get; set; } = null!;

    [Required, StringLength(20)]
    public string Code { get; set; } = null!;

    [Required, StringLength(200)]
    public string NameTh { get; set; } = null!;

    [StringLength(200)]
    public string? NameEn { get; set; }

    public AccountType AccountType { get; set; }

    public bool IsCostCenter { get; set; }

    public bool IsActive { get; set; } = true;
}
