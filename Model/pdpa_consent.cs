using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("pdpa_consent")]
public partial class pdpa_consent
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    public long consent_masterid { get; set; }

    [StringLength(50)]
    
    public string consent_code { get; set; } = null!;

    [StringLength(50)]
    
    public string company_code { get; set; } = null!;

    [StringLength(500)]
    public string? company_name { get; set; }

    [StringLength(500)]
    public string? channel { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? startdate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? enddate { get; set; }

    [StringLength(500)]
    public string? email { get; set; }

    [StringLength(50)]
    
    public string? mobile { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    [StringLength(500)]
    public string? social1 { get; set; }

    [StringLength(500)]
    public string? social2 { get; set; }

    [StringLength(500)]
    public string? social3 { get; set; }

    [StringLength(500)]
    public string? social4 { get; set; }

    [StringLength(50)]
    
    public string? objective_code { get; set; }

    public long? objectiveID { get; set; }

    [StringLength(50)]
    
    public string? cust_code { get; set; }

    public bool? isActive { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? getdate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? outdate { get; set; }

    [StringLength(250)]
    
    public string? app_code { get; set; }

    [StringLength(250)]
    
    public string? app_name { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? version_no { get; set; }

    public bool? isEmail { get; set; }

    public bool? isSms { get; set; }

    public bool? isMobile { get; set; }

    public bool? isSocial { get; set; }

    public bool? isMail { get; set; }

    public bool? isOther { get; set; }

    [StringLength(250)]
    
    public string? telephone { get; set; }

    [StringLength(500)]
    public string? address { get; set; }

    [StringLength(1000)]
    public string? other { get; set; }

    [StringLength(500)]
    public string? img1 { get; set; }

    [StringLength(500)]
    public string? img2 { get; set; }

    [StringLength(500)]
    public string? img3 { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? consent_date { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? unconsent_date { get; set; }

    [StringLength(500)]
    public string? unconsent_channel { get; set; }

    [StringLength(500)]
    public string? unconsent_remark { get; set; }

    [StringLength(500)]
    public string? remark { get; set; }

    [StringLength(50)]
    
    public string? unconsent_reasoncode { get; set; }

    [StringLength(1000)]
    public string? consent_detail { get; set; }

    [StringLength(500)]
    public string? consent_subject { get; set; }
}
