using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// AD.CRUDManage (advance-crud-manage skill) — per-action rights per
// (role × page route prefix), the reference `sc_program_role` shape:
// independent Create/Read/Edit/Delete flags, one row per routed page per
// role. Rows are auto-seeded at every app startup by ProgramRoleSeeder
// (reflecting over @page routes — code is the source of truth for which
// pages exist), so nobody ever "goes and records" a page by hand; existing
// rows are never touched by the seeder, so a human's permission decisions
// survive restarts. Checks read this table through a ~60s in-memory cache
// (ProgramRoleService), NOT login-cookie claims — a permission change takes
// effect within a minute, no re-login. Fail-closed: no matching row = no
// rights.
//
// NOT the same thing as the older sc_program/sc_role_program progcode pair
// (ProgramAuthorization.cs's "Program:EMPLOYEE_CREATE" claims — a
// proof-of-concept wired on one page): that model stamps grants into the
// login cookie and keys by hand-seeded code strings. This table supersedes
// it for new wiring; the POC stays functional until migrated.
//
// candelete governs flag-delete (soft delete) only — nothing hard-deletes
// regardless of permission, per the CRUD standard.
[Table("sc_program_role")]
public partial class sc_program_role
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long id { get; set; }

    public long roleid { get; set; }

    // Route prefix with parameter segments stripped: "/leave-requests/detail/{Id:long}"
    // seeds as "/leave-requests/detail". Longest-prefix-first matching at
    // check time, so "/leave-requests" also covers sub-routes that have no
    // more specific row of their own.
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
