using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("com_company")]
public partial class com_company
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(50)]
    public string code { get; set; } = null!;

    [StringLength(500)]
    public string name { get; set; } = null!;

    [StringLength(500)]
    public string? name_en { get; set; }

    [StringLength(500)]
    public string? logo_file { get; set; }

    [StringLength(500)]
    public string? logp_path { get; set; }

    [StringLength(50)]
    
    public string? tax_id { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    [StringLength(2000)]
    public string? mission { get; set; }

    [StringLength(500)]
    public string? slogan { get; set; }

    [StringLength(500)]
    public string? website { get; set; }

    [StringLength(500)]
    public string? address_HQ { get; set; }

    [StringLength(500)]
    public string? tel { get; set; }

    [StringLength(250)]
    
    public string? email { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? capital_register { get; set; }

    public int? amount_emp { get; set; }

    public bool? isActive { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? startdate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? enddate { get; set; }

    [StringLength(2000)]
    public string? remark { get; set; }

    [StringLength(50)]
    
    public string? abbr { get; set; }
}
