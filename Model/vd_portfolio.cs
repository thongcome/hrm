using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("vd_portfolio")]
public partial class vd_portfolio
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(250)]
    
    public string name { get; set; } = null!;

    [StringLength(250)]
    
    public string? startdate { get; set; }

    [StringLength(250)]
    
    public string? enddate { get; set; }

    [StringLength(250)]
    
    public string? customerName { get; set; }

    [StringLength(500)]
    public string? description { get; set; }

    [StringLength(1000)]
    public string? fileName { get; set; }

    [StringLength(1000)]
    public string? filePath { get; set; }

    public long vendorid { get; set; }

    [StringLength(50)]
    
    public string? vendorcode { get; set; }

    public bool? isActive { get; set; }

    public bool isWithUs { get; set; }

    [StringLength(250)]
    
    public string? refcode1 { get; set; }

    [StringLength(250)]
    
    public string? refcode2 { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    [StringLength(50)]
    
    public string? year { get; set; }

    [StringLength(1000)]
    
    public string? remark { get; set; }
}
