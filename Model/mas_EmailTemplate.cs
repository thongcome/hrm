using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("mas_EmailTemplate")]
public partial class mas_EmailTemplate
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(250)]
    
    public string? Name { get; set; }

    [StringLength(250)]
    
    public string? Name_en { get; set; }

    [StringLength(50)]
    
    public string? code { get; set; }

    public bool? isActive { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    [StringLength(250)]
    
    public string? Subject { get; set; }

    public string? body { get; set; }

    [StringLength(1000)]
    public string? prefix { get; set; }

    [StringLength(1000)]
    public string? suffix { get; set; }

    public int? noParam { get; set; }
}
