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

    // Cap per employee per year for this category (0 = no baht cap, e.g. a per-person item whose
    // limit is AmountPerPerson × MaxPersons). For FixedCap the election's AnnualAmount may not exceed it.
    [Column(TypeName = "decimal(15,2)")]
    public decimal MaxAmountPerYear { get; set; }

    // How the deductible amount is worked out (ม.47 ประมวลรัษฎากร) — see Services/Pay/Calculators/TaxDeductionRules.
    public TaxDeductionCalcMethod CalcMethod { get; set; } = TaxDeductionCalcMethod.FixedCap;

    // PerPerson: baht per person (spouse, child, parent, disabled dependent) and how many people count
    [Column(TypeName = "decimal(15,2)")]
    public decimal? AmountPerPerson { get; set; }
    public int? MaxPersons { get; set; }

    // PercentOfIncome: at most this % of the base (RMF/SSF 30% of assessable income, donation 10% of net income)
    [Column(TypeName = "decimal(5,2)")]
    public decimal? PercentCap { get; set; }
    public TaxDeductionPercentBase? PercentBase { get; set; }

    // Items sharing one legal cap (RETIREMENT: RMF+SSF+pension insurance+PVD 500,000;
    // LIFE_HEALTH: life + health insurance 100,000). Every row of a group carries the same GroupCapPerYear.
    [StringLength(50)]
    public string? CapGroup { get; set; }
    [Column(TypeName = "decimal(15,2)")]
    public decimal? GroupCapPerYear { get; set; }

    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    public virtual ICollection<Pay_EmployeeTaxDeductionElection> Elections { get; set; } = new List<Pay_EmployeeTaxDeductionElection>();
}

public enum TaxDeductionCalcMethod
{
    FixedCap = 0,         // amount paid, up to MaxAmountPerYear (life insurance, home-loan interest, prenatal)
    PerPerson = 1,        // AmountPerPerson × persons, persons ≤ MaxPersons (spouse, child, parent)
    PercentOfIncome = 2,  // amount paid, up to PercentCap % of the base and MaxAmountPerYear (RMF, SSF, donation)
}

public enum TaxDeductionPercentBase
{
    AssessableIncome = 0,          // เงินได้พึงประเมินทั้งปี (RMF/SSF/ประกันบำนาญ)
    NetIncomeAfterDeductions = 1,  // เงินได้หลังหักค่าใช้จ่ายและลดหย่อนอื่น (เงินบริจาค)
}
