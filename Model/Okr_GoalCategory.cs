using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// หมวด/ธีมเชิงกลยุทธ์ — optional tag ให้ Objective ใช้กรอง/จัดกลุ่มใน dashboard
[Table("Okr_GoalCategory")]
public class Okr_GoalCategory
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(6)]
    public string CompanyId { get; set; } = null!;

    [Required, StringLength(30)]
    public string Code { get; set; } = null!;
    [Required, StringLength(200)]
    public string Name { get; set; } = null!;
    [StringLength(200)]
    public string? NameEn { get; set; }

    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
