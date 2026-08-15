using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// One detected anomaly for one payroll run — written by
// PayrollAnomalyDetectionService right after every calculation. Purely
// advisory: never read by PayrollCalculationService or the approval
// workflow, only surfaced to HR as a non-blocking warning (same spirit as
// Pay_EmployeeInsuranceEnrollment.NeedsReview). PayrollEmployeeId is null
// for run-level anomalies (PeriodTotalAbnormal); every other type is
// per-employee.
[Table("Pay_PayrollAnomaly")]
public class Pay_PayrollAnomaly
{
    [Key]
    public long Id { get; set; }

    public long PayrollRunId { get; set; }
    public long? PayrollEmployeeId { get; set; }

    public PayrollAnomalyType AnomalyType { get; set; }
    public PayrollAnomalySeverity Severity { get; set; }

    [Required, StringLength(1000)]
    public string Description { get; set; } = null!;

    [Column(TypeName = "decimal(15,2)")]
    public decimal? DetectedValue { get; set; }
    [Column(TypeName = "decimal(15,2)")]
    public decimal? ReferenceValue { get; set; }

    public DateTime DetectedDate { get; set; } = DateTime.Now;

    public bool IsAcknowledged { get; set; }
    public long? AcknowledgedByUserId { get; set; }
    public DateTime? AcknowledgedDate { get; set; }

    public virtual Pay_PayrollRun Pay_PayrollRun { get; set; } = null!;
    public virtual Pay_PayrollEmployee? Pay_PayrollEmployee { get; set; }
}
