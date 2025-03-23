using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("wf_organize")]
public partial class wf_organize
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(50)]
    
    public string orgcode { get; set; } = null!;

    public int? orgcodefull { get; set; }

    [StringLength(15)]
    
    public string? uppercode { get; set; }

    [StringLength(250)]
    
    public string name { get; set; } = null!;

    [StringLength(50)]
    
    public string? abbname { get; set; }

    [StringLength(50)]
    
    public string? status { get; set; }

    [StringLength(250)]
    
    public string? upperorgname { get; set; }

    [StringLength(250)]
    
    public string? abbupperorgname { get; set; }

    [StringLength(250)]
    
    public string? eng_name { get; set; }

    [StringLength(30)]
    
    public string? eng_abbname { get; set; }

    [StringLength(250)]
    
    public string? eng_upperorgname { get; set; }

    [StringLength(250)]
    
    public string? eng_abbupperorgname { get; set; }

    [StringLength(20)]
    
    public string? financialOrgcode { get; set; }

    [StringLength(1)]
    
    public string? ismanpower { get; set; }

    [StringLength(1)]
    
    public string? istop { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? startdate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? enddate { get; set; }

    public string? remark { get; set; }

    [StringLength(50)]
    
    public string? bossid { get; set; }

    [StringLength(50)]
    
    public string? acting_bossid { get; set; }
}
