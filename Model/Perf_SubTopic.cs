using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// หัวข้อย่อยภายใต้ Topic (legacy: SubTopic) — Weight รวมของ SubTopic ทุกตัว
// ภายใต้ Topic เดียวกันต้อง = 100%
[Table("Perf_SubTopic")]
public class Perf_SubTopic
{
    [Key]
    public long Id { get; set; }

    public long TopicId { get; set; }

    [Required, StringLength(20)]
    public string Code { get; set; } = null!;
    [Required, StringLength(300)]
    public string Name { get; set; } = null!;

    [Column(TypeName = "decimal(5,2)")]
    public decimal Weight { get; set; }

    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual Perf_Topic Topic { get; set; } = null!;
}
