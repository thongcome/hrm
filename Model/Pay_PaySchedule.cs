using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// รอบจ่ายเงินเดือน (CEO, 11 ก.ย. 2569 — audit M8): จ่ายกี่งวดต่อเดือน เป็น config ต่อบริษัท ไม่ใช่สูตรตายตัว
// "13 − เดือน" ในตัวคำนวณ — ใช้กับพนักงานกลุ่มไหน (รายเดือน / รายวัน / ทุกคน) และมีผลตั้งแต่-ถึงวันที่
// เปลี่ยนกลางปี = เพิ่มแถวใหม่ที่มีผลวันที่ใหม่ แถวเดิมใส่วันสิ้นสุด ตัวคำนวณอ่านปฏิทินจากตารางนี้
// ทั้งงวดที่ผ่านมาและงวดที่เหลือของปี จึงประมาณการภาษีทั้งปีได้ถูกแม้ความถี่เปลี่ยน
// ค่าเริ่มต้นเมื่อไม่มีแถว = เดือนละ 1 งวด (พฤติกรรมเดิม) · ทับรายคนได้ที่ Pay_EmployeePayScheduleOverride
[Table("Pay_PaySchedule")]
public class Pay_PaySchedule
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(50)]
    public string CompanyId { get; set; } = null!;

    [Required, StringLength(50)]
    public string Code { get; set; } = null!;

    [Required, StringLength(100)]
    public string Name { get; set; } = null!;

    // 1 = เดือนละงวด, 2 = เดือนละ 2 งวด (ครึ่งเดือน)
    public int PeriodsPerMonth { get; set; } = 1;

    // งวดที่ 2 ของเดือนเริ่มวันที่เท่าไร (ใช้เมื่อ PeriodsPerMonth = 2) — งวดที่ PeriodStart.Day >= ค่านี้ คือ "งวดที่ 2"
    public int SecondTermStartDay { get; set; } = 16;

    public PayScheduleGroup AppliesTo { get; set; } = PayScheduleGroup.All;

    public DateOnly EffectiveFrom { get; set; }

    public DateOnly? EffectiveTo { get; set; }

    public bool IsActive { get; set; } = true;

    [StringLength(500)]
    public string? Note { get; set; }
}
