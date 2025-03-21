using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("mas_status")]
public partial class mas_status
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(250)]
    
    public string? name { get; set; }

    [StringLength(50)]
    
    public string? code { get; set; }

    public bool isActive { get; set; }= true;

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(50)]
    
    public string? modby { get; set; }

    [StringLength(250)]
    
    public string? group_ref { get; set; }
}
