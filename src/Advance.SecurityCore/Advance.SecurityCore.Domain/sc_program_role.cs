using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Advance.SecurityCore.Domain;

// Copied verbatim from HRM's Model/sc_program_role.cs — AD.CRUDManage's
// per-(role x route-path) Create/Read/Edit/Delete rights table. Confirmed
// fully generic already (see EXTRACTION-PLAN.md's ProgramRoleService
// section) — no HRM-specific column or logic to trim.
[Table("sc_program_role")]
public partial class sc_program_role
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long id { get; set; }

    public long roleid { get; set; }

    // Route prefix with parameter segments stripped: longest-prefix-first
    // matching at check time.
    [Required, StringLength(200)]
    public string progpath { get; set; } = null!;

    public bool cancreate { get; set; }
    public bool canread { get; set; }
    public bool canedit { get; set; }
    public bool candelete { get; set; }

    [Required]
    public bool isactive { get; set; } = true;

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    public string? modby { get; set; }
}
