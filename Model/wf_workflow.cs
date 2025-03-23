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

    [MaxLength(250)]
    public byte[]? remark { get; set; }

    [StringLength(250)]
    
    public string? wgroup { get; set; }

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
