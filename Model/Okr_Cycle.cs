using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// วงจร OKR ต่อบริษัท — แยกต่างหากจาก Perf_EvaluationPeriod โดยตั้งใจ (OKR
// มักหมุนรอบไตรมาส ส่วนรอบประเมินผลทางการมักรายปี) IsLocked=true บล็อกทั้ง
// การสร้าง Objective/KeyResult ใหม่และ check-in ใหม่ในรอบนั้น
[Table("Okr_Cycle")]
public class Okr_Cycle
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(6)]
    public string CompanyId { get; set; } = null!;

    [Required, StringLength(30)]
    public string Code { get; set; } = null!;
    [Required, StringLength(200)]
    public string Name { get; set; } = null!;

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsLocked { get; set; }

    public long CreatedByUserId { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;
}
