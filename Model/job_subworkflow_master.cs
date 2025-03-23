using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("job_subworkflow_master")]
public partial class job_subworkflow_master
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long jobsubworkflowid { get; set; }

    public long jobmasterid { get; set; }

    public long? workflowid { get; set; }

    public int wlevel { get; set; }

    public bool isupperrole { get; set; }

    public bool isupperuser { get; set; }

    public bool iscondition { get; set; }

    public bool isorcondition { get; set; }

    public bool isandcondition { get; set; }

    [Column(TypeName = "decimal(6, 2)")]
    public decimal? andpercent { get; set; }

    [StringLength(50)]
    
    public string status { get; set; } = null!;

    public bool istop { get; set; }

    [Column(TypeName = "text")]
    public string? remark { get; set; }

    public bool? iscustomUser { get; set; }

    public bool? iscustomRole { get; set; }

    public int? empLevel { get; set; }

    public bool? isshow { get; set; }

    [StringLength(250)]
    
    public string? reason { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    public int? jobseq { get; set; }
}
