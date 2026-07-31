using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("com_organization")]
public partial class com_organization
{
    [Key]   
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment

    public long id { get; set; }

    [StringLength(50)]
    
    public string? code { get; set; }

    [StringLength(500)]
    public string? name { get; set; }

    [StringLength(500)]
    public string? name_en { get; set; }

    [StringLength(50)]
    
    public string? layer { get; set; }

    [StringLength(250)]
    
    public string? parent_code { get; set; }

    public bool isCompany { get; set; } =false;

    [StringLength(50)]
    
    public string? comp_code_all { get; set; }

    public bool isBranch { get; set; } = false;

    [StringLength(50)]
    
    public string? tax_id { get; set; }

    public bool isActive { get; set; } = true;

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    [StringLength(50)]
    
    public string? layer_code { get; set; }

    public long? approver_userid { get; set; }

    public bool isManPowerCount { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? startdate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? enddate { get; set; }

    [StringLength(50)]
    
    public string? org_type { get; set; }

    [StringLength(2000)]
    public string? remark { get; set; }

    [StringLength(50)]
    
    public string? abbr { get; set; }

    [StringLength(50)]
    
    public string? boss_emp_id { get; set; }

    [StringLength(50)]
    
    public string? approver_empid { get; set; }

    [StringLength(50)]
    
    public string? comp_code { get; set; }

    public long? companyid { get; set; }

    [StringLength(50)]
    
    public string? org_level { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? asOfDate { get; set; }

    [StringLength(50)]
    
    public string? orgCode { get; set; }

    [StringLength(500)]
    public string? orgLayerName { get; set; }

    [StringLength(500)]
    public string? orgLayerNameEn { get; set; }

    [StringLength(50)]
    
    public string? refkeyParent { get; set; }

    [StringLength(50)]
    
    public string? ref1 { get; set; }

    public long? parentID { get; set; }

    [StringLength(250)]
    
    public string? boss_name { get; set; }

    [StringLength(250)]
    
    public string? approver_name { get; set; }

    [StringLength(250)]
    
    public string? approver_PosName { get; set; }

    public int? node_level { get; set; }

    public bool istop { get; set; }= false;

    // Soft-linked cost center / GL code for this org node — same
    // string-code convention as code/comp_code/orgCode elsewhere on this
    // entity, not an enforced FK. Not yet reliably populated on real
    // employee records (Hremployee has no working link into this table
    // today) — see Hremployee.CostCenterCode for the field payroll
    // reporting actually reads from in the meantime.
    [StringLength(20)]
    public string? CostCenterCode { get; set; }
}
