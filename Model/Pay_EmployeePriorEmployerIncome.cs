using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// HR-entered, once per (employee, tax year) — the cumulative income/deduction/
// tax-withheld figures from a PRIOR employer this same calendar year, taken
// from the withholding certificate (หนังสือรับรองการหักภาษี ณ ที่จ่าย, มาตรา
// 50 ทวิ) the employee brings in when hired mid-year. Folded into
// PayrollCalculationService.GetYtdAccumulatorsAsync so a mid-year hire's
// monthly withholding is computed against their TRUE annual income (which the
// law requires), not just what this company has paid them so far.
//
// Deliberately NOT consumed by WithholdingCertificateDataService (the 50-ทวิ
// THIS company issues) — each employer's certificate must show only the
// income/tax IT paid/withheld; the employee combines both certificates
// themselves at annual filing time. This table only feeds the withholding
// CALCULATION, never a document.
[Table("Pay_EmployeePriorEmployerIncome")]
public class Pay_EmployeePriorEmployerIncome
{
    [Key]
    public long Id { get; set; }

    public long HremployeeId { get; set; }

    public int TaxYear { get; set; }

    [StringLength(200)]
    public string? PriorEmployerName { get; set; }

    [Column(TypeName = "decimal(15,2)")]
    public decimal IncomeAmount { get; set; }

    // Optional — the prior employer's own month-by-month deduction split
    // (personal allowance/SSO/PF portions) usually isn't disclosed on the
    // certificate the employee actually has; leave at 0 if unknown.
    [Column(TypeName = "decimal(15,2)")]
    public decimal DeductionAmount { get; set; }

    [Column(TypeName = "decimal(15,2)")]
    public decimal TaxWithheldAmount { get; set; }

    public bool IsActive { get; set; } = true;

    [StringLength(500)]
    public string? Note { get; set; }

    public long EnteredByUserId { get; set; }
    public DateTime EnteredDate { get; set; } = DateTime.Now;

    public virtual Hremployee Hremployee { get; set; } = null!;
}
