using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Keyless]
[Table("pc_vd_RFQCondition")]
public partial class pc_vd_RFQCondition
{
    public long id { get; set; }

    [StringLength(50)]
    
    public string? code { get; set; }

    [StringLength(500)]
    public string? name { get; set; }

    [StringLength(500)]
    public string? nameEn { get; set; }

    public bool? ismandatory { get; set; }

    public bool? isactive { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(500)]
    
    public string? modby { get; set; }

    public int? orderTh { get; set; }

    public long? vendorid { get; set; }

    public long? rfqid { get; set; }

    [StringLength(250)]
    
    public string? quotationNo { get; set; }

    [StringLength(250)]
    
    public string? VendorCode { get; set; }

    [StringLength(250)]
    
    public string? text1 { get; set; }

    [StringLength(250)]
    
    public string? text2 { get; set; }

    [StringLength(250)]
    
    public string? remark { get; set; }
}
