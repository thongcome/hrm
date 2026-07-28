using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Clean progressive personal-income-tax bracket table.
// EffectiveYear is Gregorian (ค.ศ.), matching DateTime.Year — replaces the
// broken cumulative-bracket logic that used to read HRTaxRate/HRUcfTaxRate.
[Table("Pay_TaxBracket")]
public class Pay_TaxBracket
{
    [Key]
    public int Id { get; set; }

    public int EffectiveYear { get; set; }

    public int Step { get; set; }

    [Column(TypeName = "decimal(15,2)")]
    public decimal MinIncome { get; set; }

    // null = top/uncapped bracket
    [Column(TypeName = "decimal(15,2)")]
    public decimal? MaxIncome { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal RatePercent { get; set; }

    public bool IsActive { get; set; } = true;
}
