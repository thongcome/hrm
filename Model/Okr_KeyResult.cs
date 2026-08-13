using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// รายการวัดผลย่อยของ Objective หนึ่งตัว (ไม่ cascade ต่อ) — Weight% ต้องรวม
// 100% กันภายใต้ Objective เดียวกัน ตรวจใน service เป็น non-blocking warning
// (มิเรอร์ pattern Perf_Topic/SubTopic/Indicator + CompetencyLibraryAdmin chip)
[Table("Okr_KeyResult")]
public class Okr_KeyResult
{
    [Key]
    public long Id { get; set; }

    public long ObjectiveId { get; set; }

    [Required, StringLength(300)]
    public string Title { get; set; } = null!;

    public OkrKeyResultMetricType MetricType { get; set; }

    [Column(TypeName = "decimal(15,2)")]
    public decimal? StartValue { get; set; }
    [Column(TypeName = "decimal(15,2)")]
    public decimal TargetValue { get; set; }
    [Column(TypeName = "decimal(15,2)")]
    public decimal CurrentValue { get; set; }

    [StringLength(50)]
    public string? Unit { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal Weight { get; set; }

    public long CreatedByUserId { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public virtual Okr_Objective Objective { get; set; } = null!;
    public virtual ICollection<Okr_KeyResultCheckIn> CheckIns { get; set; } = new List<Okr_KeyResultCheckIn>();
}
