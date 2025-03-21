using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("asset_owner")]
public partial class asset_owner
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(50)]
    
    public string? location { get; set; }

    [StringLength(50)]
    
    public string? costCenter { get; set; }

    [StringLength(250)]
    
    public string? category { get; set; }

    [StringLength(250)]
    
    public string? assetNo { get; set; }

    [StringLength(50)]
    
    public string? comCode { get; set; }

    [StringLength(500)]
    public string? place { get; set; }

    [StringLength(50)]
    
    public string? tag { get; set; }

    [StringLength(500)]
    
    public string? description { get; set; }

    [StringLength(500)]
    public string? brand { get; set; }

    [StringLength(500)]
    public string? model { get; set; }

    [StringLength(250)]
    
    public string? serial { get; set; }

    [StringLength(50)]
    
    public string? year { get; set; }

    [StringLength(50)]
    
    public string? month { get; set; }

    [StringLength(250)]
    
    public string? inServiceDate { get; set; }

    [StringLength(250)]
    
    public string? qty { get; set; }

    [StringLength(50)]
    
    public string? currentCost { get; set; }

    [StringLength(50)]
    
    public string? AccDP { get; set; }

    [StringLength(50)]
    
    public string? nbv { get; set; }

    [StringLength(250)]
    
    public string? empName { get; set; }

    [StringLength(250)]
    
    public string? status { get; set; }

    [StringLength(50)]
    
    public string? empid { get; set; }
}
