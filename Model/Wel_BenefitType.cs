using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Welfare-benefit category — the kind of benefit, for grouping/reporting.
public enum WelfareBenefitCategory
{
    Medical = 0,     // ค่ารักษาพยาบาล / OPD
    Grant = 1,       // เงินช่วยเหลือ (งานศพ/สมรส/คลอดบุตร)
    HealthCheck = 2, // ตรวจสุขภาพประจำปี
    Allowance = 3,   // เบี้ยเลี้ยง / ค่าอุปกรณ์ / ยูนิฟอร์ม
    Other = 9,
}

// How the entitlement is capped — drives what limit fields mean and how the
// balance (WelfareBalanceService, phase 2) is computed.
public enum WelfareEntitlementMode
{
    AnnualAmount = 0,     // วงเงินรวมต่อปี (บาท) — AnnualLimitAmount
    PerEventAmount = 1,   // จำกัดต่อครั้ง (บาท) — PerEventLimitAmount (+ optional MaxClaimsPerYear)
    CountPerYear = 2,     // จำกัดจำนวนครั้งต่อปี — MaxClaimsPerYear
    // จ่ายประจำทุกเดือนผ่าน payroll (เช่น ค่ารถ) — ไม่ใช่การเบิก แต่เป็นเงินได้
    // ที่เข้าเงินเดือนอัตโนมัติ. จำนวน = MonthlyAllowanceAmount (ปรับรายคน/ตำแหน่ง
    // ได้ผ่าน Wel_Entitlement). เป็นคนละความหมายกับโหมด "cap" ข้างบน — เงินเดือน
    // ไม่ใช่เพดานการเบิก จึงต้องเป็นค่า enum ใหม่ ไม่ reuse ของเดิม.
    MonthlyAllowance = 3,
    Informational = 9,    // ไม่จำกัด/ให้ข้อมูลเฉยๆ (เช่น สวัสดิการที่บริษัทจัดให้ ไม่ต้องเบิก)
}

// One entry in a company's welfare-benefit catalog — the "entitlement"
// definition employees claim against (Wel_Claim, phase 2). Its own module
// (Wel_* prefix) alongside the financial-benefit tables (Pay_ProvidentFund*,
// Pay_InsurancePlan, Pay_WelfareFundPolicy) which cover funds/insurance; this
// covers the discretionary welfare the company grants (medical, allowances,
// life-event grants, health checks) that had no home before.
//
// Master-data discipline (advance-data-discipline skill): a stable human Code,
// status lifecycle via IsActive (block, never hard-delete), company-scoped by
// the string CompanyId convention (matches Hremployee.companyid /
// payroll_company, NOT a numeric FK), and config-first — categories and
// entitlement rules are editable data, seeded with sensible defaults, never
// hardcoded. Eligibility reuses the same MinServiceMonths gate as leave.
public class Wel_BenefitType
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(6)]
    public string CompanyId { get; set; } = null!;

    // Stable human-facing code (e.g. "MED_OPD", "GRANT_FUNERAL") — appears on
    // claims and reports; must not change casually.
    [Required, StringLength(30)]
    public string Code { get; set; } = null!;

    [Required, StringLength(120)]
    public string NameTh { get; set; } = null!;

    [StringLength(120)]
    public string? NameEn { get; set; }

    public WelfareBenefitCategory Category { get; set; }

    public WelfareEntitlementMode EntitlementMode { get; set; }

    // วงเงินรวมต่อปี (ใช้เมื่อ EntitlementMode == AnnualAmount).
    [Column(TypeName = "decimal(15,2)")]
    public decimal? AnnualLimitAmount { get; set; }

    // วงเงินต่อครั้ง (ใช้เมื่อ EntitlementMode == PerEventAmount).
    [Column(TypeName = "decimal(15,2)")]
    public decimal? PerEventLimitAmount { get; set; }

    // จำนวนครั้งสูงสุดต่อปี (ใช้เมื่อ CountPerYear หรือคุมจำนวนครั้งของ PerEventAmount).
    public int? MaxClaimsPerYear { get; set; }

    // ---- MonthlyAllowance mode (จ่ายประจำเข้า payroll) ----
    // จำนวนเงินจ่ายประจำต่อเดือน (ค่าเริ่มต้นบริษัท — override รายตำแหน่ง/รายคน
    // ได้ผ่าน Wel_Entitlement.OverrideAmount, resolve ด้วย WelfareEntitlementResolver).
    [Column(TypeName = "decimal(15,2)")]
    public decimal? MonthlyAllowanceAmount { get; set; }

    // ประเภทเงินได้ (Pay_PayItemType, หมวด Earning) ที่เงินจ่ายประจำนี้จะโพสต์เป็น
    // บรรทัดในสลิป — null = ใช้ ALLOWANCE เป็นค่าเริ่มต้น.
    public int? PayItemTypeId { get; set; }
    public virtual Pay_PayItemType? PayItemType { get; set; }

    // เงินจ่ายประจำนี้เป็นเงินได้ที่ต้องเสียภาษีหรือไม่ (ค่ารถถือเป็นเงินได้พึงประเมิน
    // ปกติเสียภาษี — บางสวัสดิการอาจยกเว้น).
    public bool IsTaxable { get; set; } = true;

    // ต้องแนบใบเสร็จ/หลักฐานเมื่อเบิก — ขับเคลื่อน UI แนบไฟล์ของหน้า claim (phase 2).
    public bool RequiresReceipt { get; set; } = true;

    // อายุงานขั้นต่ำ (เดือน) ก่อนมีสิทธิ์ — null = ไม่มีเงื่อนไข. เช็คด้วย TenureHelper
    // เหมือน Lve_LeavePolicy.MinServiceMonths.
    public int? MinServiceMonths { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
