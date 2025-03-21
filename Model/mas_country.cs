using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("mas_country")]
public partial class mas_country
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long countryid { get; set; }

    [StringLength(250)]
    
    public string? countryName { get; set; }

    [StringLength(50)]
    
    public string? countrycode { get; set; }

    public bool? isactive { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    [StringLength(250)]
    
    public string? NameEn { get; set; }

    [InverseProperty("country")]
    public virtual ICollection<mas_province> mas_provinces { get; set; } = new List<mas_province>();

    [InverseProperty("country")]
    public virtual ICollection<vd_address> vd_addresses { get; set; } = new List<vd_address>();
}
