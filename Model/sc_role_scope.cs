using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// SAP-authorization-object-style data scope for a role: which rows a
// member of this role sees, not which screens/actions (that's
// sc_role_menu). scopevalue's meaning depends on scopetype:
//   Company    -> exact match against Hremployee.companyid
//   Org/Branch -> materialized-path prefix against Hremployee.orgcodefull
//                 ("100" matches "100" and "100.02", never "1002" — the
//                 trailing "." on the LIKE pattern is what prevents that;
//                 built in RoleScopeSnapshot, not stored here)
//   CostCenter -> exact match against Hremployee.CostCenterCode
// Branch uses the exact same prefix mechanism as Org — it's a UI-level
// distinction only (the picker restricts to com_organization rows where
// isBranch=true), not a separate check.
[Table("sc_role_scope")]
public partial class sc_role_scope
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long scopeid { get; set; }

    public long roleid { get; set; }

    public RoleScopeType scopetype { get; set; }

    [Required, StringLength(250)]
    public string scopevalue { get; set; } = null!;

    public DateOnly? startdate { get; set; }
    public DateOnly? enddate { get; set; }

    [Required]
    public bool isactive { get; set; } = true;

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    public string? modby { get; set; }

    [ForeignKey("roleid")]
    [InverseProperty("sc_role_scopes")]
    public virtual sc_role role { get; set; } = null!;
}
