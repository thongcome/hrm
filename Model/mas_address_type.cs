using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("mas_address_type")]
public partial class mas_address_type
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(250)]
    
    public string name { get; set; } = null!;

    [StringLength(250)]
    
    public string? name_en { get; set; }

    [StringLength(50)]
    
    public string code { get; set; } = null!;

    public bool isActive { get; set; }

    public byte[]? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    [InverseProperty("address_typeNavigation")]
    public virtual ICollection<vd_address> vd_addresses { get; set; } = new List<vd_address>();
}
