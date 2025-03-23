using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("sc_role")]
public partial class sc_role
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long roleid { get; set; }

    [StringLength(250)]
    
    public string? name { get; set; }

    [StringLength(100)]
    
    public string? abbr { get; set; }

    [StringLength(50)]
    
    public string rolelevel { get; set; } = null!;

    [StringLength(50)]
    
    public string? upperrole { get; set; }

    [StringLength(50)]
    
    public string? rolecode { get; set; }

    [Required]
    public bool isactive { get; set; } = true;

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    public bool isHeader { get; set; } = false;

    [StringLength(500)]
    public string? ref1 { get; set; }

    [StringLength(500)]
    public string? ref2 { get; set; }

    [InverseProperty("role")]
    public virtual ICollection<sc_program_access> sc_program_accesses { get; set; } = new List<sc_program_access>();

    [InverseProperty("role")]
    public virtual ICollection<sc_role_menu> sc_role_menus { get; set; } = new List<sc_role_menu>();

    [InverseProperty("role")]
    public virtual ICollection<sc_role_program> sc_role_programs { get; set; } = new List<sc_role_program>();

    [InverseProperty("role")]
    public virtual ICollection<sc_user_role> sc_user_roles { get; set; } = new List<sc_user_role>();
}
