using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("wf_org_type")]
public partial class wf_org_type
{
    [StringLength(50)]
    
    public string orgcode { get; set; } = null!;

    [StringLength(50)]
    
    public string orgcodefull { get; set; } = null!;

    [StringLength(250)]
    
    public string name { get; set; } = null!;

    public string? remark { get; set; }

    public bool istoplevel { get; set; }

    [StringLength(50)]
    
    public string? abbname { get; set; }

    [StringLength(25)]
    
    public string? lang { get; set; }

    public int? levelno { get; set; }

    [StringLength(10)]
    
    public string? codetype { get; set; }

    [StringLength(50)]
    
    public string? upperorg { get; set; }

    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }
}
