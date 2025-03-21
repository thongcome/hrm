using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("vd_contact")]
public partial class vd_contact
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(250)]
    
    public string name { get; set; } = null!;

    [StringLength(250)]
    
    public string? email { get; set; }

    [StringLength(250)]
    
    public string mobile { get; set; } = null!;

    [StringLength(250)]
    
    public string? phone { get; set; }

    [StringLength(250)]
    
    public string? line { get; set; }

    [StringLength(250)]
    
    public string? social { get; set; }

    public string? remark { get; set; }

    public bool? isactive { get; set; }

    public bool? ismainContact { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    public long? vendorid { get; set; }

    [StringLength(50)]
    
    public string? taxid { get; set; }

    [StringLength(250)]
    
    public string? ext { get; set; }

    [StringLength(250)]
    
    public string? position { get; set; }

    public long? titleid { get; set; }
}
