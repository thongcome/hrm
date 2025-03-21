using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("sc_program")]
public partial class sc_program
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long progid { get; set; }

    [StringLength(250)]
    
    public string progname { get; set; } = null!;

    [Column(TypeName = "text")]
    public string templatename { get; set; } = null!;

    [Column(TypeName = "text")]
    public string filename { get; set; } = null!;

    [StringLength(50)]
    
    public string progcode { get; set; } = null!;

    [StringLength(50)]
    
    public string? progmastercode { get; set; }

    [Column(TypeName = "text")]
    public string? remark { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    [Required]
    public bool? isactive { get; set; }

    [InverseProperty("prog")]
    public virtual ICollection<sc_role_program> sc_role_programs { get; set; } = new List<sc_role_program>();
}
