using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// มอบฉันทะการอนุมัติ — "ช่วงวันที่นี้ ถ้างานจะไปถึงคนนี้ ให้ไปหาอีกคนแทน"
//
// ข้อเสนอ BA ข้อ 3 (CEO สั่งทำทุกข้อ 10 ก.ย. 2569)
//
// เป็นช่องว่างของต้นฉบับทั้งสองระบบ ไม่ใช่แค่ของเรา — ทั้ง JSP และ epms ไม่มี
// กลไกนี้เลย แต่เป็นเรื่องที่ลูกค้าเจอในสัปดาห์แรกที่ใช้งานจริง: หัวหน้าลาพักร้อน
// สองสัปดาห์ งานของทั้งแผนกค้างอยู่ในกล่องของคนที่ไม่อยู่ ไม่มีใครทำอะไรได้
// นอกจากให้ admin ไล่ reassign ทีละใบ
//
// เจตนาในการออกแบบ:
//   - เป็น "การแทนที่ตอนหาผู้อนุมัติ" ไม่ใช่การแก้ config ของ workflow
//     workflow ทุกตัวได้ผลพร้อมกันโดยไม่ต้องแตะ wf_sub_workflow_master เลย
//   - มีผลกับงานที่ "ยังไม่ถูกส่งมา" เท่านั้น ใบงานที่ออกไปแล้วไม่ย้ายมือเอง
//     (ใบที่ออกไปแล้วใช้หน้า reassign ซึ่งมีอยู่แล้ว) เพราะการย้ายใบที่ออกไปแล้ว
//     ทำให้ประวัติเพี้ยน — ใบหนึ่งใบต้องมีเจ้าของคนเดียวตลอดอายุของมัน
//   - จำกัดเฉพาะ workflow เดียวได้ (WorkflowId) เผื่อกรณีมอบเฉพาะเรื่องการลา
//     แต่ไม่มอบเรื่องการเงิน
[Table("Wf_ApproverDelegation")]
public class Wf_ApproverDelegation
{
    [Key]
    public long Id { get; set; }

    /// <summary>ผู้มอบ — เจ้าของงานตัวจริงที่ไม่อยู่</summary>
    public long FromUserId { get; set; }

    /// <summary>ผู้รับมอบ — คนที่จะได้งานแทนในช่วงเวลานี้</summary>
    public long ToUserId { get; set; }

    /// <summary>ว่างไว้ = มอบทุกประเภทงาน</summary>
    public long? WorkflowId { get; set; }

    public DateTime StartDate { get; set; }

    /// <summary>วันสุดท้ายที่ยังมอบอยู่ (นับรวมทั้งวัน)</summary>
    public DateTime EndDate { get; set; }

    [StringLength(500)]
    public string? Reason { get; set; }

    public bool IsActive { get; set; } = true;

    public long? CreatedByUserId { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>มีผลอยู่จริง ณ วันที่ระบุหรือไม่ — ใช้ทั้งฝั่งอ่านและฝั่งแสดงผล</summary>
    public bool CoversDate(DateTime on) =>
        IsActive && StartDate.Date <= on.Date && on.Date <= EndDate.Date;
}
