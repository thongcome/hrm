using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// ประวัติความคืบหน้าของเป้า OKR แต่ละครั้งที่มีการอัปเดต (check-in)
[Table("Perf_GoalCheckIn")]
public class Perf_GoalCheckIn
{
    [Key]
    public long Id { get; set; }

    public long GoalId { get; set; }

    public DateTime CheckInDate { get; set; } = DateTime.Now;
    [Column(TypeName = "decimal(15,2)")]
    public decimal? ValueAtCheckIn { get; set; }
    [StringLength(1000)]
    public string? Note { get; set; }

    public long CreatedByUserId { get; set; }

    public virtual Perf_Goal Goal { get; set; } = null!;
}
