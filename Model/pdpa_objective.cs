using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("pdpa_objective")]
public partial class pdpa_objective
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(250)]
    
    public string? name { get; set; }

    [StringLength(250)]
    
    public string? name_en { get; set; }

    [StringLength(50)]
    
    public string? consent_master_code { get; set; }

    public long? consent_masterid { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? version_no_master { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? createdate { get; set; }

    [StringLength(250)]
    
    public string? createby { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    public bool? isActive { get; set; }

    public bool? isApprove { get; set; }

    [StringLength(250)]
    
    public string? approveby { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? approvedate { get; set; }

    [StringLength(500)]
    public string? remark { get; set; }

    [StringLength(500)]
    public string? description { get; set; }
}
