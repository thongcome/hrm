using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("pdpa_consent_master")]
public partial class pdpa_consent_master
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(50)]
    
    public string? code_master { get; set; }

    [StringLength(50)]
    
    public string? code { get; set; }

    [StringLength(500)]
    public string? subject { get; set; }

    [StringLength(500)]
    public string? subject_en { get; set; }

    [StringLength(500)]
    public string? name { get; set; }

    [StringLength(500)]
    public string? name_en { get; set; }

    [StringLength(500)]
    public string? file_name { get; set; }

    [StringLength(500)]
    public string? file_path { get; set; }

    [StringLength(500)]
    public string? file_name2 { get; set; }

    [StringLength(500)]
    public string? file_path2 { get; set; }

    [StringLength(500)]
    public string? domain_path { get; set; }

    [StringLength(50)]
    
    public string? consent_code { get; set; }

    public bool? isActive { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? create_date { get; set; }

    [StringLength(250)]
    
    public string? create_by { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? start_date { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? end_date { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? version_no { get; set; }

    public long? new_versionid { get; set; }

    public bool? isApprove { get; set; }

    [StringLength(250)]
    
    public string? approve_by { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? approve_date { get; set; }

    [StringLength(250)]
    
    public string? company_code { get; set; }

    [StringLength(250)]
    
    public string? company_name { get; set; }

    public bool? isPrivacy { get; set; }

    public bool? isEmail { get; set; }

    public bool? isSms { get; set; }

    public bool? isMobile { get; set; }

    public bool? isSocial { get; set; }

    public bool? isMail { get; set; }

    public bool? isOther { get; set; }

    [StringLength(50)]
    
    public string? app_code { get; set; }

    [StringLength(250)]
    
    public string? app_name { get; set; }

    [StringLength(500)]
    public string? app_url { get; set; }

    [StringLength(500)]
    public string? app_qrcode { get; set; }
}
