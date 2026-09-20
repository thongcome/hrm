using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// HR-entered ad-hoc income/deduction request (bonus, commission, uniform
// deduction, damage compensation, etc.) targeting a specific pay period.
// Pending -> Approved (or Rejected/Cancelled) -> Consumed once a payroll
// calculation for TargetPeriod actually pulls it in as a line item.
//
// v1 scope: HR enters and approves on behalf of the employee (no employee
// self-service submission yet — that's a later ESS phase) and each row
// targets exactly one period (no installment/split-across-periods support
// yet; add an InstallmentGroupId later if needed without breaking this).
[Table("Pay_AdhocPayItem")]
public class Pay_AdhocPayItem
{
    [Key]
    public long Id { get; set; }

    // Stable human-facing code for this record (audit: every master/document table needs one beyond the surrogate Id).
    [StringLength(30)]
    public string? RequestCode { get; set; }

    public long HremployeeId { get; set; }

    public int PayItemTypeId { get; set; }

    // "YYYYMM", Gregorian — matches Pay_PayrollRun.PayrollPeriod
    [Required, StringLength(6)]
    public string TargetPeriod { get; set; } = null!;

    // จ่ายในรอบไหนของงวด (12 ก.ย. 2569 — พบจากเทสทั้งปี 2568): เดิมรอบปกติหยิบทุกรายการของงวดไปก่อน
    // โบนัสที่ HR อนุมัติไว้ก่อนคำนวณรอบปกติจึงไปโผล่ในรอบปกติแทนรอบโบนัส — Regular = รอบปกติ/ปรับปรุง, Bonus = รอบโบนัส
    public PayrollRunType TargetRunType { get; set; } = PayrollRunType.Regular;

    // งวดที่ของเดือนสำหรับบริษัทจ่าย 2 งวด (null = รอบแรกของเดือนที่คำนวณ)
    public int? TargetTermNo { get; set; }

    [Column(TypeName = "decimal(15,2)")]
    public decimal Amount { get; set; }

    // whether this amount counts toward taxable income for withholding tax
    // purposes (e.g. a reimbursement is typically not taxable, a bonus is)
    public bool IsTaxable { get; set; } = true;

    // ค่าชดเชยเลิกจ้าง (H-01): ส่วนที่ยกเว้นภาษี (ไม่ใช่เงินได้) และค่าใช้จ่ายที่หักจากส่วนเกิน — snapshot ณ วันส่งจ่าย
    // จากกติกา Pay_SeveranceTaxRule; null ในรายการอื่นทั้งหมด (ผลเท่าเดิม: IsTaxable ทั้งก้อนหรือไม่เลย)
    [Column(TypeName = "decimal(15,2)")]
    public decimal? TaxExemptAmount { get; set; }

    [Column(TypeName = "decimal(15,2)")]
    public decimal? TaxExpenseDeductionAmount { get; set; }

    // เงินได้ที่เข้าฐานภาษีจริงของรายการนี้ (หลังหักส่วนยกเว้นและค่าใช้จ่าย)
    [NotMapped]
    public decimal TaxableBasis => IsTaxable
        ? Math.Max(0m, Amount - (TaxExemptAmount ?? 0m) - (TaxExpenseDeductionAmount ?? 0m))
        : 0m;

    // วันที่สรุปยอด/วันที่เกิดรายการ (ไฟล์ค่าคอมของลูกค้ามีวันที่สรุปยอดมาด้วย ต้องเก็บไว้ตรวจย้อนหลัง)
    // ไม่ใช่วันที่จ่าย — วันที่จ่ายคือ TargetPeriod ที่เลือกบนหน้าจอ (พอร์ตจาก Advance.Payroll)
    [Column(TypeName = "date")]
    public DateTime? ItemDate { get; set; }

    // เลขที่เอกสารอ้างอิงจากต้นทาง (เลขบันทึกข้อความ / รายงานยอดขาย / ใบสั่งจ่าย) ไว้ตามรอยตอนตรวจสอบ
    [StringLength(100)]
    public string? ReferenceNo { get; set; }

    [Required, StringLength(500)]
    public string Reason { get; set; } = null!;

    [StringLength(500)]
    public string? Remark { get; set; }

    public PayAdhocItemStatus Status { get; set; } = PayAdhocItemStatus.Pending;

    public long RequestedByUserId { get; set; }
    public DateTime RequestedDate { get; set; } = DateTime.Now;

    public long? ApprovedByUserId { get; set; }
    public DateTime? ApprovedDate { get; set; }

    // set once a Pay_PayrollRun calculation actually consumes this item, so
    // it isn't pulled into a second run for the same period
    public long? ConsumedByPayrollRunId { get; set; }

    // Set once, at submission time, by PayrollSpikeDetector comparing this
    // Amount against the same employee's own history for the same
    // PayItemTypeId (see AdhocPayItemList.razor.SubmitAsync). Non-blocking —
    // HR sees a warning but the item still saves; this just flags it for a
    // closer look before approving. Null/empty AnomalyNote when not flagged
    // or when there wasn't enough history (<4 points) to say anything.
    public bool IsAmountAnomalyFlagged { get; set; }
    [StringLength(300)]
    public string? AnomalyNote { get; set; }

    public virtual Hremployee Hremployee { get; set; } = null!;
    public virtual Pay_PayItemType Pay_PayItemType { get; set; } = null!;
    public virtual Pay_PayrollRun? ConsumedByPayrollRun { get; set; }
}
