using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("toa")]
public partial class toa
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long toaid { get; set; }

    [StringLength(50)]
    
    public string? comcode { get; set; }

    [StringLength(50)]
    
    public string? a { get; set; }

    public int? approvelevel { get; set; }

    [StringLength(250)]
    
    public string? ApprovelevelText { get; set; }

    [StringLength(250)]
    
    public string? LineExco { get; set; }

    [StringLength(50)]
    
    public string? approverEmpid { get; set; }

    [StringLength(250)]
    
    public string? NameEn { get; set; }

    [StringLength(250)]
    
    public string? NameTh { get; set; }

    [StringLength(50)]
    
    public string? delegateEmplD { get; set; }

    [StringLength(250)]
    
    public string? Company { get; set; }

    [StringLength(250)]
    
    public string? Position { get; set; }

    [StringLength(250)]
    
    public string? SectTh { get; set; }

    [StringLength(250)]
    
    public string? SectEn { get; set; }

    [StringLength(250)]
    
    public string? DeptTh { get; set; }

    [StringLength(250)]
    
    public string? DeptEn { get; set; }

    public bool isactive { get; set; }

    public DateOnly? StartDateDG { get; set; }

    public DateOnly? EnddateDG { get; set; }

    public int? wlevel { get; set; }

    public string? remark { get; set; }
}
