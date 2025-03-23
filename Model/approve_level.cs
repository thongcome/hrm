using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("approve_level")]
public partial class approve_level
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long approveid { get; set; }

    public int approverLevel { get; set; }

    [StringLength(50)]
    
    public string? levelcode { get; set; }

    [StringLength(250)]
    
    public string? Name { get; set; }

    public bool isactive { get; set; }

    [StringLength(50)]
    
    public string? comcode { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    [StringLength(250)]
    
    public string? cost_center { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal? min_budget { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal? max_budget { get; set; }

    [StringLength(50)]
    
    public string? role_code { get; set; }

    public long? role_id { get; set; }

    [StringLength(250)]
    
    public string? engname { get; set; }
}
