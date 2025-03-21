using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("task_assign")]
public partial class task_assign
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(50)]
    
    public string? assign_empid { get; set; }

    [StringLength(250)]
    
    public string? assign_name { get; set; }

    [StringLength(50)]
    
    public string? assignee_empid { get; set; }

    [StringLength(250)]
    
    public string? assignee_name { get; set; }

    [StringLength(50)]
    
    public string? orgcode { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? startdate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? enddate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? assign_time { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    [StringLength(500)]
    public string? place { get; set; }

    [StringLength(500)]
    public string? description { get; set; }

    [StringLength(250)]
    
    public string? approve_by { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? approve_date { get; set; }

    [StringLength(250)]
    
    public string? lat { get; set; }

    [StringLength(250)]
    
    public string? lon { get; set; }

    [StringLength(500)]
    public string? description2 { get; set; }

    [StringLength(500)]
    public string? description3 { get; set; }

    public long? taskid { get; set; }

    [ForeignKey("taskid")]
    [InverseProperty("task_assigns")]
    public virtual task_master? task { get; set; }
}
