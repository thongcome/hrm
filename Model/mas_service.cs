using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("mas_service")]
public partial class mas_service
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(25)]
    
    public string? service_code { get; set; }

    [StringLength(250)]
    
    public string service_name { get; set; } = null!;

    public int? slevel { get; set; }

    [StringLength(25)]
    
    public string? service_parent_code { get; set; }

    [StringLength(250)]
    
    public string? service_name_en { get; set; }

    public bool isActive { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    [StringLength(250)]
    
    public string? service_code_all { get; set; }

    [Column(TypeName = "text")]
    public string? remark { get; set; }

    public long? Pid { get; set; }
}
