using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// ประวัติอัปเดตตัวเลขของ Key Result (append-only) — บันทึกแล้วเขียนทับ
// KeyResult.CurrentValue ด้วยค่าล่าสุด (เหมือน v1's Perf_GoalCheckIn)
[Table("Okr_KeyResultCheckIn")]
public class Okr_KeyResultCheckIn
{
    [Key]
    public long Id { get; set; }

    public long KeyResultId { get; set; }

    public DateTime CheckInDate { get; set; } = DateTime.Now;
    [Column(TypeName = "decimal(15,2)")]
    public decimal ValueAtCheckIn { get; set; }
    public OkrConfidenceLevel? Confidence { get; set; }
    [StringLength(1000)]
    public string? Note { get; set; }

    public long CreatedByUserId { get; set; }

    public virtual Okr_KeyResult KeyResult { get; set; } = null!;
}
