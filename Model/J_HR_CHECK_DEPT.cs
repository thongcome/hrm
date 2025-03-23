using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("J_HR_CHECK_DEPT")]
public partial class J_HR_CHECK_DEPT
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    public long? EMP_KEY { get; set; }

    [StringLength(250)]
    
    public string? PRS_NO { get; set; }

    [StringLength(250)]
    
    public string? PRS_E_CARD { get; set; }

    [StringLength(250)]
    
    public string? EMP_NAME { get; set; }

    [StringLength(250)]
    
    public string? EMP_SURNME { get; set; }

    [StringLength(250)]
    
    public string? PRS_TITLE { get; set; }

    [StringLength(250)]
    
    public string? PRS_DEPT { get; set; }

    [StringLength(50)]
    
    public string? PRI_STATUS { get; set; }

    [StringLength(50)]
    
    public string? PRS_GRADE_EX { get; set; }

    public byte[]? MOD_DATE { get; set; }

    [StringLength(250)]
    
    public string? MOD_BY { get; set; }

    [StringLength(1000)]
    public string? REMARK { get; set; }
}
