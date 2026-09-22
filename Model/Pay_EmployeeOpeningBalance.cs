using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// ยอดยกมา — one row per employee per month that THIS company already paid from its previous
// payroll system, for a customer that goes live mid-year (ported from Advance.Payroll,
// CEO 18–19 ก.ย. 2569). Comes in from the "ยอดยกมา" sheet of the employee import; re-importing
// the same (employee, year, month) REPLACES the row, never adds to it.
//
// Unlike Pay_EmployeePriorEmployerIncome (a DIFFERENT employer's certificate, which only feeds
// the withholding calculation), these months are this employer's own pay, so they feed:
//   - the YTD withholding projection (PayrollCalculationService.LoadYtdRowsAsync)
//   - the 50 ทวิ this company issues (WithholdingCertificateDataService)
//   - the annual ภ.ง.ด.1ก (Por1DataService.BuildAnnualAsync)
// but NOT the monthly ภ.ง.ด.1 / สปส.1-10 — those months were already filed from the old system.
// A month may not be both here and in a paid run of this system (import + pre-flight refuse it).
[Table("Pay_EmployeeOpeningBalance")]
public class Pay_EmployeeOpeningBalance
{
    [Key]
    public long Id { get; set; }

    [StringLength(50)]
    public string CompanyId { get; set; } = "";

    public long HremployeeId { get; set; }

    [StringLength(50)]
    public string EmpNo { get; set; } = "";

    // Christian-era year (the sheet takes พ.ศ. and converts)
    public int TaxYear { get; set; }

    public int Month { get; set; }

    [Column(TypeName = "decimal(15,2)")] public decimal GrossIncome { get; set; }
    [Column(TypeName = "decimal(15,2)")] public decimal TaxableIncome { get; set; }
    [Column(TypeName = "decimal(15,2)")] public decimal TaxWithheld { get; set; }
    [Column(TypeName = "decimal(15,2)")] public decimal SsoEmployee { get; set; }
    [Column(TypeName = "decimal(15,2)")] public decimal SsoEmployer { get; set; }
    [Column(TypeName = "decimal(15,2)")] public decimal PvdEmployee { get; set; }
    [Column(TypeName = "decimal(15,2)")] public decimal PvdEmployer { get; set; }
    [Column(TypeName = "decimal(15,2)")] public decimal? NetPay { get; set; }

    public long? ImportBatchId { get; set; }

    public bool IsActive { get; set; } = true;

    public long UpdatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    [NotMapped] public DateOnly PeriodStart => new(TaxYear, Month, 1);
}
