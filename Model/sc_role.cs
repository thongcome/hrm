using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

[Table("sc_role")]
public partial class sc_role
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long roleid { get; set; }

 
    public long company_id { get; set; }

    [ForeignKey("company_id")]
    public virtual com_company? company { get; set; } 

    //public long CompanyId { get; set; } // ไม่ nullable = ต้องมี
    //[ForeignKey("CompanyId")]
    //public virtual com_company Company { get; set; } = null!;



    [StringLength(250)]
    public string? name { get; set; }

    [StringLength(100)]
    
    public string? abbr { get; set; }

    [StringLength(50)]
    
    public string rolelevel { get; set; } = null!;

    [StringLength(50)]
    public string? upperrole { get; set; }

    [StringLength(50)]
    
    public string? rolecode { get; set; }

    [Required]
    public bool isactive { get; set; } = true;

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    public bool isHeader { get; set; } = false;

    [StringLength(500)]
    public string? ref1 { get; set; }

    [StringLength(500)]
    public string? ref2 { get; set; }

    // ประเภทพนักงาน (employeetype.code) ที่จะได้ role นี้อัตโนมัติตอน set user
    // ครั้งแรก (CEO access-control step 3, 2026-09-01). NULL = ไม่ auto-assign.
    // Matched against HREMPLOYEE.EMPTYPE_CODE by UserProvisioningService.
    [StringLength(100)]
    public string? employeetype_code { get; set; }

    // ตำแหน่ง/ระดับ (Pos_ExecType.Code, e.g. "A03" = หัวหน้างาน) ที่จะได้ role
    // นี้โดยอัตโนมัติ (CEO role-model expansion, 2026-09-07). NULL = ไม่
    // auto-assign. Unlike employeetype_code (bootstrap-once at first login,
    // an employee's type essentially never changes), this is CONTINUOUSLY
    // reconciled by DerivedRoleSyncService every startup against
    // HREMPLOYEE.POS_CODE — a promotion picks up the new role, a demotion or
    // transfer drops it, without anyone touching this user's account by hand.
    // Matched by code value only, not scoped to one company (same
    // simplification employeetype_code already makes).
    [StringLength(20)]
    public string? pos_exec_code { get; set; }

 
    [InverseProperty("role")]
    public virtual ICollection<sc_role_menu> sc_role_menus { get; set; } = new List<sc_role_menu>();

    [InverseProperty("role")]
    public virtual ICollection<sc_role_scope> sc_role_scopes { get; set; } = new List<sc_role_scope>();

    [InverseProperty("role")]
    public virtual ICollection<sc_role_program> sc_role_programs { get; set; } = new List<sc_role_program>();

    [InverseProperty("role")]
    public virtual ICollection<sc_user_role> sc_user_roles { get; set; } = new List<sc_user_role>();
}
