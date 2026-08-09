using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

public enum CalibrationSessionStatus
{
    Open = 1,
    Closed = 2,
}

// A calibration round: HR/a committee opens this against one evaluation
// period (optionally scoped to one organization's subtree) to review
// everyone's scores side-by-side and adjust before individuals are submitted
// for approval one-by-one via the existing PerfApprovalService — this table
// is purely additive, no change to PerfScoringService/PerfApprovalService.
[Table("Perf_CalibrationSession")]
public class Perf_CalibrationSession
{
    [Key]
    public long Id { get; set; }

    [StringLength(6)]
    public string CompanyId { get; set; } = null!;

    public long EvaluationPeriodId { get; set; }

    [Required, StringLength(200)]
    public string Name { get; set; } = null!;

    // Soft link to com_organization.id — null = whole company. Same
    // convention as OrgEmployeeResolverHelper's caller sites.
    public long? OrganizationId { get; set; }

    public CalibrationSessionStatus Status { get; set; } = CalibrationSessionStatus.Open;

    public long CreatedByUserId { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime? ClosedDate { get; set; }

    public virtual Perf_EvaluationPeriod EvaluationPeriod { get; set; } = null!;
}
