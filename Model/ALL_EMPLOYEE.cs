using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("ALL_EMPLOYEE")]
public partial class ALL_EMPLOYEE
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(250)]
    
    public string? DATASOURCE { get; set; }

    [StringLength(250)]
    
    public string? PRS_NO { get; set; }

    [StringLength(250)]
    
    public string? EMP_INTL { get; set; }

    [StringLength(250)]
    
    public string? EMP_SURNME { get; set; }

    [StringLength(250)]
    
    public string? EMP_E_NAME { get; set; }

    public DateOnly? PRS_SC_D { get; set; }

    [StringLength(250)]
    
    public string? JBT_THAIDESC { get; set; }

    [StringLength(250)]
    
    public string? COMPANY { get; set; }

    [StringLength(250)]
    
    public string? DEPT1 { get; set; }

    [StringLength(250)]
    
    public string? DEPT2 { get; set; }

    [StringLength(250)]
    
    public string? DEPT3 { get; set; }

    [StringLength(250)]
    
    public string? SEX { get; set; }

    [StringLength(250)]
    
    public string? EMP_MARITAL { get; set; }

    public DateOnly? EMP_BIRTH { get; set; }

    [StringLength(500)]
    public string? EMP_ADDR_1 { get; set; }

    [StringLength(500)]
    public string? EMP_ADDR_2 { get; set; }

    [StringLength(500)]
    public string? EMP_ADDR_3 { get; set; }

    [StringLength(500)]
    public string? EMP_POST { get; set; }

    [StringLength(250)]
    
    public string? EMP_TEL { get; set; }

    [StringLength(250)]
    
    public string? EMP_EMAIL { get; set; }

    [StringLength(250)]
    
    public string? EMP_I_CARD { get; set; }

    public DateOnly? EMP_I_EXPIRE { get; set; }

    [StringLength(250)]
    
    public string? EMP_I_ISSUE { get; set; }

    [StringLength(10)]
    public string? STATUSEMP { get; set; }

    [StringLength(50)]
    
    public string? DIPCHIP { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? LOAD_DATE { get; set; }

    [StringLength(250)]
    
    public string? EMP_NAME { get; set; }

    [StringLength(50)]
    
    public string? DIPSHIP { get; set; }
}
