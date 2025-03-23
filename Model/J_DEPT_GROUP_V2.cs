using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("J_DEPT_GROUP_V2")]
public partial class J_DEPT_GROUP_V2
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(50)]
    
    public string? DEPT_KEY { get; set; }

    [StringLength(250)]
    
    public string? DEPT_CODE { get; set; }

    [StringLength(250)]
    
    public string? DEPT_THAIDESC { get; set; }

    [StringLength(250)]
    
    public string? DEPT_ENGDESC { get; set; }

    [StringLength(250)]
    
    public string? DEPT_LEVEL { get; set; }

    [StringLength(250)]
    
    public string? DEPT_PARENT { get; set; }

    [StringLength(50)]
    
    public string? DEPT_PARENT_KEY { get; set; }

    public byte[]? MOD_DATE { get; set; }

    [StringLength(250)]
    
    public string? MOD_BY { get; set; }

    [StringLength(1000)]
    public string? REMARK { get; set; }
}
