using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// เพดานชั่วโมง OT ต่อสัปดาห์ของบริษัท (พ.ร.บ.คุ้มครองแรงงาน — ตามความเข้าใจของผู้พัฒนา มาตรา 26 = รวมไม่เกิน 36 ชม./สัปดาห์
// ยังไม่ได้ยืนยันกับตัวบทหรือหน่วยงานทางการ) เก็บเป็นตารางตั้งค่ามีวันเริ่มมีผล ไม่ฝังตัวเลขในโค้ดและไม่ seed ค่าให้
// เหมือนค่าจ้างขั้นต่ำ: ตารางว่าง = ระบบไม่ตรวจเพดาน เมื่อกฎหมายเปลี่ยนให้เพิ่มแถวใหม่พร้อมวันที่เริ่มมีผล อย่าแก้แถวเดิม
//  · BlockOnExceed = false → เตือนอย่างเดียว (บันทึกได้), true → ไม่ให้บันทึก/ส่งคำขอที่ทำให้เกินเพดาน
// นับ OT ทุกวันประเภทรวมกันตามสัปดาห์จันทร์–อาทิตย์
[Table("Att_OtPolicy")]
public class Att_OtPolicy
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(50)]
    public string CompanyId { get; set; } = null!;

    public DateOnly EffectiveFrom { get; set; }

    [Column(TypeName = "decimal(5,1)")]
    public decimal WeeklyCapHours { get; set; }

    public bool BlockOnExceed { get; set; }

    public bool IsActive { get; set; } = true;

    [StringLength(500)]
    public string? Note { get; set; }

    public long EnteredByUserId { get; set; }
    public DateTime EnteredDate { get; set; } = DateTime.Now;
}
