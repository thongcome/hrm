using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("task_master")]
public partial class task_master
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(250)]
    public string name { get; set; } = null!;

    [StringLength(250)]
    public string? name_en { get; set; }

    [StringLength(1000)]
    public string? descript { get; set; }

    [StringLength(1000)]
    public string? descript_en { get; set; }

    [StringLength(250)]
    
    public string? create_by { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? create_date { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? startdate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? enddate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? pushdate { get; set; }

    [StringLength(50)]
    
    public string? org_code_create { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    public bool? isActive { get; set; }

    [StringLength(50)]
    
    public string? status { get; set; }

    [StringLength(1000)]
    public string? remark { get; set; }

    [StringLength(50)]
    
    public string? empid_create { get; set; }

    [StringLength(50)]
    
    public string? empid_project_owner { get; set; }

    [InverseProperty("task")]
    public virtual ICollection<task_assign> task_assigns { get; set; } = new List<task_assign>();
}
