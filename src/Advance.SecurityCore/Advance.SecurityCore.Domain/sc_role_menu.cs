using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Advance.SecurityCore.Domain;

// Copied verbatim (mapping-wise) from HRM's Model/sc_role_menu.cs.
[Table("sc_role_menu")]
public partial class sc_role_menu
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long rolemenuid { get; set; }

    public long menuid { get; set; }

    public long roleid { get; set; }

    public DateOnly? startdate { get; set; }

    public DateOnly? enddate { get; set; }

    [Required]
    public bool isactive { get; set; } = true;

    // Read-only lock: grants menu access but hides Create/Edit affordances
    // (checked via the "menu_edit" claim). Default true so every existing
    // grant keeps working exactly as before.
    [Required]
    public bool canedit { get; set; } = true;

    [Column(TypeName = "text")]
    public string? remark { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    public string? modby { get; set; }

    [ForeignKey("menuid")]
    [InverseProperty("sc_role_menus")]
    public virtual sc_menu menu { get; set; } = null!;

    [ForeignKey("roleid")]
    [InverseProperty("sc_role_menus")]
    public virtual sc_role role { get; set; } = null!;
}
