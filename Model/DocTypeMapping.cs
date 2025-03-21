using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("DocTypeMapping")]
public partial class DocTypeMapping
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(250)]
    
    public string? Source { get; set; }

    public long? doctypeid { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    public bool? isActive { get; set; }

    public bool? isRequired { get; set; }

    [StringLength(250)]
    
    public string? Source2 { get; set; }

    [StringLength(50)]
    
    public string? doctypecode { get; set; }
}
