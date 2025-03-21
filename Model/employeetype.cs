using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("employeetype")]
public partial class Employeetype
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(50)]
    
    public string code { get; set; } = null!;

    [StringLength(250)]
    
    public string name { get; set; } = null!;

    
    public string? remark { get; set; }

    public int? createby { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? createdate { get; set; }

    [StringLength(50)]
    
    public string? engname { get; set; }

    public int? ismanpower { get; set; }
}
