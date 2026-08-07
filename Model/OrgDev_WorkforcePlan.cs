using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Target headcount for one org unit for one calendar year. Actual headcount
// is never stored here — WorkforcePlanService computes it live from
// Pos_PositionSlot (active + HremployeeId not null) so the gap is always
// current, not a stale snapshot.
[Table("OrgDev_WorkforcePlan")]
public class OrgDev_WorkforcePlan
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(6)]
    public string CompanyId { get; set; } = null!;

    public long OrganizationId { get; set; }

    public int PlanYear { get; set; }

    public int TargetHeadcount { get; set; }

    [StringLength(1000)]
    public string? Note { get; set; }

    public long CreatedByUserId { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;
}
