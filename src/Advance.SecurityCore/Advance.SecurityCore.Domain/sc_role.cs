using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Advance.SecurityCore.Domain;

// Copied from HRM's Model/sc_role.cs. Trims: `company` navigation to
// com_company removed (see sc_user.cs header for the reasoning — company_id
// stays a plain scalar). Navigations to sc_role_scope and sc_role_program
// removed: both are HRM-specific permission slices layered on top of the
// base role/menu model (data-scope restriction, and the superseded
// Program:XXX proof-of-concept respectively) — neither was in this
// extraction's requested entity list. sc_role_menu and sc_user_role stay;
// they are the generic AD.CRUDManage-adjacent role/menu shape this package
// owns.
[Table("sc_role")]
public partial class sc_role
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long roleid { get; set; }

    public long company_id { get; set; }

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

    // Host-specific auto-assign hooks (matched against a host's own employee
    // master, e.g. HREMPLOYEE.EMPTYPE_CODE / POS_CODE in HRM). Left as plain
    // columns here — SecurityCore doesn't know what an "employee type" or
    // "position" is; a host's own sync job (DerivedRoleSyncService-equivalent)
    // reads these columns and does the matching against its own data.
    [StringLength(100)]
    public string? employeetype_code { get; set; }

    [StringLength(20)]
    public string? pos_exec_code { get; set; }

    [InverseProperty("role")]
    public virtual ICollection<sc_role_menu> sc_role_menus { get; set; } = new List<sc_role_menu>();

    [InverseProperty("role")]
    public virtual ICollection<sc_user_role> sc_user_roles { get; set; } = new List<sc_user_role>();
}
