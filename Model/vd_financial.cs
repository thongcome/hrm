using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("vd_financial")]
public partial class vd_financial
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    public long vendorid { get; set; }

    [StringLength(50)]
    
    public string? vendorCode { get; set; }

    [StringLength(50)]
    
    public string? year { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? income { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? profit { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? loss { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? workingCapitalRatio { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    [StringLength(250)]
    
    public string? filename { get; set; }

    [StringLength(250)]
    
    public string? filepath { get; set; }
}
