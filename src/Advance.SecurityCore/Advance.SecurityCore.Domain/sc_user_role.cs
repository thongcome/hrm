using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Advance.SecurityCore.Domain;

// Copied verbatim (mapping-wise) from HRM's Model/sc_user_role.cs — no
// out-of-scope navigations to trim, this table already only points at
// sc_role/sc_user.
[Table("sc_user_role")]
public partial class sc_user_role
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long user_roleID { get; set; }

    public long roleid { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? modate { get; set; }

    [StringLength(250)]
    public string? modby { get; set; }

    [Required]
    public bool isactive { get; set; } = true;

    public DateOnly? startdate { get; set; }

    public DateOnly? enddate { get; set; }

    public long userid { get; set; }

    [StringLength(50)]
    public string? empid { get; set; }

    [ForeignKey("roleid")]
    [InverseProperty("sc_user_roles")]
    public virtual sc_role role { get; set; } = null!;

    [ForeignKey("userid")]
    [InverseProperty("sc_user_roles")]
    public virtual sc_user user { get; set; } = null!;
}
