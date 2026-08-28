using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

// Bridges HRM's own role model (just Admin/Employee — see sc_role) to
// whatever fine-grained role vocabulary a downstream OIDC client (ERP, and
// any future HumanOk module) actually needs for its own authorization —
// e.g. ERP expects strings like "Sales"/"Accountant"/"Inventory"/"Admin",
// a vocabulary HRM has no reason to know about natively. Rather than guess
// or hardcode a mapping, this is plain config: HR assigns one row per
// (user, client) that needs a specific role; anyone with no row for a given
// ClientId falls back to the client's own safe default (ERP's is
// "Viewer" — see HRM-SSO-Handoff.md section 7/9, "role ไม่ match -> Viewer").
[Table("Sso_ClientRoleMapping")]
[Index(nameof(ScUserId), nameof(ClientId), IsUnique = true)]
public class Sso_ClientRoleMapping
{
    [Key]
    public long Id { get; set; }

    public long ScUserId { get; set; }

    // The OIDC client_id this mapping applies to (e.g. "erp-web") — free
    // text so a future second client doesn't need a schema change.
    [Required, StringLength(100)]
    public string ClientId { get; set; } = null!;

    // Free text by design — the exact role vocabulary is agreed out-of-band
    // between HR and each client team (see handoff doc section 8), not a
    // fixed enum HRM would need to keep in sync with every downstream app.
    [Required, StringLength(100)]
    public string Role { get; set; } = null!;

    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public long? CreatedByUserId { get; set; }

    public bool IsActive { get; set; } = true;
}
