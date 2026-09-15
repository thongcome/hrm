using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("com_company")]
public partial class com_company
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(50)]
    public string code { get; set; } = null!;

    [StringLength(500)]
    public string name { get; set; } = null!;

    [StringLength(500)]
    public string? name_en { get; set; }

    [StringLength(500)]
    public string? logo_file { get; set; }

    [StringLength(500)]
    public string? logp_path { get; set; }

    [StringLength(50)]
    
    public string? tax_id { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    [StringLength(2000)]
    public string? mission { get; set; }

    [StringLength(500)]
    public string? slogan { get; set; }

    [StringLength(500)]
    public string? website { get; set; }

    [StringLength(500)]
    public string? address_HQ { get; set; }

    [StringLength(500)]
    public string? tel { get; set; }

    [StringLength(250)]
    
    public string? email { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? capital_register { get; set; }

    public int? amount_emp { get; set; }

    public bool? isActive { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? startdate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? enddate { get; set; }

    [StringLength(2000)]
    public string? remark { get; set; }

    [StringLength(50)]

    public string? abbr { get; set; }

    // ----- ข้อมูลตามกฎหมาย (legal/compliance) -----
    [StringLength(100)]
    public string? BusinessTypeName { get; set; }          // ประเภทนิติบุคคล เช่น "บริษัทจำกัด", "ห้างหุ้นส่วนจำกัด"

    [Column(TypeName = "date")]
    public DateOnly? RegisteredDate { get; set; }           // วันที่จดทะเบียนนิติบุคคล

    public bool VatRegistered { get; set; }                 // จดทะเบียนภาษีมูลค่าเพิ่มหรือไม่ (ใช้ตอนออกใบกำกับภาษี)

    // ----- แผนที่ (map) -----
    [StringLength(500)]
    public string? MapUrl { get; set; }                     // ลิงก์ Google Maps ไปยังที่ตั้ง

    // ----- ข้อมูลลูกค้า/การขายเพิ่ม (CRM-lite — แถวนี้อาจเป็นบริษัทของเราเองหรือลูกค้าก็ได้) -----
    public CustomerStatusType? CustomerStatus { get; set; }  // ผู้มุ่งหวัง/ลูกค้าปัจจุบัน/เลิกใช้งานแล้ว

    public long? AccountOwnerUserId { get; set; }            // soft-link -> sc_user.userid, ผู้ดูแลบัญชีลูกค้ารายนี้

    [Column(TypeName = "date")]
    public DateOnly? NextRenewalDate { get; set; }           // วันครบกำหนดต่อสัญญา/ต่ออายุ

    [Column(TypeName = "nvarchar(max)")]
    public string? SalesNote { get; set; }                   // บันทึกโอกาสขายเพิ่ม/ประวัติการติดต่อ

    // ----- การเก็บเงิน (billing) -----
    public int? PaymentTermsDays { get; set; }               // เทอมการชำระเงิน (วัน) เช่น 30

    public virtual ICollection<Com_CompanyContact> Contacts { get; set; } = new List<Com_CompanyContact>();

    // ----- ทะเบียนลูกค้า (customer registry) — mirror จาก vd_* (ระบบผู้ขาย),
    // ดู hrm_vd_vendor_tables memory + [[advance-customer-account]] -----
    public virtual ICollection<Com_CustAddress> CustAddresses { get; set; } = new List<Com_CustAddress>();
    public virtual ICollection<Com_CustFinancial> CustFinancials { get; set; } = new List<Com_CustFinancial>();
    public virtual ICollection<Com_CustService> CustServices { get; set; } = new List<Com_CustService>();
    public virtual ICollection<Com_CustDoc> CustDocs { get; set; } = new List<Com_CustDoc>();
    public virtual ICollection<Com_CustCertificate> CustCertificates { get; set; } = new List<Com_CustCertificate>();
    public virtual ICollection<Com_CustPortfolio> CustPortfolios { get; set; } = new List<Com_CustPortfolio>();
    public virtual ICollection<Com_CustSigned> CustSigneds { get; set; } = new List<Com_CustSigned>();
}
