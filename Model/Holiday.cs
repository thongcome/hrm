using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("Holiday")]
public partial class Holiday
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(250)]
    
    public string Name { get; set; } = null!;

    public int hol_month { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    public int? hol_day { get; set; }

    [StringLength(50)]
    
    public string? year { get; set; }

    public bool? isWeekEnd { get; set; }

    public bool? isActive { get; set; }
}
