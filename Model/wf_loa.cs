using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("wf_loa")]
public partial class wf_loa
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    public long loaid { get; set; }

    public long wfid { get; set; }

    [StringLength(50)]
    
    public string? levelcode { get; set; }

    [StringLength(50)]
    
    public string? orgcode { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? startdate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? enddate { get; set; }

    public bool? isactive { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal? min { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal? max { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    public long nowWorkflowid { get; set; }

    public long nextWorkflowId { get; set; }

    public int nowLevel { get; set; }

    public int nextLevel { get; set; }

    [StringLength(1000)]
    public string? descript { get; set; }

    [StringLength(1000)]
    public string? descriptEn { get; set; }

    [StringLength(250)]
    
    public string? subject { get; set; }

    [StringLength(250)]
    
    public string? subjectEn { get; set; }
}
