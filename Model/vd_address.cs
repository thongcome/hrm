using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("vd_address")]
public partial class vd_address
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(2)]
    
    public string? address_type { get; set; }

    [StringLength(1000)]
    public string? address_no { get; set; }

    [StringLength(250)]
    
    public string? building { get; set; }

    [StringLength(1000)]
    public string? sub_district { get; set; }

    [StringLength(1000)]
    public string? district { get; set; }

    [StringLength(250)]
    
    public string? province { get; set; }

    [StringLength(250)]
    
    public string? countrycode { get; set; }

    [StringLength(10)]
    
    public string? zipcode { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    public bool? isactive { get; set; }

    public long? vendorid { get; set; }

    [StringLength(50)]
    
    public string? taxid { get; set; }

    public long? address_type_id { get; set; }

    public long? countryid { get; set; }

    public long? provinceid { get; set; }

    [StringLength(250)]
    
    public string? road { get; set; }

    [ForeignKey("address_type_id")]
    [InverseProperty("vd_addresses")]
    public virtual mas_address_type? address_typeNavigation { get; set; }

    [ForeignKey("countryid")]
    [InverseProperty("vd_addresses")]
    public virtual mas_country? country { get; set; }

    [ForeignKey("provinceid")]
    [InverseProperty("vd_addresses")]
    public virtual mas_province? provinceNavigation { get; set; }
}
