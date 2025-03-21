using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("sc_program_group")]
[Index("programgroupid", Name = "sc_programgroup$programgroupid_UNIQUE", IsUnique = true)]
public partial class sc_program_group
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long programgroupid { get; set; }

    [StringLength(250)]
    public string name { get; set; } = null!;

    [StringLength(10)]
    public string? langcode { get; set; }

    [StringLength(50)]
    public string? proggroupcode { get; set; }

    [StringLength(10)]
    public string? proglevel { get; set; }

    public string? remark { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }
}
