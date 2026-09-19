using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// ค่าจ้างขั้นต่ำรายวัน (พ.ร.บ.คุ้มครองแรงงาน — คณะกรรมการค่าจ้างประกาศเป็นรายจังหวัด เปลี่ยนตามประกาศ)
// เก็บเป็นตารางตั้งค่าที่มีวันเริ่มมีผล ไม่ฝังตัวเลขในโค้ด — เหมือนอัตราประกันสังคม (H-03):
// เมื่อประกาศใหม่ ให้เพิ่มแถวใหม่พร้อมวันที่เริ่มมีผล อย่าแก้แถวเดิม
//  · ProvinceName ว่าง = ใช้กับทุกจังหวัด (ค่าตั้งต้น) — แถวของจังหวัดที่ตรงกับ Pay_PayslipSettings.WorkProvince ชนะเสมอ
//  · ตารางว่าง = ระบบไม่ตรวจค่าจ้างขั้นต่ำ (ไม่ได้แปลว่าไม่มีกฎหมาย — ต้องมีคนตั้งค่า)
// ตัวเลขจริงต้องมาจากประกาศทางการ ระบบไม่แถมค่าให้
[Table("Pay_MinimumWage")]
public class Pay_MinimumWage
{
    [Key]
    public long Id { get; set; }

    [StringLength(100)]
    public string? ProvinceName { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal DailyAmount { get; set; }

    public DateOnly EffectiveFrom { get; set; }

    public bool IsActive { get; set; } = true;

    [StringLength(500)]
    public string? Note { get; set; }

    public long EnteredByUserId { get; set; }
    public DateTime EnteredDate { get; set; } = DateTime.Now;
}
