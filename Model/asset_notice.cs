using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("asset_notice")]
public partial class asset_notice
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    // Stable human-facing code for this record (audit: every master/document table needs one beyond the surrogate Id).
    [StringLength(20)]

    public string? noticeNo { get; set; }

    [StringLength(50)]

    public string? serial { get; set; }

    [StringLength(250)]
    
    public string? assetNo { get; set; }

    [StringLength(250)]
    
    public string? comCode { get; set; }

    [StringLength(250)]
    
    public string? tag { get; set; }

    [StringLength(250)]
    
    public string? qty { get; set; }

    [StringLength(250)]
    
    public string? empName { get; set; }

    [StringLength(250)]
    
    public string? status { get; set; }

    [StringLength(250)]
    
    public string? empid { get; set; }

    [StringLength(250)]
    
    public string? orgcode { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? getdate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? outdate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    public bool? isOwnbyOrg { get; set; }

    public long? assetid { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? createdate { get; set; }

    [StringLength(250)]
    
    public string? createby { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    public long? moduserid { get; set; }
}
