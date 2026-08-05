using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// One assigned checklist task for one employee's onboarding or offboarding
// case. TemplateId is null for ad-hoc tasks HR added manually (not copied
// from a template).
[Table("Hrd_LifecycleTaskInstance")]
public class Hrd_LifecycleTaskInstance
{
    [Key]
    public long Id { get; set; }

    public long HremployeeId { get; set; }

    public LifecycleTaskDirection Direction { get; set; }

    public long? TemplateId { get; set; }

    // Snapshotted from the template at assignment time — see file header.
    [Required, StringLength(250)]
    public string Title { get; set; } = null!;

    public LifecycleTaskStatus Status { get; set; } = LifecycleTaskStatus.Pending;

    public DateTime? DueDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public long? CompletedByUserId { get; set; }

    [StringLength(1000)]
    public string? Note { get; set; }

    // Only meaningful for offboarding asset-return style tasks — free text,
    // not a full IT asset registry (deliberately out of scope, see plan).
    [StringLength(250)]
    public string? AssetDescription { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public long CreatedByUserId { get; set; }
}
