using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("wf_checklist")]
public partial class wf_checklist
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(250)]
    
    public string name { get; set; } = null!;

    [StringLength(50)]
    
    public string code { get; set; } = null!;

    [StringLength(50)]
    
    public string? status { get; set; }

    [StringLength(250)]
    
    public string? link_url { get; set; }

    [StringLength(250)]
    
    public string? refcode { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    public long workflowmasterid { get; set; }

    public long subworkflowmasterid { get; set; }

    public bool? isPass { get; set; }

    public int wlevel { get; set; }
}
