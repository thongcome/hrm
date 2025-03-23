using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("CostCenterProcurement")]
public partial class CostCenterProcurement
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(50)]
    
    public string costcenter { get; set; } = null!;

    public long userid { get; set; }

    [StringLength(50)]
    
    public string? empid { get; set; }

    public bool? isactive { get; set; }

    [StringLength(250)]
    
    public string? code { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    [StringLength(250)]
    
    public string? name { get; set; }

    [StringLength(250)]
    
    public string? costCenterAll { get; set; }

    [StringLength(250)]
    
    public string? upperCostCenter { get; set; }
}
