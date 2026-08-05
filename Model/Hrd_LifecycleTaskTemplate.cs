using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// HR-authored checklist item, per company + direction (Onboarding/Offboarding).
// Copied into Hrd_LifecycleTaskInstance rows when a case starts — editing a
// template afterwards never retroactively changes already-assigned tasks
// (Title is snapshotted on the instance).
[Table("Hrd_LifecycleTaskTemplate")]
public class Hrd_LifecycleTaskTemplate
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(6)]
    public string CompanyId { get; set; } = null!;

    public LifecycleTaskDirection Direction { get; set; }

    [Required, StringLength(50)]
    public string Code { get; set; } = null!;

    [Required, StringLength(250)]
    public string Title { get; set; } = null!;

    [StringLength(1000)]
    public string? Description { get; set; }

    public int SortOrder { get; set; }

    // Free text — "IT"/"HR"/"หัวหน้างาน" etc, not a real user link.
    [StringLength(100)]
    public string? DefaultAssigneeRole { get; set; }

    public bool IsActive { get; set; } = true;
}
