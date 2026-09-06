using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Phase B: an employee HR has put "on hold" for ONE payroll run — data not
// ready, documents pending, a dispute — with a mandatory reason. Held
// employees are skipped by PayrollCalculationService for that run and are
// listed separately on the run page; they are picked up by a later run (or an
// adjustment run) once resolved. Soft-released (IsActive=false) rather than
// deleted so the hold + its reason stay on record for audit.
[Table("Pay_PayrollRunHold")]
public class Pay_PayrollRunHold
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    public long PayrollRunId { get; set; }
    [ForeignKey(nameof(PayrollRunId))]
    public Pay_PayrollRun? Pay_PayrollRun { get; set; }

    // Plain id (no nav) on purpose: Hremployee carries a global query filter,
    // and a required relationship into it only produces EF warnings. The
    // employee is resolved by join where displayed.
    public long HremployeeId { get; set; }

    [StringLength(50)]
    public string? EmpNo { get; set; }

    [Required, StringLength(500)]
    public string Reason { get; set; } = null!;

    public long HeldByUserId { get; set; }
    public DateTime HeldDate { get; set; } = DateTime.Now;

    public bool IsActive { get; set; } = true;
    public long? ReleasedByUserId { get; set; }
    public DateTime? ReleasedDate { get; set; }
}
