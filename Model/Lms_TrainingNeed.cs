using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// A requested training topic — either for one employee (HremployeeId set)
// or for a whole organization unit (OrganizationId set). Not gated by the
// Workflow Engine (this is a request/backlog item, not a document that
// needs formal sign-off).
[Table("Lms_TrainingNeed")]
public class Lms_TrainingNeed
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(6)]
    public string CompanyId { get; set; } = null!;

    public long? HremployeeId { get; set; }

    public long? OrganizationId { get; set; }

    [Required, StringLength(250)]
    public string RequestedTopic { get; set; } = null!;

    public long? SourceCompetencyGapId { get; set; }

    public TrainingNeedStatus Status { get; set; } = TrainingNeedStatus.Requested;

    public long RequestedByUserId { get; set; }

    public DateTime RequestedDate { get; set; } = DateTime.Now;

    [StringLength(1000)]
    public string? Note { get; set; }
}
