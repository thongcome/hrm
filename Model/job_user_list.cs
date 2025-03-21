using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("job_user_list")]
[Index("jobstatus", Name = "IX_job_user_list")]
public partial class job_user_list
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long jobapproverid { get; set; }

    public long jobmasterid { get; set; }

    [StringLength(50)]
    
    public string? reqno { get; set; }

    [StringLength(50)]
    
    public string? empid { get; set; }

    public long workflowid { get; set; }

    public int? wlevel { get; set; }

    [StringLength(50)]
    
    public string? orgcode { get; set; }

    [StringLength(50)]
    
    public string? companycode { get; set; }

    [StringLength(250)]
    
    public string? orgName { get; set; }

    [StringLength(250)]
    
    public string? remark { get; set; }

    [StringLength(250)]
    
    public string? reftable { get; set; }

    public long? subjobid { get; set; }

    [StringLength(50)]
    
    public string? emplevel { get; set; }

    public bool? isSelected { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? approvedate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? recievedate { get; set; }

    public bool? istakejob { get; set; }

    public string? comment { get; set; }

    [StringLength(250)]
    
    public string? jobstatus { get; set; }

    public long? subworkflowmasterid { get; set; }

    public bool? isOr { get; set; }

    public bool? isAnd { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? sendDate { get; set; }

    public bool? isAutoApprove { get; set; }

    [StringLength(1000)]
    public string? reason { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? expectdate { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? point { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? jobtookdate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    [StringLength(250)]
    
    public string? assignby { get; set; }

    [StringLength(50)]
    
    public string? assign_id { get; set; }

    [StringLength(250)]
    
    public string? username { get; set; }

    [StringLength(50)]
    
    public string? rolecode { get; set; }

    public long? roleid { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal? budget { get; set; }

    [StringLength(1000)]
    public string? budget_refer { get; set; }

    public long? userid { get; set; }

    public bool? isLast { get; set; }

    [StringLength(250)]
    
    public string? jobStatusName { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? sendEmailDate { get; set; }

    [StringLength(250)]
    
    public string? email { get; set; }

    public long? userIDSent { get; set; }

    public long? mas_reason_id { get; set; }

    public int? jobseq { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? andPercent { get; set; }

    public bool? isHeader { get; set; }

    [StringLength(500)]
    public string? ref1 { get; set; }

    [StringLength(500)]
    public string? ref2 { get; set; }

    [ForeignKey("jobmasterid")]
    [InverseProperty("job_user_lists")]
    public virtual job_master jobmaster { get; set; } = null!;

    [ForeignKey("mas_reason_id")]
    [InverseProperty("job_user_lists")]
    public virtual mas_reason? mas_reason { get; set; }

    [ForeignKey("subworkflowmasterid")]
    [InverseProperty("job_user_lists")]
    public virtual wf_sub_workflow_master? subworkflowmaster { get; set; }

    [ForeignKey("userid")]
    [InverseProperty("job_user_lists")]
    public virtual sc_user? user { get; set; }
}
