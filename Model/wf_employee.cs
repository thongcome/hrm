using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("wf_employee")]
public partial class wf_employee
{
    [StringLength(25)]
    
    public string empid { get; set; } = null!;

    public int? titleid { get; set; }

    public string? picname { get; set; }

    [StringLength(250)]
    
    public string firstname_th { get; set; } = null!;

    [StringLength(250)]
    
    public string? midname_th { get; set; }

    [StringLength(250)]
    
    public string lastname_th { get; set; } = null!;

    [StringLength(50)]
    
    public string? nickname { get; set; }

    [StringLength(20)]
    
    public string cardid { get; set; } = null!;

    [StringLength(10)]
    
    public string sexid { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime? birthdate { get; set; }

    [StringLength(50)]
    
    public string? nation { get; set; }

    
    public string? remark { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(50)]
    
    public string? modby { get; set; }

    [StringLength(100)]
    
    public string? firstname_en { get; set; }

    [StringLength(100)]
    
    public string? midname_en { get; set; }

    [StringLength(100)]
    
    public string? lastname_en { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? start_date { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? end_date { get; set; }

    public int? employeetype { get; set; }

    [StringLength(10)]
    
    public string? status { get; set; }

    [StringLength(100)]
    
    public string? email { get; set; }

    [StringLength(250)]
    
    public string? phone { get; set; }

    [StringLength(50)]
    
    public string? orgcode { get; set; }

    [StringLength(50)]
    
    public string? orgcodefull { get; set; }

    [StringLength(250)]
    
    public string? orgname_th { get; set; }

    [StringLength(250)]
    
    public string? orgname_en { get; set; }

    public bool? isactive { get; set; }

    [StringLength(50)]
    
    public string? wlevel { get; set; }

    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }
}
