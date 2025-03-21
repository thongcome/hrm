using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("pc_RFQCondition")]
public partial class pc_RFQCondition
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
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
}
