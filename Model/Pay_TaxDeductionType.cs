using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Config-driven catalog of OPTIONAL personal-income-tax deduction categories
// (life insurance, RMF/SSF, donations, etc.) — HR adds/edits rows at
// /pay/admin/tax-deduction-config, no code change needed for a new category.
// One row per (Code, EffectiveYear): a cap changing next year means adding a
// NEW row for the new year, same "row per year" convention as
// Pay_TaxBracket, rather than mutating history. An employee elects into one
// of these via Pay_EmployeeTaxDeductionElection.
[Table("Pay_TaxDeductionType")]
public class Pay_TaxDeductionType
{
    [Key]
    public int Id { get; set; }

    [Required, StringLength(30)]
    public string Code { get; set; } = null!;

    public int EffectiveYear { get; set; }

    [Required, StringLength(200)]
    public string NameTh { get; set; } = null!;

    [Required, StringLength(200)]
    public string NameEn { get; set; } = null!;

    // Cap per employee per year for this category — an election's
    // AnnualAmount must not exceed this (validated at save time).
    [Column(TypeName = "decimal(15,2)")]
    public decimal MaxAmountPerYear { get; set; }

    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    public virtual ICollection<Pay_EmployeeTaxDeductionElection> Elections { get; set; } = new List<Pay_EmployeeTaxDeductionElection>();
}
