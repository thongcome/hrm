using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("app_application_list")]
public partial class app_application_list
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(50)]
    
    public string? code { get; set; }

    [StringLength(250)]
    
    public string? name { get; set; }

    [StringLength(250)]
    
    public string? name_en { get; set; }

    [StringLength(50)]
    
    public string? company_code { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? startdate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? enddate { get; set; }
}
