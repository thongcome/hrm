using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// An employee's request to claim (เบิก) a welfare benefit against their
// entitlement — phase 2 of the Wel_* module. Routed through the generic
// Workflow Approval Engine exactly like Lve_LeaveRequest: JobMasterId is null
// while it is an editable draft and gets set once StartJobAsync(reftable:
// "Wel_Claim", refid: this.Id) is called; the live status is read back from
// job_master (never a status column here), same lazy apply-on-read pattern the
// rest of the app uses.
//
// The amount an employee may claim is resolved per-person via
// WelfareEntitlementResolver (company default → position → individual override)
// and checked against the year's remaining balance by WelfareBalanceService —
// so the same benefit can be a different limit for different people.
[Table("Wel_Claim")]
public class Wel_Claim
{
    [Key]
    public long Id { get; set; }

    // Stable human-facing code (every document table gets one beyond the Id).
    [StringLength(30)]
    public string? ClaimNo { get; set; }

    [Required, StringLength(6)]
    public string CompanyId { get; set; } = null!;

    public long HremployeeId { get; set; }
    [Required, StringLength(6)]
    public string EmpNo { get; set; } = null!;

    public long BenefitTypeId { get; set; }
    public virtual Wel_BenefitType BenefitType { get; set; } = null!;

    // วันที่เกิดเหตุ/ค่าใช้จ่ายจริง (e.g. the receipt date), distinct from the
    // submission date — matters for which year's balance the claim counts against.
    public DateOnly EventDate { get; set; }

    public DateTime RequestedDate { get; set; } = DateTime.Now;

    // จำนวนเงินที่ขอเบิก (บาท).
    [Column(TypeName = "decimal(15,2)")]
    public decimal Amount { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    // หลักฐาน/ใบเสร็จ — doc_center row keyed refid=this.Id,
    // doctypecode="WELFARE_RECEIPT" (same generic attachment table leave uses).
    public long? ReceiptDocCenterId { get; set; }

    // job_master.jobmasterid of the approval job. Null until submitted — the
    // claim stays an editable/deletable draft in that state.
    public long? JobMasterId { get; set; }

    // Set once an approved claim is pushed to payroll as a Pay_AdhocPayItem
    // (reimbursement income) — later phase; mirrors
    // Lve_LeaveRequest.AdhocPayItemId's stamp-back-to-prevent-double-push.
    public long? AdhocPayItemId { get; set; }

    public virtual Hremployee Hremployee { get; set; } = null!;
}
