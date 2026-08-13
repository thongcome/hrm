using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// บันทึกอัปเดตเชิงคุณภาพของ Objective เอง (แยกจากตัวเลขของ Key Result เพราะ
// Objective ไม่มีตัวเลขของตัวเอง แต่ยังต้อง trace ได้ว่าใครพูดอะไรเกี่ยวกับ
// ความคืบหน้าโดยรวมบ้าง)
[Table("Okr_ObjectiveUpdate")]
public class Okr_ObjectiveUpdate
{
    [Key]
    public long Id { get; set; }

    public long ObjectiveId { get; set; }

    public DateTime UpdateDate { get; set; } = DateTime.Now;
    public OkrObjectiveStatus StatusAtUpdate { get; set; }
    [Required, StringLength(1000)]
    public string Note { get; set; } = null!;

    public long CreatedByUserId { get; set; }

    public virtual Okr_Objective Objective { get; set; } = null!;
}
