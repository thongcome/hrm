using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("task_activity")]
public partial class task_activity
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    public long? checkinid { get; set; }

    public long? userid { get; set; }

    public long? objectiveid { get; set; }

    public long? keyresultid { get; set; }

    [StringLength(250)]
    
    public string? subject { get; set; }

    [StringLength(250)]
    
    public string? OKRType { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? startdate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? enddate { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? qty { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? expectdate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? finisheddate { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? unit_amount { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? total { get; set; }

    [StringLength(2000)]
    public string? detail1 { get; set; }

    [StringLength(2000)]
    public string? detail2 { get; set; }

    [StringLength(2000)]
    public string? detail3 { get; set; }

    [StringLength(250)]
    
    public string? assignee_EmpID { get; set; }

    [StringLength(250)]
    
    public string? assignby_EmpID { get; set; }

    [StringLength(250)]
    
    public string? files { get; set; }

    [StringLength(250)]
    
    public string? path { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    public bool? isactive { get; set; }

    [StringLength(250)]
    
    public string? orgcode { get; set; }

    [StringLength(250)]
    
    public string? costcenter { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? total_get { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? total_expect { get; set; }

    [StringLength(250)]
    
    public string? assignee_name { get; set; }

    [StringLength(250)]
    
    public string? assigner_name { get; set; }
}
