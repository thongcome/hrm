using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("pdpa_datamart")]
public partial class pdpa_datamart
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(50)]
    
    public string? code { get; set; }

    [StringLength(2000)]
    public string? name { get; set; }

    [StringLength(2000)]
    public string? name_en { get; set; }

    public bool? isActive { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(4000)]
    public string? modby { get; set; }

    public int? sLevel { get; set; }

    [StringLength(50)]
    
    public string? sLevelCode { get; set; }

    [StringLength(500)]
    public string? uri_api { get; set; }
}
