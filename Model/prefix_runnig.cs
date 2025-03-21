using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("prefix_runnig")]
public partial class prefix_runnig
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long gencodeid { get; set; }

    [StringLength(50)]
    
    public string code { get; set; } = null!;

    [StringLength(250)]
    
    public string lastrun { get; set; } = null!;

    [StringLength(250)]
    
    public string? runing { get; set; }

    [StringLength(50)]
    
    public string? year { get; set; }

    [StringLength(250)]
    
    public string? lastcode { get; set; }

    public bool isActive { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }
}
