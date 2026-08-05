using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// HR-configured list of expense categories employees pick from when adding
// a claim line item (e.g. "ค่าเดินทาง", "ค่าที่พัก", "ค่าอาหาร"). Deliberately
// a flat master table, not an enum — HR must be able to add/rename/retire
// categories without a code deploy, matching Pay_PayItemType's convention.
[Table("Exp_ExpenseCategory")]
public class Exp_ExpenseCategory
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(6)]
    public string CompanyId { get; set; } = null!;

    [Required, StringLength(20)]
    public string Code { get; set; } = null!;

    [Required, StringLength(200)]
    public string Name { get; set; } = null!;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
