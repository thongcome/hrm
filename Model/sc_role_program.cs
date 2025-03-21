using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("sc_role_program")]
public partial class sc_role_program
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long roleprogid { get; set; }

    public long? roleid { get; set; }

    public long? progid { get; set; }

    public bool isactive { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    [ForeignKey("progid")]
    [InverseProperty("sc_role_programs")]
    public virtual sc_program? prog { get; set; }

    [ForeignKey("roleid")]
    [InverseProperty("sc_role_programs")]
    public virtual sc_role? role { get; set; }
}
