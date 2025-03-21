using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("com_organize_layer")]
public partial class com_organize_layer
{
    [Key]  
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(50)]
    
    public string? layercode { get; set; }

    [StringLength(500)]
    public string name { get; set; } = null!;

    [StringLength(500)]
    public string? name_en { get; set; }

    public int? layer { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    public bool isActive { get; set; } = true;

    [StringLength(500)]
    public string? remark { get; set; }

    [StringLength(50)]
    
    public string? group_layer { get; set; }

    [StringLength(50)]
    
    public string? abbr { get; set; }
}
