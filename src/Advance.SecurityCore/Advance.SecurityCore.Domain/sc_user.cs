using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Advance.SecurityCore.Domain;

// Copied from HRM's Model/sc_user.cs (D:\GitWorkspace\HRM\Model\sc_user.cs) with
// deliberate trims — see EXTRACTION-PLAN.md "Domain copy trims" for the full
// list and reasoning. In short:
//   - `company` navigation to com_company REMOVED (com_company is a
//     com_*/sc_* bigint-keyed table that was not in this extraction's scope;
//     company_id is kept as a plain scalar long so the column/FK shape on
//     the table is unchanged — a host app that also owns com_company can
//     re-add the navigation property in its own partial/derived mapping).
//   - Navigations to job_user_list, wf_adhoc_user, wf_custom_user, emp_checkin
//     REMOVED — those belong to the Workflow/Attendance domains, not
//     SecurityCore. sc_user_role and sc_user_session stay (session tracking
//     is a SecurityCore concern: ScUserClaimsPrincipalFactory stamps a
//     session row on every sign-in).
//   - sc_user_session itself is not part of this extraction's requested
//     entity list (sc_user, sc_role, sc_user_role, sc_menu, sc_role_menu,
//     sc_program_role, AuditLog) — the navigation collection and the
//     session-stamping code in ScUserClaimsPrincipalFactory are commented
//     out / flagged rather than silently dropped so the human doing the
//     real cutover sees the gap. See EXTRACTION-PLAN.md.
// All column names, types and StringLengths are otherwise byte-for-byte the
// same as HRM's mapping, since this must stay wire-compatible with the same
// live `sc_user` table during a staged migration (no dual-write, no data
// migration — the same rows, read from two codebases during the transition).
[Table("sc_user")]
[Index("userid", "password", "isEmployee", Name = "IX_sc_user")]
public partial class sc_user
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long userid { get; set; }

    // company navigation intentionally omitted — see file header.
    public long company_id { get; set; }

    [StringLength(250)]
    public string firstname { get; set; } = null!;

    [StringLength(250)]
    public string lastname { get; set; } = null!;

    [StringLength(250)]
    public string loginname { get; set; } = null!;

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

    // Advance Security slice 2 (permversion bump-on-role-change) — kept as a
    // plain column; the HRMContext.PermVersion.cs interceptor that bumps it
    // is HRM-specific EF interceptor code, not copied here (see
    // EXTRACTION-PLAN.md — flagged as something SecurityCore's own
    // SecurityDbContext should eventually re-implement, not HRM-only).
    [Required]
    public int permversion { get; set; } = 0;

    // External-auth mode for this account (AD/SSO). null/empty = local
    // password. "AD" = verified via LDAP bind. Anything else = the name of a
    // configured SSO provider.
    [StringLength(50)]
    public string? AuthProvider { get; set; }

    [InverseProperty("user")]
    public virtual ICollection<sc_user_role> sc_user_roles { get; set; } = new List<sc_user_role>();

    // sc_user_session (session-revocation tracking) NOT included in this
    // extraction's requested entity list — ScUserClaimsPrincipalFactory
    // below has its session-stamping code commented out pending that table
    // being added to SecurityCore.Domain. See EXTRACTION-PLAN.md.
}
