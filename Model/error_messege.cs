using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("error_messege")]
public partial class error_messege
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(50)]
    
    public string messege_code { get; set; } = null!;

    [StringLength(250)]
    
    public string messege_Th { get; set; } = null!;

    [StringLength(250)]
    
    public string? message_En { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    public bool isActive { get; set; }

    [StringLength(50)]
    
    public string? lang { get; set; }
}
