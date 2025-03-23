using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("loa")]
public partial class Loa
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(250)]
    
    public string? Name { get; set; }

    [StringLength(50)]
    
    public string? Code { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal? min { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal? max { get; set; }

    [StringLength(50)]
    
    public string? loaTypeCode { get; set; }

    public bool? isActive { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    [StringLength(50)]
    
    public string? levelcode { get; set; }

    [StringLength(50)]
    
    public string? orgcode { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? startdate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? enddate { get; set; }
}
