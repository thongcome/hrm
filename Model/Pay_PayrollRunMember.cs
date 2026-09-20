using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Who a leaver final-pay run (PayrollRunType.FinalPay) is for — that run calculates ONLY these
// employees, and the regular run of the same period skips them (they were paid here). Regular
// and bonus runs never have members: their population comes from PayrollEligibility.
// Added/removed by FinalPayService while the run is still Draft/Calculated.
[Table("Pay_PayrollRunMember")]
public class Pay_PayrollRunMember
{
    [Key]
    public long Id { get; set; }

    public long PayrollRunId { get; set; }

    public long HremployeeId { get; set; }

    [StringLength(50)]
    public string EmpNo { get; set; } = "";

    public long AddedByUserId { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.Now;
}
