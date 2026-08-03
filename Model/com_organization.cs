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

    // Fixed-width hierarchical path: 2 digits per level, no delimiter,
    // built by walking parent_code up from the root (istop=true) — e.g.
    // CEO="01", HR (child of CEO)="0101", HRM (child of HR)="010101".
    // Lets Hremployee.orgcodefull / anything else that copies this value
    // filter an entire subtree with a plain `LIKE 'xxxx%'` at a fixed
    // offset, no recursive join needed. Backfilled once via a recursive
    // CTE in the migration that added this column — re-run that same
    // computation (not by hand) if the org tree is ever restructured.
    // NOTE: two existing rows both have code='Ploan' under parent 'loan'
    // (ids 9 and 10) — a real pre-existing data-quality issue (parent_code
    // links by code string, not id, so codes need to be unique per parent
    // to be unambiguous). The backfill still produces distinct, correct
    // orgcodefull values for both (010301/010302) since it keys off id,
    // but anything that instead tries to look up an org by code='Ploan'
    // alone can't tell which of the two is meant.
    [StringLength(100)]
    public string? orgcodefull { get; set; }

    // Soft-linked to Com_SectionType.Code — "ประเภทสังกัด" (org-level
    // classification), e.g. กรมการผู้จัดการใหญ่/ฝ่าย/ส่วน. Same string-code
    // convention as parent_code (not an enforced FK).
    [StringLength(20)]
    public string? SectionTypeCode { get; set; }

    // Soft-linked to Com_SubSectionType.Id — "ลักษณะของหน่วยงาน" (nature of
    // the unit, e.g. คณะกรรมการ/Front/Back/Support).
    public long? SubSectionTypeId { get; set; }

    public DateTime? createdate { get; set; }
    [StringLength(50)]
    public string? createby { get; set; }

    [StringLength(50)]
    public string? abbr_en { get; set; }
}
