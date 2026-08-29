using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Sibling of Hr_DisciplinaryCase — mirrors its shape exactly (JobMasterId
// null = still-editable draft; once submitted, current status is derived
// live by joining job_masters.status via JobMasterId, see
// RewardCaseService.GetJobStatusesAsync).
public class Hr_RewardCase
{
    public long Id { get; set; }

    // Stable human-facing case number for this record (audit: every master/document table needs one beyond the surrogate Id).
    [StringLength(30)]
    public string? CaseNo { get; set; }

    public long HremployeeId { get; set; }
    [StringLength(6)]
    public string? EmpNo { get; set; }
    [StringLength(6)]
    public string CompanyId { get; set; } = null!;

    public RewardType RewardType { get; set; }
    public DateOnly AwardDate { get; set; }
    [Column(TypeName = "nvarchar(max)")]
    public string Description { get; set; } = null!;
    // Only meaningful when RewardType = CashBonus.
    [Column(TypeName = "decimal(15,2)")]
    public decimal? Amount { get; set; }

    public long? JobMasterId { get; set; }

    public long CreatedByUserId { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public virtual Hremployee Hremployee { get; set; } = null!;
}
