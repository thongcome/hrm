using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("sc_menu_program")]
public partial class sc_menu_program
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    public long menuid { get; set; }

    [StringLength(50)]
    
    public string? menucode { get; set; }

    public long programid { get; set; }

    [MaxLength(50)]
    public byte[]? programcode { get; set; }

    public bool isActive { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }
}
