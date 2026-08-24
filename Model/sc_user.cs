using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.Services.Login;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("sc_user")]
[Index("userid", "password", "isEmployee", Name = "IX_sc_user")]
public partial class sc_user
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long userid { get; set; }

    public long company_id { get; set; }

    [ForeignKey("company_id")]
    public virtual com_company? company { get; set; }

    [StringLength(250)]
    public string firstname { get; set; } = null!;

    [StringLength(250)]
    public string lastname { get; set; } = null!;

    [StringLength(250)]
    public string loginname { get; set; } = null!;

    //[StrongPassword(ErrorMessage = "รหัสผ่านต้องมีความยาวอย่างน้อย 8 ตัวอักษร และมีตัวพิมพ์ใหญ่ ตัวพิมพ์เล็ก ตัวเลข และอักขระพิเศษ")]
    [StringLength(500)]
    public string? password { get; set; }

    [StringLength(250)]
    public string? phone { get; set; }

    [StringLength(250)]
    public string? mobilephone { get; set; }

    public bool isforcechanged { get; set; } = true;

    public bool isdisable { get; set; } = true;

    public bool iscancel { get; set; } = false;

    [Column(TypeName = "datetime")]
    public DateTime? pwdexpdate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? lasttimelogin { get; set; }

    public int? invalidpwcount { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? lastinvalidpwd { get; set; }

    public string? remark { get; set; }

    [StringLength(250)]
    public string? title { get; set; }

    public long? upperuserid { get; set; }

    [StringLength(250)]
    public string? orgname { get; set; }

    [StringLength(50)]
    public string? orgcode { get; set; }

    public DateOnly? startdate { get; set; }

    public DateOnly? enddate { get; set; }

    [StringLength(250)]
    public string? remindpwd { get; set; }

    [StringLength(250)]
    public string? social { get; set; }

    [StringLength(10)]
    public string? langcode { get; set; }

    public int? sex_sexid { get; set; }

    public long? title_titleid { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]

    public string? modby { get; set; }

    [StringLength(50)]

    public string? empid { get; set; }

    public long? orgid { get; set; }

    public bool isroot { get; set; } = false;

    [StringLength(250)]
    public string? email { get; set; }

    [StringLength(2)]

    public string? isEmployee { get; set; }

    [StringLength(50)]

    public string? poscode { get; set; }

    [StringLength(250)]

    public string? pos_name { get; set; }

    public bool isActivate { get; set; } = true;

    [StringLength(50)]

    public string? vendorCode { get; set; }

    public bool isVendor { get; set; } = false;

    public bool isAccept { get; set; } = false;

    [StringLength(50)]

    public string? vendorid { get; set; }

    [StringLength(250)]

    public string? supervisor { get; set; }

    [StringLength(250)]

    public string? costcenter { get; set; }

    [StringLength(250)]

    public string? verifycode { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? registerdate { get; set; }

    public bool isHeader { get; set; } = false;

    [StringLength(500)]
    public string? ref1 { get; set; }

    [StringLength(500)]
    public string? ref2 { get; set; }

    [StringLength(100)]
    public string? salt { get; set; }


    [StringLength(50)]

    public string? Companycode { get; set; }

    // Advance Security slice 2: bumped by HRMContext (see
    // Model/HRMContext.PermVersion.cs) whenever this user's sc_user_role,
    // or any sc_role_menu/sc_role_scope on a role they hold, changes.
    // Baked into a claim at sign-in (ScUserClaimsPrincipalFactory) and
    // compared against the live value on each revalidation
    // (IdentityRevalidatingAuthenticationStateProvider) — a mismatch means
    // this session's permissions are stale and forces a re-sign-in.
    [Required]
    public int permversion { get; set; } = 0;

    [InverseProperty("user")]
    public virtual ICollection<emp_checkin> emp_checkins { get; set; } = new List<emp_checkin>();

    [InverseProperty("user")]
    public virtual ICollection<sc_user_session> sc_user_sessions { get; set; } = new List<sc_user_session>();

    [InverseProperty("user")]
    public virtual ICollection<job_user_list> job_user_lists { get; set; } = new List<job_user_list>();

    [InverseProperty("user")]
    public virtual ICollection<sc_user_role> sc_user_roles { get; set; } = new List<sc_user_role>();

    [InverseProperty("user")]
    public virtual ICollection<wf_adhoc_user> wf_adhoc_users { get; set; } = new List<wf_adhoc_user>();

    [InverseProperty("user")]
    public virtual ICollection<wf_custom_user> wf_custom_users { get; set; } = new List<wf_custom_user>();
}
