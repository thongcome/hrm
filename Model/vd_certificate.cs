using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("vd_certificate")]
public partial class vd_certificate
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(250)]
    
    public string name { get; set; } = null!;

    [StringLength(50)]
    
    public string? standCode { get; set; }

    [MaxLength(250)]
    public byte[]? issueBy { get; set; }

    [StringLength(50)]
    
    public string? issueYear { get; set; }

    [StringLength(50)]
    
    public string? expireYear { get; set; }

    public bool? isActive { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    [StringLength(1000)]
    public string? description { get; set; }

    [StringLength(500)]
    public string? fileRef { get; set; }

    [StringLength(500)]
    public string? filePath { get; set; }

    public long vendorid { get; set; }

    [StringLength(50)]
    
    public string? vendorcode { get; set; }
}
