using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Standard/mandatory personal-income-tax deduction parameters, one row per
// tax year — same global, EffectiveYear-scoped convention as Pay_TaxBracket
// (no CompanyId; these figures are set by Thai revenue law, not company
// policy). Consumed by PayrollCalculationService.cs for EVERY employee
// automatically, no election needed — unlike Pay_TaxDeductionType below,
// which is for the OPTIONAL categories an employee elects into.
[Table("Pay_TaxDeductionSetting")]
public class Pay_TaxDeductionSetting
{
    [Key]
    public int Id { get; set; }

    // Stable human-facing code for this record (audit: every master/document table needs one beyond the surrogate Id).
    [StringLength(30)]
    public string? Code { get; set; }

    public int EffectiveYear { get; set; }

    // ค่าลดหย่อนส่วนตัว — flat per year, applied 1/12 per payroll period
    // regardless of that period's income.
    [Column(TypeName = "decimal(15,2)")]
    public decimal PersonalAllowancePerYear { get; set; } = 60000m;

    // ค่าใช้จ่าย — deducted at this rate against PROJECTED ANNUAL income
    // (not a flat per-period amount), capped at ExpenseDeductionCap. See
    // TaxBracketCalculator.CalculateMonthlyWithholding, which computes this
    // internally from the annualized income rather than accepting it as an
    // external flat monthly figure.
    [Column(TypeName = "decimal(5,4)")]
    public decimal ExpenseDeductionRate { get; set; } = 0.50m;

    [Column(TypeName = "decimal(15,2)")]
    public decimal ExpenseDeductionCap { get; set; } = 100000m;

    public bool IsActive { get; set; } = true;
}
