using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("com_position")]
public partial class com_position
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(50)]
    
    public string? code { get; set; }

    [StringLength(250)]
    public string? name { get; set; }

    [StringLength(250)]
    public string? name_en { get; set; }

    [StringLength(50)]
    
    public string? comp_code_all { get; set; }

    [StringLength(50)]
    
    public string? pos_type { get; set; }

    public bool isActive { get; set; } = true;

    [Column(TypeName = "datetime")]
    public DateTime? startdate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? enddate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? min_salary { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? max_salary { get; set; }

    [StringLength(2000)]
    public string? remark { get; set; }

    [StringLength(50)]
    
    public string? costcentercode { get; set; }
}
