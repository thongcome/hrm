using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("wf_button")]
public partial class wf_button
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long wfbuttonid { get; set; }

    [StringLength(250)]
    
    public string? btname { get; set; }

    [StringLength(250)]
    
    public string? bt_type { get; set; }

    public bool isactive { get; set; }

    [StringLength(50)]
    
    public string? bcode { get; set; }

    [StringLength(250)]
    
    public string? remark { get; set; }

    public bool? isshow { get; set; }

    [StringLength(50)]
    
    public string? showwhenstatus { get; set; }

    [StringLength(2000)]
    public string? class_style { get; set; }

    public long button_masterid { get; set; }

    public bool? istop { get; set; }

    public bool? isStart { get; set; }

    [StringLength(50)]
    
    public string? status { get; set; }

    public long? workflowid { get; set; }

    public int? wlevel { get; set; }

    public long? subworkflowid { get; set; }

    public bool isAndCondition { get; set; }

    [ForeignKey("button_masterid")]
    [InverseProperty("wf_buttons")]
    public virtual wf_button_master button_master { get; set; } = null!;
}
