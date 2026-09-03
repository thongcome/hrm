using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// ผลงานเชิงตัวเลขของการประเมินหนึ่งรายการ ใช้กับวิธี RankByResult (AutoX #2;
// legacy PIS: evaltype 14 "ยอดขาย ≥ 500 ล้านบาท"). เก็บค่าที่วัดได้จริง (เช่น
// ยอดขาย) ต่อ Perf_EvaluationInstance หนึ่งตัว — ระบบเอาค่าเหล่านี้ในกลุ่มเดียวกัน
// มาจัดอันดับ → เปอร์เซ็นไทล์ → เทียบ Perf_GradeBand เป็นเกรด แทนการให้คะแนน
// ตัวชี้วัดแบบถ่วงน้ำหนัก. หนึ่ง instance มีได้หลาย metric (เช่น ยอดขาย + จำนวนดีล).
[Table("Perf_ResultMetric")]
public class Perf_ResultMetric
{
    [Key]
    public long Id { get; set; }

    // Which evaluation this result belongs to.
    public long EvaluationInstanceId { get; set; }

    // ชื่อตัววัด เช่น "ยอดขาย", "จำนวนดีลที่ปิดได้", "ยอดเก็บหนี้"
    [Required, StringLength(200)]
    public string MetricName { get; set; } = null!;

    // ค่าที่วัดได้จริง (ยิ่งมากยิ่งดีตาม HigherIsBetter).
    [Column(TypeName = "decimal(18,2)")]
    public decimal MetricValue { get; set; }

    // หน่วย เช่น "บาท", "ดีล", "%"
    [StringLength(30)]
    public string? Unit { get; set; }

    // ปกติผลงานยิ่งมากยิ่งดี — ตั้ง false สำหรับตัววัดที่ยิ่งน้อยยิ่งดี (เช่น
    // จำนวนข้อร้องเรียน) เพื่อกลับทิศการจัดอันดับ.
    public bool HigherIsBetter { get; set; } = true;

    [StringLength(500)]
    public string? Note { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual Perf_EvaluationInstance EvaluationInstance { get; set; } = null!;
}
