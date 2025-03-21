using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("vd_service")]
public partial class vd_service
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(50)]
    
    public string? service1_code { get; set; }

    public long? service1_id { get; set; }

    public int? service1_level { get; set; }

    [StringLength(250)]
    
    public string? service1_name_en { get; set; }

    [StringLength(250)]
    
    public string? service1_name { get; set; }

    [StringLength(50)]
    
    public string? service2_code { get; set; }

    public long? service2_id { get; set; }

    public int? service2_level { get; set; }

    [StringLength(50)]
    
    public string? service3_code { get; set; }

    public long? service3_id { get; set; }

    public int? service3_level { get; set; }

    [StringLength(50)]
    
    public string? service4_code { get; set; }

    public long? service4_id { get; set; }

    public int? service4_level { get; set; }

    public bool? isActive { get; set; }

    public bool? isApproved { get; set; }

    public string? remark { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    [StringLength(250)]
    
    public string? service_code_all { get; set; }

    [StringLength(250)]
    
    public string? approveby { get; set; }

    [StringLength(50)]
    
    public string? status { get; set; }

    [StringLength(1000)]
    public string? file1 { get; set; }

    [StringLength(1000)]
    public string? file2 { get; set; }

    [StringLength(1000)]
    public string? file3 { get; set; }

    [StringLength(50)]
    
    public string? vendor_code { get; set; }

    [StringLength(50)]
    
    public string? taxid { get; set; }

    public long? vendorid { get; set; }

    [StringLength(250)]
    
    public string? service2_name_en { get; set; }

    [StringLength(250)]
    
    public string? service2_name { get; set; }

    [StringLength(250)]
    
    public string? service3_name { get; set; }

    [StringLength(250)]
    
    public string? service4_name { get; set; }

    [StringLength(250)]
    
    public string? service4_name_en { get; set; }

    [StringLength(250)]
    
    public string? service3_name_en { get; set; }

    [StringLength(1000)]
    public string? file1_path { get; set; }

    [StringLength(1000)]
    public string? file2_path { get; set; }

    [StringLength(1000)]
    public string? file3_path { get; set; }
}
