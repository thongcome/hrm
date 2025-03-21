using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("pr_service_type")]
public partial class pr_service_type
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(250)]
    
    public string name { get; set; } = null!;

    [StringLength(250)]
    
    public string? name_en { get; set; }

    [StringLength(50)]
    
    public string? code { get; set; }

    public bool isActive { get; set; }

    public byte[]? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    public bool isTE { get; set; }

    public string? remark { get; set; }

    [InverseProperty("pr_service_type")]
    public virtual ICollection<pc_pr> pc_prs { get; set; } = new List<pc_pr>();
}
