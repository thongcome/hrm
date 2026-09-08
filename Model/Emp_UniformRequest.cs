using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// End-to-end proof-of-concept for the generic Workflow Approval Engine
// (CEO, 2026-09-08): "สร้าง workflow ที่มี flow 1.พนักงานขอ 2.หัวหน้าอนุมัติ
// 3.HR รับเรื่อง ถ้าทำได้ แสดงว่าคุณสร้าง workflow ถูก" — a deliberately small,
// single-purpose module (employee requests a uniform set) used to verify the
// engine handles a BRAND NEW module with zero special-casing: vertical
// (org-chart) resolution to the requester's own supervisor, then a
// role-based HR step, plus the WorkflowDocumentSlot pluggable-component
// mechanism. Mirrors Lve_LeaveRequest's shape exactly (soft-link
// HremployeeId/EmpNo/CompanyId, nullable JobMasterId set once submitted).
[Table("Emp_UniformRequest")]
public class Emp_UniformRequest
{
    [Key]
    public long Id { get; set; }

    [StringLength(30)]
    public string? RequestNo { get; set; }

    public long HremployeeId { get; set; }
    [Required, StringLength(6)]
    public string EmpNo { get; set; } = null!;
    [StringLength(6)]
    public string? CompanyId { get; set; }

    [Required, StringLength(100)]
    public string UniformType { get; set; } = null!; // เช่น เสื้อโปโล, ชุดฟอร์มพนักงาน

    [Required, StringLength(20)]
    public string Size { get; set; } = null!; // S/M/L/XL/...

    public int Quantity { get; set; } = 1;

    [StringLength(500)]
    public string? Reason { get; set; }

    public DateTime RequestedDate { get; set; } = DateTime.Now;

    // job_master.jobmasterid of the approval job — null while still an
    // editable draft, same convention as Lve_LeaveRequest.JobMasterId.
    public long? JobMasterId { get; set; }

    public virtual Hremployee Hremployee { get; set; } = null!;
}
