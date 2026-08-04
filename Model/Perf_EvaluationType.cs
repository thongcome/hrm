using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// ประเภทแบบประเมิน (legacy: evaltype) เช่น "ประเมินผลปฏิบัติงานประจำปี",
// "ประเมินทดลองงาน", "ประเมิน 360" — แต่ละแบบมีชุด Topic/SubTopic/Indicator
// ของตัวเอง ทำให้บริษัทเดียวใช้แบบประเมินหลายชนิดพร้อมกันได้
[Table("Perf_EvaluationType")]
public class Perf_EvaluationType
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(6)]
    public string CompanyId { get; set; } = null!;

    [Required, StringLength(20)]
    public string Code { get; set; } = null!;

    [Required, StringLength(200)]
    public string Name { get; set; } = null!;
    [StringLength(200)]
    public string? NameEn { get; set; }

    public bool IsActive { get; set; } = true;
}
