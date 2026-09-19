using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// กติกาภาษีของ "ค่าชดเชยตามกฎหมายแรงงาน" (ประมวลรัษฎากร มาตรา 42(25) และค่าใช้จ่ายของเงินได้เพราะเหตุออกจากงาน)
// เป็นตารางตั้งค่ามีวันเริ่มมีผล ไม่ฝังตัวเลขในโค้ด — เมื่อกฎหมายเปลี่ยน ให้เพิ่มแถวใหม่พร้อมวันที่ อย่าแก้แถวเดิม
//  · ยกเว้นภาษี = min(ค่าชดเชยที่ได้รับ, ExemptDays × ค่าจ้างรายวันอัตราสุดท้าย, ExemptCap)
//  · ส่วนที่เกิน หักค่าใช้จ่ายส่วนที่ 1 = ExpensePerYear × ปีที่ทำงาน (เศษปีนับเป็น 1 ปี)
//    แล้วหักส่วนที่ 2 = RemainderExpenseRate ของเงินที่เหลือ (จำกัดที่ RemainderExpenseCap ถ้ามี)
// ตัวเลขตั้งต้นที่ seed มาจาก มติ ครม. 18 มิ.ย. 2567 (PRD) และบทความ SCB ตรวจ 20 ก.ย. 2569 —
// ยังไม่ใช่การยืนยันจากราชกิจจานุเบกษา ให้ผู้รู้ภาษีตรวจแถวเหล่านี้ก่อนใช้จ่ายจริง (ดูช่องหมายเหตุของแต่ละแถว)
[Table("Pay_SeveranceTaxRule")]
public class Pay_SeveranceTaxRule
{
    [Key]
    public long Id { get; set; }

    public DateOnly EffectiveFrom { get; set; }

    public int ExemptDays { get; set; }

    [Column(TypeName = "decimal(15,2)")]
    public decimal ExemptCap { get; set; }

    [Column(TypeName = "decimal(15,2)")]
    public decimal ExpensePerYear { get; set; }

    [Column(TypeName = "decimal(6,4)")]
    public decimal RemainderExpenseRate { get; set; }

    // null = ไม่จำกัดเพดานค่าใช้จ่ายส่วนที่ 2
    [Column(TypeName = "decimal(15,2)")]
    public decimal? RemainderExpenseCap { get; set; }

    public bool IsActive { get; set; } = true;

    [StringLength(500)]
    public string? Note { get; set; }

    public long EnteredByUserId { get; set; }
    public DateTime EnteredDate { get; set; } = DateTime.Now;
}
