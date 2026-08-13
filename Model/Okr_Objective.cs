using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// โหนดที่ cascade กันเชิงองค์กร (Company -> Organization -> Employee) —
// แยกจาก Key Result โดยตั้งใจ (มาตรฐาน OKR สากล): Objective คือทิศทาง/การ
// จัดตำแหน่งองค์กร ไม่ใช่ตัวเลขวัดผลเอง ProgressPercent คำนวณจาก
// Okr_KeyResult ของตัวเองเท่านั้น ไม่ auto roll-up จากลูก (กัน double-count) —
// ดู OkrGoalService.CalculateObjectiveProgress
[Table("Okr_Objective")]
public class Okr_Objective
{
    [Key]
    public long Id { get; set; }

    public long CycleId { get; set; }
    public long? CategoryId { get; set; }

    public OkrOwnerType OwnerType { get; set; }
    public long? OwnerOrganizationId { get; set; }
    public long? OwnerHremployeeId { get; set; }

    public long? ParentObjectiveId { get; set; }

    [Required, StringLength(300)]
    public string Title { get; set; } = null!;
    [StringLength(2000)]
    public string? Description { get; set; }

    public OkrObjectiveStatus Status { get; set; } = OkrObjectiveStatus.NotStarted;

    public long CreatedByUserId { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public virtual Okr_Cycle Cycle { get; set; } = null!;
    public virtual Okr_GoalCategory? Category { get; set; }
    public virtual Okr_Objective? ParentObjective { get; set; }
    public virtual ICollection<Okr_Objective> ChildObjectives { get; set; } = new List<Okr_Objective>();
    public virtual ICollection<Okr_KeyResult> KeyResults { get; set; } = new List<Okr_KeyResult>();
    public virtual ICollection<Okr_ObjectiveUpdate> Updates { get; set; } = new List<Okr_ObjectiveUpdate>();
}
