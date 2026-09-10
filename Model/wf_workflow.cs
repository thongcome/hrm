using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("wf_workflow")]
public partial class wf_workflow
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long workflowid { get; set; }

    [StringLength(250)]
    
    public string wname { get; set; } = null!;

    [StringLength(50)]
    
    public string wstatus { get; set; } = null!;

    public DateOnly? wstartdate { get; set; }

    public DateOnly? wenddate { get; set; }

    public int? wexpireday { get; set; }

    [StringLength(500)]
    public string? url { get; set; }

    [StringLength(250)]
    
    public string? c_detail { get; set; }

    [StringLength(250)]
    
    public string? c_create { get; set; }

    [StringLength(250)]
    
    public string? c_edit { get; set; }

    [StringLength(250)]
    
    public string? c_list { get; set; }

    [Required]
    public bool? isshow { get; set; }

    [Required]
    public bool? isactive { get; set; }

    // When true, StartJobAsync completes the job immediately at submit with no
    // approver assigned (auto-approve). Opt-in PER workflow (demo/low-risk
    // flows) — leaves every other workflow's normal multi-level routing
    // untouched. Owner request 2026-09-03 so demo flows don't stall waiting
    // for a human approver.
    public bool? isautoapprove { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    [StringLength(250)]
    
    public string? tablename { get; set; }

    [Column(TypeName = "decimal(10, 2)")]
    public decimal? lifetimeday { get; set; }

    [StringLength(20)]
    
    public string? code { get; set; }

    [StringLength(250)]
    
    public string? columnref { get; set; }

    [StringLength(250)]
    
    public string? tableref { get; set; }

    [StringLength(250)]
    
    public string? create_mothod { get; set; }

    [StringLength(250)]
    
    public string? edit_mothod { get; set; }

    [StringLength(250)]
    
    public string? view_mothod { get; set; }

    [StringLength(250)]
    
    public string? list_mothod { get; set; }

    [StringLength(50)]
    
    public string workflowcode { get; set; } = null!;

    [StringLength(250)]
    
    public string? icon { get; set; }

    [StringLength(250)]
    
    public string? description { get; set; }

    [StringLength(500)]
    public string? remark { get; set; }

    [StringLength(250)]
    
    public string? wgroup { get; set; }

    // เอกสารแนบของงานนี้อยู่ใน doc_center ภายใต้ doctypecode ไหน
    // (CEO, 10 ก.ย. 2569: "คุณต้องเอา doc_center module มาใน workflow ด้วย")
    // doc_center เก็บด้วย (doctypecode, refid) — refid ของงานคือ job_master.refid
    // ตั้งค่านี้แล้วหน้า workflow จะดึงไฟล์แนบของเอกสารต้นทางมาแสดงเอง
    [StringLength(50)]
    public string? doctypecode { get; set; }

    // เครื่องยนต์ตัวไหนเดินงานของ workflow นี้ (CEO, 10 ก.ย. 2569)
    //   null/false = WorkflowEngineService ตัวเดิม (ค่าเริ่มต้น ไม่กระทบของที่ใช้อยู่)
    //   true       = WorkflowService ตัวใหม่
    // ย้ายโมดูลมาใช้ตัวใหม่ = ติ๊กช่องนี้ ไม่ต้องแก้โค้ดโมดูล ไม่ต้อง deploy
    // ถอยกลับ = ปลดติ๊ก งานที่วิ่งอยู่ใช้ตารางชุดเดียวกันทั้งคู่จึงไม่หาย
    public bool? useNewEngine { get; set; }

    [StringLength(50)]
    
    public string? abbname { get; set; }

    public int? expecttime { get; set; }

    [StringLength(10)]
    
    public string? expecttimeUnit { get; set; }

    public bool? expectinBiz { get; set; }

    public string? param { get; set; }

    public bool? IsRejectToReq { get; set; }

    [StringLength(250)]
    
    public string? controller { get; set; }

    [StringLength(250)]
    
    public string? action { get; set; }

    [StringLength(250)]
    
    public string? wname_en { get; set; }

    [InverseProperty("workflow")]
    public virtual ICollection<job_master> job_masters { get; set; } = new List<job_master>();
}
