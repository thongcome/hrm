using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("his_doc_bak")]
public partial class his_doc_bak
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(250)]
    
    public string doc_type { get; set; } = null!;

    [StringLength(250)]
    
    public string? doc_name { get; set; }

    public DateOnly? doc_date { get; set; }

    public DateOnly? doc_expire_date { get; set; }

    [StringLength(250)]
    
    public string? filename { get; set; }

    [StringLength(1000)]
    public string? file_path { get; set; }

    public string? comment { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    [StringLength(50)]
    
    public string? status { get; set; }

    [StringLength(50)]
    
    public string? secureLevel { get; set; }

    public bool isActive { get; set; }

    [StringLength(250)]
    
    public string? table_ref { get; set; }

    [StringLength(250)]
    
    public string? code_ref { get; set; }

    public byte[]? createon { get; set; }
}
