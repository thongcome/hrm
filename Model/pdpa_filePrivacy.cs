using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("pdpa_filePrivacy")]
public partial class pdpa_filePrivacy
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(50)]
    
    public string? code { get; set; }

    [StringLength(500)]
    public string? name { get; set; }

    [StringLength(500)]
    public string? name_en { get; set; }

    [StringLength(500)]
    public string? file_name { get; set; }

    [StringLength(500)]
    public string? file_path { get; set; }

    [StringLength(500)]
    public string? domain_path { get; set; }

    [StringLength(50)]
    
    public string? consent_code { get; set; }

    public bool? isActive { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? start_date { get; set; }

    [StringLength(10)]
    public string? end_date { get; set; }
}
