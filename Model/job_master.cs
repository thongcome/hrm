using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("job_master")]
public partial class job_master
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long jobmasterid { get; set; }

    // กันกดพร้อมกัน (audit H5, 11 ก.ย. 2569): ทุก action ของ engine เขียนแถวนี้ (jobseq/lastLevel/status)
    // ผู้อนุมัติสองคนกดขั้นเดียวกันพร้อมกัน คนที่ commit ทีหลังได้ DbUpdateConcurrencyException
    // แทนที่จะเขียนทับผลของคนแรกจนงานค้าง — คอลัมน์เพิ่มด้วย Migrations/Manual/2026-09-11_workflow_concurrency_and_engine_default.sql
    [Timestamp]
    public byte[]? RowVersion { get; set; }

    [StringLength(1000)]
    public string? jobmastername { get; set; }

    [StringLength(1000)]
    public string? subject { get; set; }

    public long workflowid { get; set; }

    public int? maxlevel { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? createdate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? enddate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? expirydate { get; set; }

    [StringLength(50)]
    
    public string? status { get; set; }

    [StringLength(250)]
    
    public string? remark { get; set; }

    [StringLength(50)]
    
    public string? workflowcode { get; set; }

    [StringLength(50)]
    
    public string? jobrefid { get; set; }

    public bool? isactive { get; set; }

    [StringLength(250)]
    
    public string? reftable { get; set; }

    [StringLength(50)]
    
    public string? refid { get; set; }

    public int? lastLevel { get; set; }

    [StringLength(50)]
    
    public string? ReqEmplD { get; set; }

    [StringLength(50)]
    
    public string? reqno { get; set; }

    [StringLength(20)]
    
    public string? bizstatus { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? reqdate { get; set; }

    [StringLength(250)]
    
    public string? reqdept { get; set; }

    public long? reqforUserid { get; set; }

    [Column(TypeName = "decimal(20, 4)")]
    public decimal? reqamont { get; set; }

    [StringLength(1000)]
    public string? description { get; set; }

    [StringLength(250)]
    
    public string? reqName { get; set; }

    [StringLength(250)]
    
    public string? reqForName { get; set; }

    [StringLength(250)]
    
    public string? wname { get; set; }

    [StringLength(50)]
    
    public string? empid { get; set; }

    [StringLength(50)]
    
    public string? barcodeID { get; set; }

    [StringLength(50)]
    
    public string? or_and { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? percent_and { get; set; }

    [StringLength(250)]
    
    public string? createby { get; set; }

    public long? createuserid { get; set; }

    [StringLength(50)]
    
    public string? lastReqID { get; set; }

    [StringLength(250)]
    
    public string? createusername { get; set; }

    [StringLength(50)]
    
    public string? reqOrg { get; set; }

    public bool? isJobClosed { get; set; }

    [StringLength(250)]
    
    public string? reasonClosed { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? approvedDate { get; set; }

    [StringLength(250)]
    
    public string? approvedBy { get; set; }

    public long? approvedUserID { get; set; }

    public int? jobseq { get; set; }

    [StringLength(1000)]
    public string? jobmastername_en { get; set; }

    [StringLength(50)]
    
    public string? costcenter { get; set; }

    public long? loaid { get; set; }

    [StringLength(250)]

    public string? reqForNameEN { get; set; }

    // Pool Workflow claim-lock (CEO, 2026-09-07 follow-up: "ล็อกไม่ให้คนอื่น
    // ทำซ้ำจริงจัง"). A pool candidate can claim this job's current pending
    // level so teammates see it's already being worked and skip it, instead
    // of everyone reading the same case before whoever's fastest wins.
    // Enforced (not just a UI hint) in ApproveAsync/RejectAsync, gated on the
    // level snapshot's isPool==true — a non-pool job is never affected.
    // Scoped to PoolClaimedWLevel/PoolClaimedJobSeq matching the job's
    // CURRENT lastLevel/jobseq so a stale claim from an earlier level or
    // bounce-back round can never block a later one — same jobseq-matching
    // discipline as the existing staleness guards in ApproveAsync/RejectAsync.
    public long? PoolClaimedByUserId { get; set; }
    public int? PoolClaimedWLevel { get; set; }
    public int? PoolClaimedJobSeq { get; set; }
    [Column(TypeName = "datetime")]
    public DateTime? PoolClaimedDate { get; set; }

    [InverseProperty("jobmaster")]
    public virtual ICollection<job_user_list> job_user_lists { get; set; } = new List<job_user_list>();

    [InverseProperty("jobmaster")]
    public virtual ICollection<wf_adhoc_user> wf_adhoc_users { get; set; } = new List<wf_adhoc_user>();

    [ForeignKey("workflowid")]
    [InverseProperty("job_masters")]
    public virtual wf_workflow workflow { get; set; } = null!;
}
