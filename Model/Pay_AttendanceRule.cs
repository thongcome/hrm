using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// กติกา "เวลาทำงาน → เงิน" แบบตั้งค่าได้ (CEO, 21 ก.ย. 2569 — ลูกค้า PST: สาย 1 นาทีในรอบ 26–25 เบี้ยขยันเป็น 0)
// "ให้มี config ได้ว่าตัดจากส่วนไหน โดยให้ link row ที่เป็นรายได้ และกำหนดได้ว่าสูตรหักเป็นอย่างไร หรือไม่จ่ายเงิน …
//  เผื่อ requirement ที่อื่นไม่เหมือนที่นี่"
//
// หนึ่งแถว = หนึ่งกติกา: เหตุ (สาย / ขาดงาน) → เป้าหมาย (เงินเดือน หรือรายได้ประจำตัวหนึ่ง เช่น เบี้ยขยัน) → สูตร
// บริษัทมีได้หลายกติกา ใช้เรียงตาม SortOrder · ยอดที่หักจากเป้าหมายรวมกันไม่เกินยอดของเป้าหมายนั้น
// นโยบายเดิม (Pay_AttendanceDeductionPolicy.LateMode/AbsentMode) ยังทำงานเหมือนเดิม — กติกานี้คือส่วนต่อขยาย
public enum PayAttendanceTrigger
{
    Late = 1,     // วันที่มาสายเกินเวลาผ่อนผัน
    Absent = 2,   // วันที่ขาดงาน (Att_DailyAttendance.IsAbsent)
}

public enum PayAttendanceTarget
{
    BaseSalary = 0,      // หักจากเงินเดือน (เฉพาะพนักงานรายเดือน — รายวันวันที่ขาดไม่ได้ค่าจ้างอยู่แล้ว)
    RecurringEarning = 1 // หักจาก/ไม่จ่าย รายได้ประจำที่ระบุใน WelBenefitTypeId (เบี้ยขยัน ค่าอาหาร ฯลฯ)
}

public enum PayAttendanceFormula
{
    ForfeitAll = 1,           // เกิดเหตุครบ ThresholdCount ครั้ง → ไม่จ่ายเป้าหมายทั้งก้อน
    AmountPerMinute = 2,      // นาทีที่สาย × Value บาท
    PercentPerMinute = 3,     // นาทีที่สาย × Value % ของยอดเป้าหมาย
    AmountPerOccurrence = 4,  // จำนวนครั้ง(วัน) × Value บาท
    PercentPerOccurrence = 5, // จำนวนครั้ง(วัน) × Value % ของยอดเป้าหมาย
}

[Table("Pay_AttendanceRule")]
public class Pay_AttendanceRule
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(50)]
    public string CompanyId { get; set; } = null!;

    [Required, StringLength(200)]
    public string Name { get; set; } = null!;

    public PayAttendanceTrigger Trigger { get; set; } = PayAttendanceTrigger.Late;

    public PayAttendanceTarget Target { get; set; } = PayAttendanceTarget.RecurringEarning;

    // รายได้ประจำที่ถูกหัก (Wel_BenefitType แบบจ่ายประจำรายเดือน) — ต้องมีเมื่อ Target = RecurringEarning
    public long? WelBenefitTypeId { get; set; }

    public PayAttendanceFormula Formula { get; set; } = PayAttendanceFormula.ForfeitAll;

    // บาท หรือ % ตามสูตร (ForfeitAll ไม่ใช้)
    [Column(TypeName = "decimal(15,4)")]
    public decimal Value { get; set; }

    // สาย: นาทีผ่อนผันต่อวัน (สายไม่เกินนี้ไม่นับ) · ขาดงาน: ไม่ใช้
    public int GraceMinutes { get; set; }

    // เกิดเหตุกี่ครั้ง(วัน)ขึ้นไปกติกาจึงเริ่มทำงาน — 1 = ครั้งแรกก็โดน (PST: สายครั้งเดียวเบี้ยขยัน = 0)
    public int ThresholdCount { get; set; } = 1;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    [StringLength(500)]
    public string? Note { get; set; }

    public DateTime ModifiedDate { get; set; } = DateTime.Now;
    public long? ModifiedByUserId { get; set; }

    public virtual Wel_BenefitType? WelBenefitType { get; set; }
}
