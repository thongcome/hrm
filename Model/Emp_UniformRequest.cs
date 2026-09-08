using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// End-to-end proof-of-concept for the generic Workflow Approval Engine
// (CEO, 2026-09-08): "สร้าง workflow ที่มี flow 1.พนักงานขอ 2.หัวหน้าอนุมัติ
// 3.HR รับเรื่อง ถ้าทำได้ แสดงว่าคุณสร้าง workflow ถูก" — a deliberately small,
// single-purpose module (employee requests a uniform set) used to verify the
// engine handles a BRAND NEW module with zero special-casing.
//
// The TABLE was created through /admin/workflow-design (the drag-and-drop
// screen designer) on 8 ก.ย. 2569, not by a migration — this class is the
// hand-written counterpart that the designer deliberately did not overwrite
// (its EntityExists guard), so the column list below must track what that
// design actually produced.
//
// NOTE — no JobMasterId column here, by CEO decision the same day:
// "jobmasterid ไม่ควรมาอยู่ใน domain, jobmaster ต้อง link ด้วย id มาที่ domain".
// job_master already carries reftable + refid, so the link is owned by the
// workflow side alone and the domain table stays free of workflow columns.
// UniformRequestService resolves the job via
// job_master.reftable == "Emp_UniformRequest" && refid == Id.
// (Lve_LeaveRequest and the other older modules still hold their own
// JobMasterId column — they predate this rule and are not changed here.)
[Table("Emp_UniformRequest")]
public class Emp_UniformRequest
{
    [Key]
    public long Id { get; set; }

    [StringLength(50)]
    public string? RequestNo { get; set; }

    [Required, StringLength(50)]
    public string EmpNo { get; set; } = null!;

    public long HremployeeId { get; set; }

    [StringLength(50)]
    public string? CompanyId { get; set; }

    [Required, StringLength(200)]
    public string UniformType { get; set; } = null!; // เช่น เสื้อโปโล, ชุดฟอร์มพนักงาน

    [Required, StringLength(50)]
    public string Size { get; set; } = null!; // S/M/L/XL/...

    public int Quantity { get; set; } = 1;

    [StringLength(500)]
    public string? Reason { get; set; }

    public DateTime RequestedDate { get; set; } = DateTime.Now;

    // Written by the designer's CREATE TABLE (every generated table gets it,
    // with a getdate() default). Mapped so EF stops being surprised by it.
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public virtual Hremployee Hremployee { get; set; } = null!;
}
