using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Advance.Payroll.Domain;

public enum PayLateDeductionMode
{
    None = 0,
    PerMinute = 1,      // minutes late (beyond grace) × amount per minute
    PerOccurrence = 2,  // each late day × fixed amount
}

public enum PayAbsentDeductionMode
{
    None = 0,
    DailyRate = 1,      // absent days × (base salary ÷ DaysPerMonthDivisor)
}

public enum PayDailyWageDaysMode
{
    CalendarDays = 0,   // DailyWage × calendar days in the period (pro-rated by join/resign)
    AttendanceDays = 1, // DailyWage × days with an attendance record that is not absent
    WorkingDays = 2,    // DailyWage × working days (company work-day mask, minus company holidays) — ค่าเริ่มต้น (audit M6)
}

// อัตราต่อวันของพนักงานรายเดือนใช้สูตรเดียวทั้งตอนคิดสัดส่วนเข้า-ออกกลางเดือนและตอนหักขาดงาน (audit M7)
public enum PayProrationMode
{
    ActualDaysInPeriod = 0, // เงินเดือน × วันที่ทำงานจริง ÷ จำนวนวันของงวด (28–31)
    DaysPerMonthDivisor = 1, // เงินเดือน ÷ DaysPerMonthDivisor (เช่น 30) × วันที่ทำงานจริง ไม่เกินเงินเดือนเต็ม — ตรงกับสูตรหักขาดงาน
}

// Config-first rules for turning attendance facts (Att_DailyAttendance) into
// payroll money — one row per company, edited at /pay/admin/attendance-deduction.
// The engine applies nothing unless a mode is switched on, so enabling
// attendance tracking never silently changes anyone's pay.
[Table("Pay_AttendanceDeductionPolicy")]
public class Pay_AttendanceDeductionPolicy
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Required, StringLength(6)]
    public string CompanyId { get; set; } = null!;

    public PayLateDeductionMode LateMode { get; set; } = PayLateDeductionMode.None;

    // Minutes of lateness per day that are forgiven before any deduction.
    public int LateGraceMinutes { get; set; }

    // PerMinute: null = derive from the employee's hourly rate
    // (salary ÷ DaysPerMonthDivisor ÷ HoursPerDay ÷ 60).
    [Column(TypeName = "decimal(10,2)")]
    public decimal? LateAmountPerMinute { get; set; }

    // PerOccurrence: fixed amount per late day.
    [Column(TypeName = "decimal(10,2)")]
    public decimal LateAmountPerOccurrence { get; set; }

    public PayAbsentDeductionMode AbsentMode { get; set; } = PayAbsentDeductionMode.None;

    // Thai practice: daily rate = monthly salary ÷ 30 (labour-law convention).
    public int DaysPerMonthDivisor { get; set; } = 30;

    [Column(TypeName = "decimal(4,1)")]
    public decimal HoursPerDay { get; set; } = 8m;

    public PayDailyWageDaysMode DailyWageMode { get; set; } = PayDailyWageDaysMode.WorkingDays;

    // สัดส่วนเงินเดือนของคนเข้า/ออกกลางงวดคิดจากอะไร — ค่าเริ่มต้นใช้ตัวหารเดียวกับการหักขาดงาน (audit M7)
    public PayProrationMode ProrationMode { get; set; } = PayProrationMode.DaysPerMonthDivisor;

    public bool IsActive { get; set; } = true;
    public DateTime ModifiedDate { get; set; } = DateTime.Now;
    public long? ModifiedByUserId { get; set; }
}
