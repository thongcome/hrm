using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("sc_menugroup")]
[Index("menugroupid", Name = "sc_menugroup$programgroupid_UNIQUE", IsUnique = true)]
public partial class sc_menugroup
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long menugroupid { get; set; }

    [StringLength(250)]
    public string menugroupname { get; set; } = null!;

    [StringLength(10)]
    public string? langcode { get; set; }

    [StringLength(50)]
    public string? menucode { get; set; }

    [StringLength(50)]
    public string? menugrouplevel { get; set; }

    public string? remark { get; set; }

    [Required]
    public bool? isactive { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    [InverseProperty("menugroup")]
    public virtual ICollection<sc_menu> sc_menus { get; set; } = new List<sc_menu>();
}
