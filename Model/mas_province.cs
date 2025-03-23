using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("mas_province")]
public partial class mas_province
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    public long countryID { get; set; }

    [StringLength(50)]
    
    public string? countryCode { get; set; }

    [StringLength(250)]
    
    public string? nameEN { get; set; }

    [StringLength(250)]
    
    public string nameTH { get; set; } = null!;

    [StringLength(50)]
    
    public string? code { get; set; }

    [StringLength(50)]
    
    public string? stateCode { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    public bool isActive { get; set; }

    [ForeignKey("countryID")]
    [InverseProperty("mas_provinces")]
    public virtual mas_country country { get; set; } = null!;

    [InverseProperty("provinceNavigation")]
    public virtual ICollection<vd_address> vd_addresses { get; set; } = new List<vd_address>();
}
