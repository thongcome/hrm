using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Advance.SecurityCore.Domain;

// NOT in the original requested entity list (sc_user, sc_role, sc_user_role,
// sc_menu, sc_role_menu, sc_program_role, AuditLog) — added because
// ScProgramRouteSeeder.cs (explicitly requested for copy) writes into this
// legacy `sc_program` route registry table, and leaving the seeder half-
// copied with nothing to write to would be worse than flagging the addition
// here. Copied from HRM's Model/sc_program.cs, trimmed: `sc_role_programs`
// navigation to sc_role_program removed — sc_role_program is the OLDER
// "Program:XXX" progcode mechanism CLAUDE.md calls a superseded
// proof-of-concept ("don't extend it to new pages") layered on top of this
// table, and it was not requested for this extraction. See
// EXTRACTION-PLAN.md's "sc_program vs sc_program_role" note.
[Table("sc_program")]
public partial class sc_program
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long progid { get; set; }

    [StringLength(250)]
    public string progname { get; set; } = null!;

    [Column(TypeName = "text")]
    public string? templatename { get; set; }

    [Column(TypeName = "text")]
    public string? filename { get; set; }

    [StringLength(50)]
    public string progcode { get; set; } = null!;

    [StringLength(50)]
    public string? progmastercode { get; set; }

    [Column(TypeName = "text")]
    public string? remark { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    public string? modby { get; set; }

    [Required]
    public bool? isactive { get; set; }
}
