using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("sc_menu")]
public partial class sc_menu
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long menuid { get; set; }

    [StringLength(250)]
    public string menuname { get; set; } = null!;

    public int menulevel { get; set; }

    public bool isfinal { get; set; }= false;

    public int? menuorder { get; set; }

    [StringLength(20)]
    public string? menucode { get; set; }

    [StringLength(20)]
    public string? langcode { get; set; }

    [Required]
    public bool isshow { get; set; } = true;

    [StringLength(500)]
    public string? tooltip { get; set; }

    public DateOnly? startdate { get; set; }

    public DateOnly? enddate { get; set; }

    public long? programid { get; set; }

    [StringLength(250)]
    
    public string? icon { get; set; }

    [StringLength(250)]
    
    public string? small_icon { get; set; }

    [Column(TypeName = "text")]
    public string? url { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    [StringLength(50)]
    
    public string? uppermenucode { get; set; }

    public long menugroupid { get; set; }

    [Required]
    public bool isactive { get; set; } = true;

    [StringLength(250)]
    
    public string? method_action { get; set; }

    [StringLength(250)]
    
    public string? menuname_en { get; set; }

    [ForeignKey("menugroupid")]
    [InverseProperty("sc_menus")]
    public virtual sc_menugroup? menugroup { get; set; } = null!;

    [InverseProperty("menu")]
    public virtual ICollection<sc_program_access> sc_program_accesses { get; set; } = new List<sc_program_access>();

    [InverseProperty("menu")]
    public virtual ICollection<sc_role_menu> sc_role_menus { get; set; } = new List<sc_role_menu>();
}
