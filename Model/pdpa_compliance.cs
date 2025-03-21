using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("pdpa_compliance")]
public partial class pdpa_compliance
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(50)]
    
    public string? complianceCode { get; set; }

    [StringLength(50)]
    
    public string? ref1 { get; set; }

    [StringLength(50)]
    
    public string? ref2 { get; set; }

    [StringLength(50)]
    
    public string? ref3 { get; set; }

    [StringLength(50)]
    
    public string? ref4 { get; set; }

    [StringLength(4000)]
    public string? descript1 { get; set; }

    [StringLength(4000)]
    public string? descript2 { get; set; }

    [StringLength(4000)]
    public string? descript3 { get; set; }

    [StringLength(50)]
    
    public string? law_code { get; set; }

    public bool? isActive { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? startdate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? enddate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    [StringLength(500)]
    public string? subject { get; set; }
}
